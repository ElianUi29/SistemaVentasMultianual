using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;

namespace BackendVentas.Controllers
{
    public class ProveedorModel
    {
        public int IdProveedor { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public bool Estado { get; set; } = true;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ProveedoresController : ControllerBase
    {
        private readonly Conexion _conexion;

        public ProveedoresController(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Listar todos los proveedores
        [HttpGet("listar")]
        public async Task<IActionResult> ListarProveedores()
        {
            var lista = new List<ProveedorModel>();

            try
            {
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
                                Documento = reader["Documento"].ToString() ?? "",
                                RazonSocial = reader["RazonSocial"].ToString() ?? "",
                                Telefono = reader["Telefono"] != DBNull.Value ? reader["Telefono"].ToString() : "-",
                                Direccion = reader["Direccion"] != DBNull.Value ? reader["Direccion"].ToString() : "-",
                                Estado = reader["Estado"] != DBNull.Value && Convert.ToBoolean(reader["Estado"])
                            });
                        }
                    }
                }
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al listar proveedores: {ex.Message}");
            }
        }

        // 2. Registrar nuevo proveedor
        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarProveedor([FromBody] ProveedorModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Documento) || string.IsNullOrWhiteSpace(model.RazonSocial))
                return BadRequest("El Documento/RUC y la Razón Social son requeridos.");

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();

                    // Validar si ya existe el documento
                    var checkQuery = "SELECT COUNT(1) FROM Proveedores WHERE Documento = @doc";
                    using (var checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@doc", model.Documento.Trim());
                        int existe = (int)await checkCmd.ExecuteScalarAsync();
                        if (existe > 0)
                            return BadRequest("Ya existe un proveedor registrado con ese RUC o documento.");
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

                return Ok(new { mensaje = "Proveedor registrado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar proveedor: {ex.Message}");
            }
        }

        // 3. Eliminación física real (Borra de Proveedores y de SQL Server)
        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarProveedor(int id)
        {
            if (id <= 0) return BadRequest("Identificador de proveedor inválido.");

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // A. Borrar primero compras asociadas para no violar la Foreign Key
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
                                    return NotFound("El proveedor no existe o ya fue eliminado.");
                                }
                            }

                            await transaction.CommitAsync();
                            return Ok(new { mensaje = "Proveedor eliminado completamente de la base de datos." });
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar proveedor: {ex.Message}");
            }
        }
    }
}