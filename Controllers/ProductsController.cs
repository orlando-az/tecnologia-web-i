using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {

        private readonly AppDbContext _context;
        public ProductsController(AppDbContext context)
        {
            _context=context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetById(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p=> p.Id==id);
            if(product is null)
                return NotFound();
            return Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> Create([FromBody] Product product)
        {
           // product.Id = _products.Any() ? _products.Max(p=>p.Id)+1 : 1;

            if (String.IsNullOrEmpty(product.Name))
                return BadRequest("El nombre de producto no puede estar vacio");

            if(String.IsNullOrEmpty(product.Category))
                return BadRequest("La categoria no puede estar vacia");

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            //return Ok(product);
            return CreatedAtAction(nameof(GetById),new {id=product.Id},product);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> Update(int id, Product updateProduct)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p=> p.Id==id);

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

            await _context.SaveChangesAsync();

            return Ok(product);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p=>p.Id==id);
            if(product is null)
                return NotFound();
            
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        
    }
}
