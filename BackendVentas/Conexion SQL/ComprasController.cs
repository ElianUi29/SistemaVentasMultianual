using Microsoft.AspNetCore.Mvc;
using BackendVentas.Models;
using BackendVentas.Services;

namespace BackendVentas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComprasController : ControllerBase
    {
        private readonly CompraService _compraService;

        public ComprasController(CompraService compraService)
        {
            _compraService = compraService;
        }

        // 1. Listar todas las compras
        [HttpGet("listar")]
        [HttpGet]
        public async Task<IActionResult> ListarCompras()
        {
            try
            {
                var lista = await _compraService.ListarComprasAsync();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al obtener compras: {ex.Message}" });
            }
        }

        // 2. Obtener datos detallados de una compra
        [HttpGet("detalle/{id}")]
        public async Task<IActionResult> ObtenerDetalleCompra(int id)
        {
            try
            {
                var detalle = await _compraService.ObtenerDetalleCompraAsync(id);
                if (detalle == null)
                {
                    return NotFound(new { mensaje = "Compra no encontrada." });
                }

                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al obtener compra: {ex.Message}" });
            }
        }

        // 3. Registrar una nueva Orden de Compra
        [HttpPost("guardar")]
        [HttpPost]
        public async Task<IActionResult> GuardarCompra([FromBody] CompraModel compra)
        {
            if (compra == null || compra.IdProveedor <= 0 || compra.TotalCosto <= 0)
            {
                return BadRequest(new { mensaje = "Datos de compra inválidos." });
            }

            try
            {
                await _compraService.GuardarCompraAsync(compra);
                return Ok(new { mensaje = "Orden de compra registrada exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al guardar compra: {ex.Message}" });
            }
        }
    }
}