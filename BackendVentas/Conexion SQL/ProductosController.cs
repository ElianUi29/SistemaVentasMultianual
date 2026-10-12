using Microsoft.AspNetCore.Mvc;
using BackendVentas.Models;
using BackendVentas.Services;

namespace BackendVentas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly ProductoService _productoService;

        public ProductosController(ProductoService productoService)
        {
            _productoService = productoService;
        }

        // 1. Listar inventario
        [HttpGet("listar")]
        [HttpGet]
        public async Task<IActionResult> ListarProductos()
        {
            try
            {
                var lista = await _productoService.ListarProductosAsync();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al listar productos: {ex.Message}" });
            }
        }

        // 2. Obtener producto por ID (para editar)
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerProducto(int id)
        {
            try
            {
                var producto = await _productoService.ObtenerProductoAsync(id);
                if (producto == null)
                    return NotFound(new { mensaje = "Producto no encontrado." });

                return Ok(producto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        // 3. Registrar nuevo producto
        [HttpPost("guardar")]
        [HttpPost]
        public async Task<IActionResult> GuardarProducto([FromBody] ProductoModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre) || model.Precio <= 0)
                return BadRequest(new { mensaje = "El nombre y un precio válido son requeridos." });

            try
            {
                var (exito, mensaje) = await _productoService.GuardarProductoAsync(model);
                if (!exito)
                    return BadRequest(new { mensaje });

                return Ok(new { mensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        // 4. Actualizar producto existente
        [HttpPut("actualizar/{id}")]
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarProducto(int id, [FromBody] ProductoModel model)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(model.Nombre))
                return BadRequest(new { mensaje = "Datos inválidos." });

            try
            {
                bool actualizado = await _productoService.ActualizarProductoAsync(id, model);
                if (!actualizado)
                    return NotFound(new { mensaje = "Producto no encontrado." });

                return Ok(new { mensaje = "Producto actualizado con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        // 5. Eliminar producto físicamente
        [HttpDelete("eliminar/{id}")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            try
            {
                bool eliminado = await _productoService.EliminarProductoAsync(id);
                if (!eliminado)
                    return NotFound(new { mensaje = "Producto no encontrado." });

                return Ok(new { mensaje = "Producto eliminado con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
    }
}