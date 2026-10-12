using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;
using BackendVentas.Models;

namespace BackendVentas.Services
{
    public class UsuarioService
    {
        private readonly Conexion _conexion;

        public UsuarioService(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Validar usuario en SQL Server (Login)
        public async Task<UsuarioDto?> ValidarCredencialesAsync(string correo, string clave)
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();

                var query = @"
                    SELECT TOP 1 
                        IdUsuario, 
                        NombreCompleto, 
                        Correo, 
                        Rol, 
                        Estado 
                    FROM Usuarios 
                    WHERE Correo = @correo AND Clave = @clave";

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@correo", correo.Trim());
                    cmd.Parameters.AddWithValue("@clave", clave.Trim());

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new UsuarioDto
                            {
                                IdUsuario = Convert.ToInt32(reader["IdUsuario"]),
                                NombreCompleto = reader["NombreCompleto"].ToString() ?? string.Empty,
                                Correo = reader["Correo"].ToString() ?? string.Empty,
                                Rol = reader["Rol"].ToString() ?? string.Empty,
                                Estado = Convert.ToBoolean(reader["Estado"])
                            };
                        }
                    }
                }
            }

            return null;
        }

        // 2. Verificar existencia del correo
        public async Task<bool> ExisteCorreoAsync(string correo)
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();

                var query = "SELECT COUNT(1) FROM Usuarios WHERE Correo = @correo";
                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@correo", correo.Trim());
                    int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }

        // 3. Insertar usuario en SQL Server (Registro)
        public async Task RegistrarUsuarioAsync(RegistroRequest request)
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();

                var query = @"
                    INSERT INTO Usuarios (NombreCompleto, Correo, Clave, Rol, Estado, FechaRegistro)
                    VALUES (@nombre, @correo, @clave, @rol, 1, GETDATE())";

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@nombre", request.NombreCompleto.Trim());
                    cmd.Parameters.AddWithValue("@correo", request.Correo.Trim());
                    cmd.Parameters.AddWithValue("@clave", request.Clave.Trim());
                    cmd.Parameters.AddWithValue("@rol", string.IsNullOrWhiteSpace(request.Rol) ? "Vendedor" : request.Rol.Trim());

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        // 4. Listar todos los usuarios
        public async Task<List<UsuarioDto>> ListarUsuariosAsync()
        {
            var lista = new List<UsuarioDto>();

            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();
                var query = "SELECT IdUsuario, NombreCompleto, Correo, Rol, Estado FROM Usuarios ORDER BY IdUsuario DESC";

                using (var cmd = new SqlCommand(query, con))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lista.Add(new UsuarioDto
                        {
                            IdUsuario = Convert.ToInt32(reader["IdUsuario"]),
                            NombreCompleto = reader["NombreCompleto"].ToString() ?? string.Empty,
                            Correo = reader["Correo"].ToString() ?? string.Empty,
                            Rol = reader["Rol"].ToString() ?? string.Empty,
                            Estado = Convert.ToBoolean(reader["Estado"])
                        });
                    }
                }
            }

            return lista;
        }

        // 5. Desactivar o Activar usuario (Baja lógica segura para no romper ventas/cajas)
        public async Task<bool> CambiarEstadoUsuarioAsync(int idUsuario, bool nuevoEstado)
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();
                var query = "UPDATE Usuarios SET Estado = @estado WHERE IdUsuario = @id";

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@estado", nuevoEstado ? 1 : 0);
                    cmd.Parameters.AddWithValue("@id", idUsuario);

                    int filas = await cmd.ExecuteNonQueryAsync();
                    return filas > 0;
                }
            }
        }

        // 6. Eliminar usuario físicamente (Borrado real si no tiene registros vinculados)
        public async Task<(bool Exito, string Mensaje)> EliminarUsuarioFisicoAsync(int idUsuario)
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();
                try
                {
                    var query = "DELETE FROM Usuarios WHERE IdUsuario = @id";
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", idUsuario);
                        int filas = await cmd.ExecuteNonQueryAsync();

                        if (filas == 0)
                            return (false, "El usuario no existe o ya fue eliminado.");

                        return (true, "Usuario eliminado permanentemente de la base de datos.");
                    }
                }
                catch (SqlException ex) when (ex.Number == 547) // Código de error SQL Server para restricción FOREIGN KEY
                {
                    return (false, "No se puede eliminar de raíz porque tiene ventas, compras o aperturas de caja asociadas. Se recomienda desactivar su acceso poniendo Estado = 0.");
                }
            }
        }
    }
}