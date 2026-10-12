using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;

namespace BackendVentas.Controllers
{
    public class CompraModel
    {
        public int IdProveedor { get; set; }
        public int IdUsuario { get; set; } = 1;
        public string? NumeroOC { get; set; }
        public decimal TotalCosto { get; set; }
        public string EstadoRecepcion { get; set; } = "Recibido";
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ComprasController : ControllerBase
    {
        private readonly Conexion _conexion;

        public ComprasController(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Listar todas las compras
        [HttpGet("listar")]
        public async Task<IActionResult> ListarCompras()
        {
            var lista = new List<object>();

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    var query = @"
                        SELECT 
                            c.IdCompra,
                            c.NumeroOC,
                            c.FechaCompra,
                            p.RazonSocial AS Proveedor,
                            c.TotalCosto,
                            c.EstadoRecepcion
                        FROM Compras c
                        INNER JOIN Proveedores p ON c.IdProveedor = p.IdProveedor
                        ORDER BY c.IdCompra DESC";

                    using (var cmd = new SqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lista.Add(new
                            {
                                idCompra = Convert.ToInt32(reader["IdCompra"]),
                                numeroOC = reader["NumeroOC"]?.ToString() ?? "",
                                fechaCompra = reader["FechaCompra"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["FechaCompra"]).ToString("yyyy-MM-dd") 
                                    : "-",
                                proveedor = reader["Proveedor"]?.ToString() ?? "Proveedor General",
                                totalCosto = Convert.ToDecimal(reader["TotalCosto"]),
                                estadoRecepcion = reader["EstadoRecepcion"]?.ToString() ?? "Recibido"
                            });
                        }
                    }
                }

                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener compras: {ex.Message}");
            }
        }

        // 2. Obtener datos detallados de una compra específica para el PDF
        [HttpGet("detalle/{id}")]
        public async Task<IActionResult> ObtenerDetalleCompra(int id)
        {
            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    var query = @"
                        SELECT 
                            c.IdCompra,
                            c.NumeroOC,
                            c.FechaCompra,
                            c.TotalCosto,
                            c.EstadoRecepcion,
                            p.RazonSocial AS Proveedor,
                            p.Documento AS RucProveedor,
                            p.Telefono,
                            p.Direccion
                        FROM Compras c
                        INNER JOIN Proveedores p ON c.IdProveedor = p.IdProveedor
                        WHERE c.IdCompra = @id";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var detalle = new
                                {
                                    idCompra = Convert.ToInt32(reader["IdCompra"]),
                                    numeroOC = reader["NumeroOC"]?.ToString() ?? $"OC-{id}",
                                    fechaCompra = reader["FechaCompra"] != DBNull.Value 
                                        ? Convert.ToDateTime(reader["FechaCompra"]).ToString("yyyy-MM-dd") 
                                        : "-",
                                    totalCosto = Convert.ToDecimal(reader["TotalCosto"]),
                                    estadoRecepcion = reader["EstadoRecepcion"]?.ToString() ?? "Recibido",
                                    proveedor = reader["Proveedor"]?.ToString() ?? "-",
                                    rucProveedor = reader["RucProveedor"]?.ToString() ?? "-",
                                    telefono = reader["Telefono"]?.ToString() ?? "-",
                                    direccion = reader["Direccion"]?.ToString() ?? "-"
                                };

                                return Ok(detalle);
                            }
                        }
                    }
                }

                return NotFound("Compra no encontrada.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener compra: {ex.Message}");
            }
        }

        // 3. Registrar una nueva Orden de Compra
        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarCompra([FromBody] CompraModel compra)
        {
            if (compra == null || compra.IdProveedor <= 0 || compra.TotalCosto <= 0)
            {
                return BadRequest("Datos de compra inválidos.");
            }

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    var query = @"
                        INSERT INTO Compras (IdProveedor, IdUsuario, NumeroOC, TotalCosto, EstadoRecepcion, FechaCompra)
                        VALUES (@idProv, @idUsu, @oc, @total, @estado, GETDATE())";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@idProv", compra.IdProveedor);
                        cmd.Parameters.AddWithValue("@idUsu", compra.IdUsuario > 0 ? compra.IdUsuario : 1);
                        cmd.Parameters.AddWithValue("@oc", string.IsNullOrWhiteSpace(compra.NumeroOC) 
                            ? $"OC-{DateTime.Now:yyyy}-{DateTime.Now.Ticks % 1000:000}" 
                            : compra.NumeroOC.Trim());
                        cmd.Parameters.AddWithValue("@total", compra.TotalCosto);
                        cmd.Parameters.AddWithValue("@estado", string.IsNullOrWhiteSpace(compra.EstadoRecepcion) ? "Recibido" : compra.EstadoRecepcion);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { mensaje = "Orden de compra registrada exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar compra: {ex.Message}");
            }
        }
    }
}