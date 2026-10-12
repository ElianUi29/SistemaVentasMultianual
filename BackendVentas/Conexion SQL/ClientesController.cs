using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;

namespace BackendVentas.Controllers
{
    // Modelo fuertemente tipado para el cliente
    public class ClienteModel
    {
        public int IdCliente { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly Conexion _conexion;

        public ClientesController(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Obtener lista de clientes activos
        [HttpGet("listar")]
        public async Task<IActionResult> ListarClientes()
        {
            var lista = new List<ClienteModel>();

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    var query = "SELECT IdCliente, Documento, Nombre, Telefono, Direccion FROM Clientes WHERE Estado = 1";

                    using (var cmd = new SqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lista.Add(new ClienteModel
                            {
                                IdCliente = Convert.ToInt32(reader["IdCliente"]),
                                Documento = reader["Documento"].ToString() ?? string.Empty,
                                Nombre = reader["Nombre"].ToString() ?? string.Empty,
                                // Manejo seguro de valores NULL de base de datos
                                Telefono = reader["Telefono"] == DBNull.Value ? null : reader["Telefono"].ToString(),
                                Direccion = reader["Direccion"] == DBNull.Value ? null : reader["Direccion"].ToString()
                            });
                        }
                    }
                }

                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al consultar la base de datos: {ex.Message}");
            }
        }

        // 2. Guardar un nuevo cliente en SQL Server
        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarCliente([FromBody] ClienteModel cliente)
        {
            if (cliente == null || string.IsNullOrWhiteSpace(cliente.Documento) || string.IsNullOrWhiteSpace(cliente.Nombre))
            {
                return BadRequest("El documento y el nombre son obligatorios.");
            }

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    var query = @"INSERT INTO Clientes (Documento, Nombre, Telefono, Direccion, Estado) 
                                  VALUES (@doc, @nom, @tel, @dir, 1)";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@doc", cliente.Documento.Trim());
                        cmd.Parameters.AddWithValue("@nom", cliente.Nombre.Trim());
                        cmd.Parameters.AddWithValue("@tel", (object?)cliente.Telefono?.Trim() ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@dir", (object?)cliente.Direccion?.Trim() ?? DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { mensaje = "Cliente registrado con éxito en SQL Server" });
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                // Error cuando el Documento ya existe por la restricción UNIQUE
                return Conflict($"El documento '{cliente.Documento}' ya se encuentra registrado en el sistema.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar el cliente: {ex.Message}");
            }
        }
    }
}