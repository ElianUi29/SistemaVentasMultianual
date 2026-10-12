using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using BackendVentas.Models;
using BackendVentas.Services;

namespace BackendVentas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ClienteService _clienteService;

        public ClientesController(ClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        // 1. Obtener lista de clientes activos
        [HttpGet("listar")]
        [HttpGet] // Permite responder tanto en /api/Clientes como en /api/Clientes/listar
        public async Task<IActionResult> ListarClientes()
        {
            try
            {
                var lista = await _clienteService.ListarClientesAsync();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error interno al consultar la base de datos: {ex.Message}" });
            }
        }

        // 2. Guardar un nuevo cliente en SQL Server
        [HttpPost("guardar")]
        [HttpPost] // Permite responder tanto en /api/Clientes como en /api/Clientes/guardar
        public async Task<IActionResult> GuardarCliente([FromBody] ClienteModel cliente)
        {
            if (cliente == null || string.IsNullOrWhiteSpace(cliente.Documento) || string.IsNullOrWhiteSpace(cliente.Nombre))
            {
                return BadRequest(new { mensaje = "El documento y el nombre son obligatorios." });
            }

            try
            {
                await _clienteService.GuardarClienteAsync(cliente);
                return Ok(new { mensaje = "Cliente registrado con éxito en SQL Server" });
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                return Conflict(new { mensaje = $"El documento '{cliente.Documento}' ya se encuentra registrado en el sistema." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al guardar el cliente: {ex.Message}" });
            }
        }
    }
}