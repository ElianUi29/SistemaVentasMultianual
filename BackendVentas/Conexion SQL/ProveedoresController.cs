using Microsoft.AspNetCore.Mvc;
using BackendVentas.Models;
using BackendVentas.Services;

namespace BackendVentas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProveedoresController : ControllerBase
    {
        private readonly ProveedorService _proveedorService;

        public ProveedoresController(ProveedorService proveedorService)
        {
            _proveedorService = proveedorService;
        }

        // 1. Listar todos los proveedores
        [HttpGet("listar")]
        [HttpGet]
        public async Task<IActionResult> ListarProveedores()
        {
            try
            {
                var lista = await _proveedorService.ListarProveedoresAsync();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al listar proveedores: {ex.Message}" });
            }
        }

        // 2. Registrar nuevo proveedor
        [HttpPost("guardar")]
        [HttpPost]
        public async Task<IActionResult> GuardarProveedor([FromBody] ProveedorModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Documento) || string.IsNullOrWhiteSpace(model.RazonSocial))
                return BadRequest(new { mensaje = "El Documento/RUC y la Razón Social son requeridos." });

            try
            {
                var (exito, mensaje) = await _proveedorService.GuardarProveedorAsync(model);
                if (!exito)
                    return BadRequest(new { mensaje });

                return Ok(new { mensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al guardar proveedor: {ex.Message}" });
            }
        }

        // 3. Eliminación física
        [HttpDelete("eliminar/{id}")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProveedor(int id)
        {
            if (id <= 0) 
                return BadRequest(new { mensaje = "Identificador de proveedor inválido." });

            try
            {
                var (exito, mensaje) = await _proveedorService.EliminarProveedorAsync(id);
                if (!exito)
                    return NotFound(new { mensaje });

                return Ok(new { mensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al eliminar proveedor: {ex.Message}" });
            }
        }
    }
}