using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;
using System.Data;

namespace BackendVentas.Controllers
{
    // Modelos para transferir datos de ventas
    public class DetalleVentaModel
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class VentaModel
    {
        public int IdCliente { get; set; }
        public int IdUsuario { get; set; } = 1;
        public string? NumeroComprobante { get; set; }
        public decimal Total { get; set; }
        public List<DetalleVentaModel> Detalles { get; set; } = new();
    }

    [ApiController]
    [Route("api/[controller]")]
    public class VentasController : ControllerBase
    {
        private readonly Conexion _conexion;

        public VentasController(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Obtener lista de ventas para la tabla principal
        [HttpGet("listar")]
        public async Task<IActionResult> ListarVentas()
        {
            var lista = new List<object>();

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    var query = @"
                        SELECT 
                            v.IdVenta,
                            v.NumeroComprobante,
                            v.FechaVenta,
                            c.Nombre AS Cliente,
                            v.Total
                        FROM Ventas v
                        INNER JOIN Clientes c ON v.IdCliente = c.IdCliente
                        ORDER BY v.IdVenta DESC";

                    using (var cmd = new SqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lista.Add(new
                            {
                                idVenta = Convert.ToInt32(reader["IdVenta"]),
                                numeroComprobante = reader["NumeroComprobante"]?.ToString() ?? string.Empty,
                                fechaVenta = reader["FechaVenta"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["FechaVenta"]).ToString("yyyy-MM-dd") 
                                    : "-",
                                cliente = reader["Cliente"]?.ToString() ?? "Consumidor Final",
                                total = Convert.ToDecimal(reader["Total"])
                            });
                        }
                    }
                }

                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener ventas: {ex.Message}");
            }
        }

        // 2. Obtener comprobante detallado para VER DETALLE e IMPRIMIR TICKET
        [HttpGet("detalle/{id}")]
        public async Task<IActionResult> ObtenerDetalleVenta(int id)
        {
            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    var query = @"
                        SELECT 
                            v.IdVenta,
                            v.NumeroComprobante,
                            v.FechaVenta,
                            v.Total,
                            c.Nombre AS Cliente,
                            c.Documento,
                            c.Telefono,
                            c.Direccion
                        FROM Ventas v
                        INNER JOIN Clientes c ON v.IdCliente = c.IdCliente
                        WHERE v.IdVenta = @id";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var ventaDetalle = new
                                {
                                    idVenta = Convert.ToInt32(reader["IdVenta"]),
                                    numeroComprobante = reader["NumeroComprobante"]?.ToString() ?? $"VNT-{id}",
                                    fechaVenta = reader["FechaVenta"] != DBNull.Value 
                                        ? Convert.ToDateTime(reader["FechaVenta"]).ToString("dd/MM/yyyy HH:mm") 
                                        : "-",
                                    total = Convert.ToDecimal(reader["Total"]),
                                    cliente = reader["Cliente"]?.ToString() ?? "Consumidor Final",
                                    documento = reader["Documento"]?.ToString() ?? "-",
                                    telefono = reader["Telefono"] == DBNull.Value ? "-" : reader["Telefono"]?.ToString(),
                                    direccion = reader["Direccion"] == DBNull.Value ? "-" : reader["Direccion"]?.ToString()
                                };

                                return Ok(ventaDetalle);
                            }
                        }
                    }
                }

                return NotFound($"No se encontró la venta con ID {id}.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener el detalle: {ex.Message}");
            }
        }

        // 3. Registrar venta con transacción (Cabecera + Detalle)
        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarVenta([FromBody] VentaModel venta)
        {
            if (venta == null || venta.IdCliente <= 0 || venta.Total <= 0)
            {
                return BadRequest("Datos de venta inválidos: se requiere un cliente válido y un total mayor a 0.");
            }

            int idUsuarioValido = venta.IdUsuario > 0 ? venta.IdUsuario : 1;

            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();
                using (var transaccion = con.BeginTransaction())
                {
                    try
                    {
                        // A. Insertar Cabecera de Venta
                        var queryVenta = @"
                            INSERT INTO Ventas (IdCliente, IdUsuario, NumeroComprobante, Total, FechaVenta)
                            OUTPUT INSERTED.IdVenta
                            VALUES (@idCliente, @idUsuario, @nroComprobante, @total, GETDATE())";

                        int idVentaGenerada;
                        using (var cmdVenta = new SqlCommand(queryVenta, con, transaccion))
                        {
                            cmdVenta.Parameters.AddWithValue("@idCliente", venta.IdCliente);
                            cmdVenta.Parameters.AddWithValue("@idUsuario", idUsuarioValido);
                            cmdVenta.Parameters.AddWithValue("@nroComprobante", string.IsNullOrWhiteSpace(venta.NumeroComprobante) 
                                ? $"VNT-{DateTime.Now:yyyyMMdd}-{DateTime.Now.Ticks % 1000}" 
                                : venta.NumeroComprobante.Trim());
                            cmdVenta.Parameters.AddWithValue("@total", venta.Total);

                            var scalarResult = await cmdVenta.ExecuteScalarAsync();
                            idVentaGenerada = Convert.ToInt32(scalarResult);
                        }

                        // B. Insertar Detalles si fueron enviados
                        if (venta.Detalles != null && venta.Detalles.Count > 0)
                        {
                            foreach (var item in venta.Detalles)
                            {
                                var queryDetalle = @"
                                    INSERT INTO DetalleVentas (IdVenta, IdProducto, Cantidad, Precio, Subtotal)
                                    VALUES (@idVenta, @idProd, @cant, @precio, @subtotal)";

                                using (var cmdDetalle = new SqlCommand(queryDetalle, con, transaccion))
                                {
                                    cmdDetalle.Parameters.AddWithValue("@idVenta", idVentaGenerada);
                                    cmdDetalle.Parameters.AddWithValue("@idProd", item.IdProducto);
                                    cmdDetalle.Parameters.AddWithValue("@cant", item.Cantidad);
                                    cmdDetalle.Parameters.AddWithValue("@precio", item.Precio);
                                    cmdDetalle.Parameters.AddWithValue("@subtotal", item.Subtotal);

                                    await cmdDetalle.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        await transaccion.CommitAsync();
                        return Ok(new { mensaje = "Venta registrada con éxito", idVenta = idVentaGenerada });
                    }
                    catch (SqlException sqlEx)
                    {
                        await transaccion.RollbackAsync();
                        return StatusCode(500, $"Error en base de datos al registrar venta: {sqlEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        await transaccion.RollbackAsync();
                        return StatusCode(500, $"Error inesperado al registrar venta: {ex.Message}");
                    }
                }
            }
        }
    }
}