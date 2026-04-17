# Guía de Laboratorio — Sesión 2
## Tecnología Web I · DeliveryApi con C# y ASP.NET Core

> **Prerrequisito:** Proyecto `DeliveryApi` con CRUD en memoria funcional

---

## Objetivos de aprendizaje

Al finalizar esta sesión, el estudiante será capaz de:

1. Configurar **Entity Framework Core** con SQLite para persistir datos en una base de datos relacional.
2. Integrar **Scalar UI** como herramienta de documentación interactiva de la API.

---

## Parte 1 — Entity Framework Core con SQLite

### 1.1 Introducción

Hasta el momento, los datos del proyecto `DeliveryApi` se almacenan en una lista estática en memoria (`List<Product>`). Esto significa que cada vez que se reinicia la aplicación, todos los cambios se pierden. Para solucionar este problema, se utilizará **Entity Framework Core (EF Core)**, el ORM (Object-Relational Mapper) oficial de .NET, junto con **SQLite** como motor de base de datos.

### 1.2 Instalación de paquetes

Ejecutar los siguientes comandos en la terminal, dentro de la carpeta del proyecto:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

Verificar que los paquetes fueron agregados correctamente en el archivo `DeliveryApi.csproj`.

### 1.3 Crear el contexto de base de datos

Crear la carpeta `Data/` en la raíz del proyecto y dentro de ella el archivo `AppDbContext.cs`:

```csharp
// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Models;

namespace DeliveryApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Product> Products { get; set; }
    }
}
```

> **Nota:** `DbSet<Product>` representa la tabla `Products` en la base de datos. EF Core se encargará de crear y gestionar dicha tabla.

### 1.4 Registrar el contexto en `Program.cs`

Modificar el archivo `Program.cs` para registrar el contexto como un servicio de la aplicación:

```csharp
using DeliveryApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Registrar EF Core con SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=delivery.db"));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 1.5 Crear y aplicar la migración inicial

Las **migraciones** son el mecanismo de EF Core para traducir los modelos de C# en estructuras de base de datos. Ejecutar:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Tras ejecutar estos comandos, se generará:
- La carpeta `Migrations/` con el código de la migración.
- El archivo `delivery.db` en la raíz del proyecto (la base de datos SQLite).

### 1.6 Actualizar el controlador

Reemplazar la lista estática `_products` por la inyección del contexto `AppDbContext`. A continuación se presenta el controlador actualizado:

```csharp
// Controllers/ProductsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryApi.Data;
using DeliveryApi.Models;

namespace DeliveryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll()
            => Ok(await _context.Products.ToListAsync());

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = $"Producto con Id {id} no encontrado." });
            return Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> Create([FromBody] Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> Update(int id, [FromBody] Product updatedProduct)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = $"Producto con Id {id} no encontrado." });

            product.Name        = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Category    = updatedProduct.Category;
            product.Price       = updatedProduct.Price;
            product.IsAvailable = updatedProduct.IsAvailable;

            await _context.SaveChangesAsync();
            return Ok(product);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = $"Producto con Id {id} no encontrado." });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
```

> **Importante:** Observar el uso de `async/await` y `SaveChangesAsync()`. Las operaciones de base de datos son asíncronas para no bloquear el hilo principal de la aplicación.

### 1.7 Verificación

Ejecutar la aplicación con `dotnet run` y probar los endpoints con el archivo `requests.http`. Reiniciar la aplicación y verificar que los datos creados anteriormente persisten.

---

## Parte 2 — Documentación interactiva con Scalar UI

### 2.1 Introducción

**Scalar** es una herramienta moderna de documentación de APIs que genera una interfaz visual e interactiva a partir del estándar **OpenAPI**. A diferencia de Swagger UI, Scalar se integra nativamente con `.NET 9` a través del middleware `AddOpenApi()`, sin dependencias externas adicionales.

### 2.2 Instalación

```bash
dotnet add package Scalar.AspNetCore
```

### 2.3 Configurar `Program.cs`

Actualizar el archivo `Program.cs` con la configuración de Scalar:

```csharp
using DeliveryApi.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi(); // Genera el documento OpenAPI

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=delivery.db"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();              // Expone el JSON en /openapi/v1.json
    app.MapScalarApiReference();   // UI interactiva en /scalar/v1
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 2.4 Acceder a la documentación

Con la aplicación en ejecución (`dotnet run`), ingresar a:

```
https://localhost:{puerto}/scalar/v1
```

Desde esta interfaz es posible explorar y probar todos los endpoints de la API directamente en el navegador.

---

## Estructura final del proyecto

```
DeliveryApi/
├── Controllers/
│   └── ProductsController.cs
├── Data/
│   └── AppDbContext.cs
├── Migrations/
│   └── (archivos generados automáticamente)
├── Models/
│   └── Product.cs
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── delivery.db               ← base de datos SQLite
├── DeliveryApi.csproj
├── Program.cs
└── requests.http
```

---

## Resumen de comandos

| Comando | Descripción |
|---|---|
| `dotnet add package Microsoft.EntityFrameworkCore.Sqlite` | Instalar EF Core para SQLite |
| `dotnet add package Microsoft.EntityFrameworkCore.Design` | Herramientas de diseño de EF Core |
| `dotnet ef migrations add InitialCreate` | Crear la migración inicial |
| `dotnet ef database update` | Aplicar migraciones a la base de datos |
| `dotnet add package Scalar.AspNetCore` | Instalar Scalar UI |
| `dotnet run` | Ejecutar la aplicación |

---

## Lista de verificación

Al finalizar la sesión, el estudiante debe poder confirmar los siguientes puntos:

- [ ] Los datos persisten al reiniciar la aplicación.
- [ ] La interfaz de Scalar es accesible en `/scalar/v1`.
- [ ] Todos los endpoints aparecen documentados en Scalar con sus códigos de respuesta.

---

*Tecnología Web I — Guía de Laboratorio · Sesión 1*
