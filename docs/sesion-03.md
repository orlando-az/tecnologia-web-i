# Guía Completa: Publicar una Web API ASP.NET Core con SQLite y Scalar en IIS (Windows 10)

---

## Índice

1. [Requisitos previos](#1-requisitos-previos)
2. [Modificar el proyecto antes de publicar](#2-modificar-el-proyecto-antes-de-publicar)
3. [Publicar la aplicación](#3-publicar-la-aplicación)
4. [Habilitar IIS en Windows 10](#4-habilitar-iis-en-windows-10)
5. [Instalar el .NET Hosting Bundle](#5-instalar-el-net-hosting-bundle)
6. [Configurar el sitio en IIS](#6-configurar-el-sitio-en-iis)
7. [Permisos de carpeta para SQLite](#7-permisos-de-carpeta-para-sqlite)
8. [Probar la API](#8-probar-la-api)
9. [Acceso desde otros equipos de la red](#9-acceso-desde-otros-equipos-de-la-red)
10. [Cómo actualizar la API después de cambios](#10-cómo-actualizar-la-api-después-de-cambios)
11. [Solución de problemas comunes](#11-solución-de-problemas-comunes)
12. [Tips y buenas prácticas](#12-tips-y-buenas-prácticas)

---

## 1. Requisitos previos

Antes de empezar, asegúrate de tener:

- **Windows 10** (Pro, Enterprise o Education — la versión Home tiene IIS limitado)
- **Visual Studio** o el **SDK de .NET** instalado (en este caso .NET 10)
- Tu proyecto **ASP.NET Core Web API con SQLite** funcionando en modo desarrollo
- **Acceso como administrador** en tu equipo

---

## 2. Modificar el proyecto antes de publicar

### 2.1 — Modificar el archivo .csproj

Necesitas que el archivo de base de datos `delivery.db` se copie automáticamente al publicar. Abre `DeliveryApi.csproj` y agrega este bloque antes del cierre `</Project>`:

```xml
<ItemGroup>
  <None Update="delivery.db">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
  </None>
</ItemGroup>
```

> **Nota:** `PreserveNewest` solo copia el archivo si es más nuevo, así no sobreescribes datos en producción cada vez que publicas.

El archivo completo queda así:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.5" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.6" />
    <PackageReference Include="Scalar.AspNetCore" Version="*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <Folder Include="Controllers\" />
    <Folder Include="Data\" />
    <Folder Include="Models\" />
  </ItemGroup>

  <ItemGroup>
    <None Update="delivery.db">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
    </None>
  </ItemGroup>

</Project>
```

---

### 2.2 — Modificar Program.cs

Se necesitan 4 cambios importantes:

1. **Fijar `ContentRootPath`** para que IIS encuentre el archivo `delivery.db`
2. **Mover el connection string** al `appsettings.json`
3. **Habilitar OpenAPI y Scalar en producción** para poder probar desde IIS con una interfaz visual interactiva
4. **Activar WAL mode** en SQLite para mejor rendimiento

Primero, instala el paquete de Scalar en la terminal de tu proyecto:

```
dotnet add package Scalar.AspNetCore
```

Luego modifica `Program.cs`:

```csharp
using DeliveryApi.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

// Habilitar OpenAPI y Scalar en todos los entornos
app.MapOpenApi();
app.MapScalarApiReference();

// Habilitar WAL mode para SQLite (mejor rendimiento en IIS)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

app.MapControllers();
//app.UseHttpsRedirection();

app.Run();
```

**¿Por qué cada cambio?**

| Cambio | Razón |
|--------|-------|
| `ContentRootPath = AppContext.BaseDirectory` | Sin esto, IIS busca `delivery.db` en `C:\Windows\System32` en vez de la carpeta de tu app |
| `GetConnectionString("DefaultConnection")` | Buena práctica: el connection string queda en configuración, no en código |
| `using Scalar.AspNetCore` | Importa la librería de Scalar para la documentación interactiva |
| `app.MapOpenApi()` fuera del `if` | Expone el esquema OpenAPI en todos los entornos |
| `app.MapScalarApiReference()` | Habilita la interfaz visual de Scalar para explorar y probar la API |
| `PRAGMA journal_mode=WAL` | SQLite maneja mejor las peticiones simultáneas con WAL activado |

---

### 2.3 — Modificar appsettings.json

Agrega el bloque `ConnectionStrings`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=delivery.db"
  }
}
```

> La ruta `Data Source=delivery.db` funciona correctamente porque en Program.cs ya fijamos `ContentRootPath = AppContext.BaseDirectory`, así que buscará el archivo en la misma carpeta donde está la DLL publicada.

---

### 2.4 — Verificar que compile

Abre una terminal en la carpeta del proyecto y ejecuta:

```
dotnet build
```

Debe decir **Build succeeded** con 0 errores antes de continuar.

---

## 3. Publicar la aplicación

En la terminal, ejecuta:

```
dotnet publish -c Release
```

Los archivos se generan en:

```
tu-proyecto\bin\Release\net10.0\publish\
```

**Verifica que la carpeta `publish` contenga estos archivos clave:**

- `DeliveryApi.dll` — tu aplicación compilada
- `web.config` — se genera automáticamente, IIS lo necesita
- `delivery.db` — tu base de datos SQLite
- `appsettings.json` — tu configuración

Luego **copia toda la carpeta `publish`** a la ubicación donde IIS la servirá, por ejemplo:

```
C:\inetpub\DeliveryApi
```

> Puedes copiarla a cualquier ruta que prefieras. Lo importante es recordar dónde la pusiste para configurar IIS.

---

## 4. Habilitar IIS en Windows 10

IIS viene incluido en Windows 10 pero está desactivado por defecto.

1. Presiona **Win + R**, escribe `optionalfeatures` y presiona **Enter**
2. En la ventana "Características de Windows", busca y marca **Internet Information Services**
3. Expande el nodo y verifica que estén marcados:
   - **Herramientas de administración web** > **Consola de administración de IIS**
   - **Servicios World Wide Web** > **Características de desarrollo de aplicaciones** > **ASP.NET 4.8** y **Extensibilidad de .NET 4.8**
   - **Servicios World Wide Web** > **Características HTTP comunes** > **Documento predeterminado**, **Contenido estático**, **Errores HTTP**
4. Click **Aceptar** y espera (2-5 minutos)

**Verificar:** abre el navegador y ve a `http://localhost`. Debes ver la página azul de bienvenida de IIS.

---

## 5. Instalar el .NET Hosting Bundle

**Este es el paso más crítico.** Sin el Hosting Bundle, IIS no puede ejecutar aplicaciones ASP.NET Core.

1. Ve a: **https://dotnet.microsoft.com/en-us/download/dotnet/10.0**
2. En la sección **ASP.NET Core Runtime**, descarga el **Hosting Bundle** (Windows)
3. Ejecuta el instalador **como administrador**
4. Sigue los pasos hasta completar

**Después de instalar, reinicia IIS.** Abre **CMD como administrador**:

```
net stop was /y
net start w3svc
```

> **Importante:** si te saltas el reinicio de IIS, el módulo de ASP.NET Core no se carga y verás errores 500.19 o 502.5.

---

## 6. Configurar el sitio en IIS

Abre IIS Manager: **Win + R** > `inetmgr` > **Enter**

### 6.1 — Crear el Application Pool

1. Panel izquierdo: click en **Grupos de aplicaciones** (Application Pools)
2. Panel derecho: click en **Agregar grupo de aplicaciones...** (Add Application Pool)
3. Configura:
   - **Nombre:** `DeliveryApiPool`
   - **Versión de .NET CLR:** **Sin código administrado** (No Managed Code)
   - **Modo de canalización administrada:** Integrada (Integrated)
4. Click **Aceptar**

> **¿Por qué "Sin código administrado"?** Porque ASP.NET Core usa su propio runtime. No necesita el CLR clásico de .NET Framework.

### 6.2 — Crear el sitio web

1. Panel izquierdo: click derecho en **Sitios** (Sites) > **Agregar sitio web...** (Add Website)
2. Configura:
   - **Nombre del sitio:** `DeliveryApi`
   - **Grupo de aplicaciones:** click en **Seleccionar...** y elige `DeliveryApiPool`
   - **Ruta de acceso física:** `C:\inetpub\DeliveryApi` (donde copiaste el publish)
   - **Puerto:** `5050` (para no chocar con el Default Web Site que usa el 80)
3. Click **Aceptar**

---

## 7. Permisos de carpeta para SQLite

**Este paso es fundamental.** SQLite necesita permisos de escritura porque crea archivos temporales (.db-journal, .db-wal, .db-shm) junto al archivo de base de datos.

1. Abre el explorador de archivos, ve a `C:\inetpub\DeliveryApi`
2. Click derecho > **Propiedades** > pestaña **Seguridad**
3. Click **Editar** > **Agregar**
4. Escribe `IIS_IUSRS`, click **Comprobar nombres** > **Aceptar**
5. Marca el permiso **Modificar** ✅
6. Click **Aplicar** > **Aceptar**
7. Repite el proceso agregando el usuario `IUSR` también con permiso **Modificar** ✅

> **Sin estos permisos** obtendrás el error: `SqliteException: SQLite Error 8: 'attempt to write a readonly database'`

---

## 8. Probar la API

Abre el navegador y ve a:

```
http://localhost:5050/api/TuControlador
```

Cambia `TuControlador` por el nombre real de uno de tus controllers. Deberías ver la respuesta JSON de tu API.

**Probar con Scalar (interfaz visual interactiva):**

```
http://localhost:5050/scalar/v1
```

Scalar te muestra todos tus endpoints organizados, con ejemplos de request/response, y te permite probar cada uno directamente desde el navegador. Es mucho más cómodo que probar con URLs manuales.

**Ver el esquema OpenAPI en JSON:**

```
http://localhost:5050/openapi/v1.json
```

---

## 9. Acceso desde otros equipos de la red

Si quieres acceder a la API desde otro equipo o desde tu celular:

### 9.1 — Averiguar tu IP

En CMD ejecuta:

```
ipconfig
```

Busca la **Dirección IPv4** (ejemplo: `192.168.1.50`).

### 9.2 — Abrir el puerto en el Firewall

Abre **CMD como administrador**:

```
netsh advfirewall firewall add rule name="DeliveryApi" dir=in action=allow protocol=TCP localport=5050
```

### 9.3 — Probar desde otro equipo

Desde otra computadora o celular conectado a la misma red:

```
http://192.168.1.50:5050/api/TuControlador
```

---

## 10. Cómo actualizar la API después de cambios

Cada vez que modifiques el código de tu API:

1. **Publicar nuevamente:**
   ```
   dotnet publish -c Release
   ```

2. **Detener el sitio en IIS:**
   - Abre IIS Manager (`inetmgr`)
   - Selecciona el sitio `DeliveryApi`
   - Panel derecho: click **Detener** (Stop)

3. **Copiar los archivos:**
   - Copia el contenido de `bin\Release\net10.0\publish\` a `C:\inetpub\DeliveryApi`
   - Reemplaza todos los archivos

4. **Iniciar el sitio:**
   - En IIS Manager, click **Iniciar** (Start)

> **Importante:** si no detienes el sitio antes de copiar, algunos archivos pueden estar bloqueados y no se copiarán correctamente.

---

## 11. Solución de problemas comunes

### Error 500.19 — Error de configuración

**Causa:** el Hosting Bundle no está instalado o IIS no se reinició después.

**Solución:**
```
net stop was /y
net start w3svc
```
Si persiste, reinstala el Hosting Bundle.

---

### Error 502.5 — Process Failure

**Causa:** la aplicación no puede iniciar. Puede ser un error en el código o falta el runtime.

**Solución:** habilita los logs. Edita `web.config` en la carpeta publicada:

```xml
<aspNetCore processPath="dotnet"
            arguments=".\DeliveryApi.dll"
            stdoutLogEnabled="true"
            stdoutLogFile=".\logs\stdout"
            hostingModel="InProcess" />
```

Crea la carpeta `logs` dentro de `C:\inetpub\DeliveryApi\` y recarga la página. Revisa el archivo de log generado.

---

### Error 403 — Forbidden

**Causa:** faltan permisos en la carpeta.

**Solución:** verifica que `IIS_IUSRS` y `IUSR` tengan permiso **Modificar** (ver sección 7).

---

### Error 404 — Not Found

**Causa posibles:**
- La ruta del endpoint está mal
- El sitio apunta a la carpeta incorrecta
- `web.config` tiene mal el nombre de la DLL

**Solución:** verifica que en `web.config` el valor de `arguments` sea `.\DeliveryApi.dll` (el nombre exacto de tu DLL).

---

### SQLite Error 8: attempt to write a readonly database

**Causa:** `IIS_IUSRS` no tiene permisos de escritura.

**Solución:** dar permiso **Modificar** a `IIS_IUSRS` y `IUSR` en la carpeta de la app (ver sección 7).

---

### La API funciona en localhost pero no desde otro equipo

**Causa:** el Firewall de Windows bloquea el puerto.

**Solución:**
```
netsh advfirewall firewall add rule name="DeliveryApi" dir=in action=allow protocol=TCP localport=5050
```

---

### Error de conexión a base de datos (no encuentra delivery.db)

**Causa:** `ContentRootPath` no está configurado correctamente.

**Solución:** verifica que tu `Program.cs` tenga:
```csharp
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
```

---

## 12. Tips y buenas prácticas

### Variables de entorno

Para configurar el entorno como Production, puedes editarlo en `web.config`:

```xml
<aspNetCore ...>
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
  </environmentVariables>
</aspNetCore>
```

### Hosting model: InProcess vs OutOfProcess

En `web.config`, `hostingModel="InProcess"` es más rápido porque la app corre dentro del proceso de IIS. Si tienes problemas de compatibilidad, cambia a `OutOfProcess`:

```xml
<aspNetCore processPath="dotnet"
            arguments=".\DeliveryApi.dll"
            hostingModel="OutOfProcess" />
```

Con OutOfProcess, la app corre en Kestrel y IIS actúa como proxy inverso.

### Archivos temporales de SQLite

SQLite crea estos archivos junto a `delivery.db`:
- `delivery.db-journal` (modo journal)
- `delivery.db-shm` y `delivery.db-wal` (modo WAL)

**No los borres** mientras la app esté corriendo. Se generan automáticamente.

### Backup de la base de datos

Antes de cada actualización, haz una copia de `delivery.db`:
```
copy C:\inetpub\DeliveryApi\delivery.db C:\inetpub\DeliveryApi\delivery_backup.db
```

---

> **Guía preparada para la asignatura Tecnología Web I (SIS-0150)**
> Publicación de Web API ASP.NET Core con SQLite y Scalar en IIS — Windows 10
