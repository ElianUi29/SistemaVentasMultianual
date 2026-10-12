using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;

namespace BackendVentas.Controllers
{
    public class LoginRequest
    {
        public string Correo { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
    }

    public class RegistroRequest
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public string Rol { get; set; } = "Vendedor"; // Rol por defecto
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly Conexion _conexion;

        public AuthController(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. INICIAR SESIÓN
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Clave))
            {
                return BadRequest(new { mensaje = "Por favor, ingresa tu correo y contraseña." });
            }

            try
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
                        cmd.Parameters.AddWithValue("@correo", request.Correo.Trim());
                        cmd.Parameters.AddWithValue("@clave", request.Clave.Trim());

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                bool estado = Convert.ToBoolean(reader["Estado"]);

                                if (!estado)
                                {
                                    return StatusCode(403, new { mensaje = "Acceso denegado: Tu usuario está inactivo o no tiene permisos de acceso." });
                                }

                                var usuario = new
                                {
                                    idUsuario = Convert.ToInt32(reader["IdUsuario"]),
                                    nombreCompleto = reader["NombreCompleto"].ToString(),
                                    correo = reader["Correo"].ToString(),
                                    rol = reader["Rol"].ToString()
                                };

                                return Ok(new
                                {
                                    mensaje = "Acceso concedido.",
                                    usuario = usuario
                                });
                            }
                        }
                    }
                }

                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error del servidor: {ex.Message}" });
            }
        }

        // 2. CREAR NUEVO USUARIO (REGISTRO EN SQL SERVER)
        [HttpPost("registro")]
        public async Task<IActionResult> Registro([FromBody] RegistroRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreCompleto) || 
                string.IsNullOrWhiteSpace(request.Correo) || 
                string.IsNullOrWhiteSpace(request.Clave))
            {
                return BadRequest(new { mensaje = "Todos los campos son obligatorios." });
            }

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();

                    // Validar si el correo ya existe en SQL Server
                    var queryCheck = "SELECT COUNT(1) FROM Usuarios WHERE Correo = @correo";
                    using (var cmdCheck = new SqlCommand(queryCheck, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@correo", request.Correo.Trim());
                        int existe = (int)await cmdCheck.ExecuteScalarAsync();
                        if (existe > 0)
                        {
                            return BadRequest(new { mensaje = "Este correo electrónico ya se encuentra registrado." });
                        }
                    }

                    // Insertar nuevo usuario con Estado = 1 (Activo)
                    var queryInsert = @"
                        INSERT INTO Usuarios (NombreCompleto, Correo, Clave, Rol, Estado, FechaRegistro)
                        VALUES (@nombre, @correo, @clave, @rol, 1, GETDATE())";

                    using (var cmdInsert = new SqlCommand(queryInsert, con))
                    {
                        cmdInsert.Parameters.AddWithValue("@nombre", request.NombreCompleto.Trim());
                        cmdInsert.Parameters.AddWithValue("@correo", request.Correo.Trim());
                        cmdInsert.Parameters.AddWithValue("@clave", request.Clave.Trim());
                        cmdInsert.Parameters.AddWithValue("@rol", string.IsNullOrWhiteSpace(request.Rol) ? "Vendedor" : request.Rol.Trim());

                        await cmdInsert.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { mensaje = "¡Cuenta creada exitosamente en SQL Server! Ya puedes iniciar sesión." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al registrar usuario: {ex.Message}" });
            }
        }
    }
}