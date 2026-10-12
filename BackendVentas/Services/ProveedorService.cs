using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;
using BackendVentas.Models;

namespace BackendVentas.Services
{
    public class ProveedorService
    {
        private readonly Conexion _conexion;

        public ProveedorService(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Listar todos los proveedores
        public async Task<List<ProveedorModel>> ListarProveedoresAsync()
        {
            var lista = new List<ProveedorModel>();

            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();
                var query = @"
                    SELECT IdProveedor, Documento, RazonSocial, Telefono, Direccion, Estado 
                    FROM Proveedores 
                    ORDER BY IdProveedor DESC";

                using (var cmd = new SqlCommand(query, con))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lista.Add(new ProveedorModel
                        {
                            IdProveedor = Convert.ToInt32(reader["IdProveedor"]),
                            Documento = reader["Documento"].ToString() ?? string.Empty,
                            RazonSocial = reader["RazonSocial"].ToString() ?? string.Empty,
                            Telefono = reader["Telefono"] != DBNull.Value ? reader["Telefono"].ToString() : "-",
                            Direccion = reader["Direccion"] != DBNull.Value ? reader["Direccion"].ToString() : "-",
                            Estado = reader["Estado"] != DBNull.Value && Convert.ToBoolean(reader["Estado"])
                        });
                    }
                }
            }

            return lista;
        }

        // 2. Registrar nuevo proveedor
        public async Task<(bool Exito, string Mensaje)> GuardarProveedorAsync(ProveedorModel model)
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();

                // Validar si ya existe el documento
                var checkQuery = "SELECT COUNT(1) FROM Proveedores WHERE Documento = @doc";
                using (var checkCmd = new SqlCommand(checkQuery, con))
                {
                    checkCmd.Parameters.AddWithValue("@doc", model.Documento.Trim());
                    int existe = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (existe > 0)
                        return (false, "Ya existe un proveedor registrado con ese RUC o documento.");
                }

                var insertQuery = @"
                    INSERT INTO Proveedores (Documento, RazonSocial, Telefono, Direccion, Estado)
                    VALUES (@doc, @razon, @tel, @dir, 1)";

                using (var cmd = new SqlCommand(insertQuery, con))
                {
                    cmd.Parameters.AddWithValue("@doc", model.Documento.Trim());
                    cmd.Parameters.AddWithValue("@razon", model.RazonSocial.Trim());
                    cmd.Parameters.AddWithValue("@tel", string.IsNullOrWhiteSpace(model.Telefono) ? DBNull.Value : model.Telefono.Trim());
                    cmd.Parameters.AddWithValue("@dir", string.IsNullOrWhiteSpace(model.Direccion) ? DBNull.Value : model.Direccion.Trim());

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return (true, "Proveedor registrado correctamente.");
        }

        // 3. Eliminación física con transacción
        public async Task<(bool Exito, string Mensaje)> EliminarProveedorAsync(int id)
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();

                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        // A. Borrar compras asociadas para proteger integridad referencial
                        var deleteCompras = "DELETE FROM Compras WHERE IdProveedor = @id";
                        using (var cmdCompras = new SqlCommand(deleteCompras, con, transaction))
                        {
                            cmdCompras.Parameters.AddWithValue("@id", id);
                            await cmdCompras.ExecuteNonQueryAsync();
                        }

                        // B. Borrar definitivamente el proveedor
                        var deleteProveedor = "DELETE FROM Proveedores WHERE IdProveedor = @id";
                        using (var cmdProv = new SqlCommand(deleteProveedor, con, transaction))
                        {
                            cmdProv.Parameters.AddWithValue("@id", id);
                            int filas = await cmdProv.ExecuteNonQueryAsync();

                            if (filas == 0)
                            {
                                await transaction.RollbackAsync();
                                return (false, "El proveedor no existe o ya fue eliminado.");
                            }
                        }

                        await transaction.CommitAsync();
                        return (true, "Proveedor eliminado completamente de la base de datos.");
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }
    }
}