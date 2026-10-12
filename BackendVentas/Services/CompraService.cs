using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;
using BackendVentas.Models;

namespace BackendVentas.Services
{
    public class CompraService
    {
        private readonly Conexion _conexion;

        public CompraService(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Listar todas las compras
        public async Task<List<CompraItemDto>> ListarComprasAsync()
        {
            var lista = new List<CompraItemDto>();

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
                        lista.Add(new CompraItemDto
                        {
                            IdCompra = Convert.ToInt32(reader["IdCompra"]),
                            NumeroOC = reader["NumeroOC"]?.ToString() ?? string.Empty,
                            FechaCompra = reader["FechaCompra"] != DBNull.Value 
                                ? Convert.ToDateTime(reader["FechaCompra"]).ToString("yyyy-MM-dd") 
                                : "-",
                            Proveedor = reader["Proveedor"]?.ToString() ?? "Proveedor General",
                            TotalCosto = Convert.ToDecimal(reader["TotalCosto"]),
                            EstadoRecepcion = reader["EstadoRecepcion"]?.ToString() ?? "Recibido"
                        });
                    }
                }
            }

            return lista;
        }

        // 2. Obtener detalle de compra por ID
        public async Task<CompraDetalleDto?> ObtenerDetalleCompraAsync(int id)
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
                            return new CompraDetalleDto
                            {
                                IdCompra = Convert.ToInt32(reader["IdCompra"]),
                                NumeroOC = reader["NumeroOC"]?.ToString() ?? $"OC-{id}",
                                FechaCompra = reader["FechaCompra"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["FechaCompra"]).ToString("yyyy-MM-dd") 
                                    : "-",
                                TotalCosto = Convert.ToDecimal(reader["TotalCosto"]),
                                EstadoRecepcion = reader["EstadoRecepcion"]?.ToString() ?? "Recibido",
                                Proveedor = reader["Proveedor"]?.ToString() ?? "-",
                                RucProveedor = reader["RucProveedor"]?.ToString() ?? "-",
                                Telefono = reader["Telefono"]?.ToString() ?? "-",
                                Direccion = reader["Direccion"]?.ToString() ?? "-"
                            };
                        }
                    }
                }
            }

            return null;
        }

        // 3. Registrar una nueva orden de compra
        public async Task GuardarCompraAsync(CompraModel compra)
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
        }
    }
}