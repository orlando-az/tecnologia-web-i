# 🛵 DeliveryApi — Web API REST con C# y VSCode

Guía práctica para crear una Web API de pedidos delivery con operaciones **GET · GET by ID · POST · PUT · DELETE** usando datos estáticos en memoria.

---

## 📋 Prerrequisitos

| Herramienta              | Versión recomendada | Descarga                                               |
| ------------------------ | ------------------- | ------------------------------------------------------ |
| .NET SDK                 | 9.0 o superior      | [dotnet.microsoft.com](https://dotnet.microsoft.com)   |
| Visual Studio Code       | Última versión      | [code.visualstudio.com](https://code.visualstudio.com) |
| Extensión C#             | C# Dev Kit          | Marketplace de VSCode                                  |
| REST Client _(opcional)_ | by Huachao Mao      | Marketplace de VSCode                                  |

> **Verificar instalación:**
>
> ```bash
> dotnet --version
> ```

---

## 1. Crear el Proyecto

```bash
# Crear carpeta del proyecto
mkdir DeliveryApi
cd DeliveryApi

# Crear el proyecto Web API
dotnet new webapi -n DeliveryApi

# Abrir en VSCode
code .
```

### Estructura de archivos

```
DeliveryApi/
├── Controllers/
│   └── WeatherForecastController.cs  ← puedes eliminar este
├── Models/
├── Properties/
│   └── launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── DeliveryApi.csproj
└── Program.cs
```

---

## 2. Crear el Modelo

Crea la carpeta `Models` y dentro el archivo `Product.cs`:

```csharp
// Models/Product.cs
namespace DeliveryApi.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Ej: Hamburguesas, Bebidas, Postres
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
```

---

## 3. Crear el Controlador

Crea el archivo `ProductsController.cs` dentro de la carpeta `Controllers`:

```csharp
// Controllers/ProductsController.cs
using Microsoft.AspNetCore.Mvc;
using DeliveryApi.Models;

namespace DeliveryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private static List<Product> _products = new List<Product>
        {
            new Product { Id = 1, Name = "Hamburguesa Clásica",
                          Description = "Carne, lechuga, tomate y queso",
                          Category = "Hamburguesas", Price = 8.99m, IsAvailable = true },
            new Product { Id = 2, Name = "Pizza Pepperoni",
                          Description = "Pizza mediana con pepperoni",
                          Category = "Pizzas", Price = 12.50m, IsAvailable = true },
            new Product { Id = 3, Name = "Coca-Cola 500ml",
                          Description = "Bebida gaseosa fría",
                          Category = "Bebidas", Price = 2.50m, IsAvailable = true },
        };

        // Los endpoints se agregan aquí...
    }
}
```

---

## 4. Endpoints

### `GET /api/products` — Ver todos los productos

```csharp
[HttpGet]
public ActionResult<IEnumerable<Product>> GetAll()
{
    return Ok(_products);
}
```

**Respuesta 200 OK:**

```json
[
  {
    "id": 1,
    "name": "Hamburguesa Clásica",
    "description": "Carne, lechuga, tomate y queso",
    "category": "Hamburguesas",
    "price": 8.99,
    "isAvailable": true
  },
  {
    "id": 2,
    "name": "Pizza Pepperoni",
    "description": "Pizza mediana con pepperoni",
    "category": "Pizzas",
    "price": 12.5,
    "isAvailable": true
  },
  {
    "id": 3,
    "name": "Coca-Cola 500ml",
    "description": "Bebida gaseosa fría",
    "category": "Bebidas",
    "price": 2.5,
    "isAvailable": true
  }
]
```

---

### `GET /api/products/{id}` — Ver un producto por ID

```csharp
[HttpGet("{id}")]
public ActionResult<Product> GetById(int id)
{
    var product = _products.FirstOrDefault(p => p.Id == id);

    if (product == null)
        return NotFound(new { message = $"Producto con Id {id} no encontrado." });

    return Ok(product);
}
```

**Respuesta 200 OK** para `GET /api/products/1`:

```json
{
  "id": 1,
  "name": "Hamburguesa Clásica",
  "description": "Carne, lechuga, tomate y queso",
  "category": "Hamburguesas",
  "price": 8.99,
  "isAvailable": true
}
```

**Respuesta 404 Not Found:**

```json
{ "message": "Producto con Id 99 no encontrado." }
```

---

### `POST /api/products` — Agregar un producto al menú

```csharp
[HttpPost]
public ActionResult<Product> Create([FromBody] Product product)
{
    product.Id = _products.Any() ? _products.Max(p => p.Id) + 1 : 1;
    _products.Add(product);
    return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
}
```

**Body de la petición:**

```json
{
  "name": "Papas Fritas",
  "description": "Papas crujientes con sal",
  "category": "Acompañamientos",
  "price": 3.5,
  "isAvailable": true
}
```

**Respuesta 201 Created:**

```json
{
  "id": 4,
  "name": "Papas Fritas",
  "description": "Papas crujientes con sal",
  "category": "Acompañamientos",
  "price": 3.5,
  "isAvailable": true
}
```

---

### `PUT /api/products/{id}` — Actualizar un producto

```csharp
[HttpPut("{id}")]
public ActionResult<Product> Update(int id, [FromBody] Product updatedProduct)
{
    var index = _products.FindIndex(p => p.Id == id);

    if (index == -1)
        return NotFound(new { message = $"Producto con Id {id} no encontrado." });

    updatedProduct.Id = id;
    _products[index] = updatedProduct;

    return Ok(updatedProduct);
}
```

**Body de la petición** para `PUT /api/products/1`:

```json
{
  "name": "Hamburguesa Doble",
  "description": "Doble carne, lechuga, tomate y queso",
  "category": "Hamburguesas",
  "price": 11.99,
  "isAvailable": true
}
```

---

### `DELETE /api/products/{id}` — Quitar un producto del menú

```csharp
[HttpDelete("{id}")]
public ActionResult Delete(int id)
{
    var product = _products.FirstOrDefault(p => p.Id == id);

    if (product == null)
        return NotFound(new { message = $"Producto con Id {id} no encontrado." });

    _products.Remove(product);
    return NoContent(); // 204 No Content
}
```

> **Nota:** `204 No Content` es el código estándar para DELETE exitoso. No retorna cuerpo en la respuesta.

---

## 5. Controlador Completo

```csharp
using Microsoft.AspNetCore.Mvc;
using DeliveryApi.Models;

namespace DeliveryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private static List<Product> _products = new List<Product>
        {
            new Product { Id = 1, Name = "Hamburguesa Clásica", Description = "Carne, lechuga, tomate y queso",
                          Category = "Hamburguesas", Price = 8.99m, IsAvailable = true },
            new Product { Id = 2, Name = "Pizza Pepperoni", Description = "Pizza mediana con pepperoni",
                          Category = "Pizzas", Price = 12.50m, IsAvailable = true },
            new Product { Id = 3, Name = "Coca-Cola 500ml", Description = "Bebida gaseosa fría",
                          Category = "Bebidas", Price = 2.50m, IsAvailable = true },
        };

        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetAll()
            => Ok(_products);

        [HttpGet("{id}")]
        public ActionResult<Product> GetById(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = $"Producto con Id {id} no encontrado." });
            return Ok(product);
        }

        [HttpPost]
        public ActionResult<Product> Create([FromBody] Product product)
        {
            product.Id = _products.Any() ? _products.Max(p => p.Id) + 1 : 1;
            _products.Add(product);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpPut("{id}")]
        public ActionResult<Product> Update(int id, [FromBody] Product updatedProduct)
        {
            var index = _products.FindIndex(p => p.Id == id);
            if (index == -1)
                return NotFound(new { message = $"Producto con Id {id} no encontrado." });
            updatedProduct.Id = id;
            _products[index] = updatedProduct;
            return Ok(updatedProduct);
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return NotFound(new { message = $"Producto con Id {id} no encontrado." });
            _products.Remove(product);
            return NoContent();
        }
    }
}
```

---

## 6. Configurar `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 7. Ejecutar y Probar

### Iniciar la API

```bash
dotnet run
```

La API estará disponible en `https://localhost:5001` o `http://localhost:5000`.

### Probar con archivo `.http` (REST Client para VSCode)

Crea un archivo `requests.http` en la raíz del proyecto:

```http
### GET - Ver menú completo
GET https://localhost:5001/api/products
Content-Type: application/json

###

### GET by ID - Ver un producto
GET https://localhost:5001/api/products/1
Content-Type: application/json

###

### POST - Agregar producto al menú
POST https://localhost:5001/api/products
Content-Type: application/json

{
  "name": "Papas Fritas",
  "description": "Papas crujientes con sal",
  "category": "Acompañamientos",
  "price": 3.50,
  "isAvailable": true
}

###

### PUT - Actualizar producto
PUT https://localhost:5001/api/products/1
Content-Type: application/json

{
  "name": "Hamburguesa Doble",
  "description": "Doble carne, lechuga, tomate y queso",
  "category": "Hamburguesas",
  "price": 11.99,
  "isAvailable": true
}

###

### DELETE - Quitar producto del menú
DELETE https://localhost:5001/api/products/3
```

### Referencia rápida de endpoints

| Método   | Ruta                 | Código éxito     | Descripción              |
| -------- | -------------------- | ---------------- | ------------------------ |
| `GET`    | `/api/products`      | `200 OK`         | Ver menú completo        |
| `GET`    | `/api/products/{id}` | `200 OK`         | Ver un producto          |
| `POST`   | `/api/products`      | `201 Created`    | Agregar producto al menú |
| `PUT`    | `/api/products/{id}` | `200 OK`         | Actualizar producto      |
| `DELETE` | `/api/products/{id}` | `204 No Content` | Quitar producto del menú |

---

## 8. Consejos Finales

1. **Datos temporales:** la lista en memoria se reinicia cada vez que se reinicia la app. Para persistencia real, usa Entity Framework Core con SQLite o SQL Server.
2. **Validaciones:** agrega `[Required]`, `[Range]`, `[StringLength]` en el modelo para mayor robustez.
3. **Swagger:** en .NET 9 no se incluye por defecto. Para activarlo, agrega el paquete `Swashbuckle.AspNetCore` y configura `AddSwaggerGen()` en `Program.cs`.
4. **CORS:** usa `app.UseCors()` si consumes la API desde un frontend en otro origen (React, Angular, etc.).
5. **Siguiente paso:** reemplaza la lista estática por un repositorio con inyección de dependencias para una arquitectura más limpia.

---

> ¡Tu DeliveryApi con C# y VSCode está lista para usar! 🛵🎉
