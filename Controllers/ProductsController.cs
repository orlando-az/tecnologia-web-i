using DeliveryApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private static List<Product> _products = new List<Product>
        {
            new Product {Id=1, Name="Pizza", Description="Pizza Familiar", 
            Category="Pizza", Price=80.5m ,IsAvailable=true},
            new Product {Id=2, Name="Hamburguesa", Description="Hamburguesa Especial", 
            Category="Hamburguesa", Price=40.5m ,IsAvailable=true},
            new Product {Id=3, Name="Pollo", Description="Pollo a la canasta", 
            Category="Pollo", Price=20m ,IsAvailable=true},
            new Product {Id=4, Name="Coca-Cola", Description="Coca-Cola 500ml", 
            Category="Bebida", Price=6m ,IsAvailable=true}
        };

        [HttpGet]
        public ActionResult<IEnumerable<Product>> GetAll()
        {
            return Ok(_products);
        }

        [HttpGet("{id}")]
        public ActionResult<Product> GetById(int id)
        {
            var product = _products.FirstOrDefault(p=> p.Id==id);
            if(product is null)
                return NotFound();
            return Ok(product);
        }

        [HttpPost]
        public ActionResult<Product> Create([FromBody] Product product)
        {
            product.Id = _products.Any() ? _products.Max(p=>p.Id)+1 : 1;

            if (String.IsNullOrEmpty(product.Name))
                return BadRequest("El nombre de producto no puede estar vacio");

            if(String.IsNullOrEmpty(product.Category))
                return BadRequest("La categoria no puede estar vacia");

            _products.Add(product);
            //return Ok(product);
            return CreatedAtAction(nameof(GetById),new {id=product.Id},product);
        }

        [HttpPut("{id}")]
        public ActionResult<Product> Update(int id, Product updateProduct)
        {
            var product = _products.FirstOrDefault(p=> p.Id==id);

            if(product is null)
                return NotFound();

            if (String.IsNullOrEmpty(product.Name))
                return BadRequest("El nombre de producto no puede estar vacio");

            if(String.IsNullOrEmpty(product.Category))
                return BadRequest("La categoria no puede estar vacia");
                
            product.Name= updateProduct.Name;
            product.Category= updateProduct.Category;
            product.Description= updateProduct.Description;
            product.Price= updateProduct.Price;

            return Ok(product);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var product = _products.FirstOrDefault(p=>p.Id==id);
            if(product is null)
                return NotFound();
            
            _products.Remove(product);
            return NoContent();
        }
    }
}
