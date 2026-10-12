using Microsoft.AspNetCore.Mvc;
using BackendVentas.Models;
using BackendVentas.Services;

namespace BackendVentas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UsuarioService _usuarioService;

        public AuthController(UsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Clave))
            {
                return BadRequest(new { mensaje = "Por favor, ingresa tu correo y contraseña." });
            }

            try
            {
                var usuario = await _usuarioService.ValidarCredencialesAsync(request.Correo, request.Clave);

                if (usuario == null)
                {
                    return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
                }

                if (!usuario.Estado)
                {
                    return StatusCode(403, new { mensaje = "Acceso denegado: Tu usuario está inactivo o no tiene permisos de acceso." });
                }

                return Ok(new
                {
                    mensaje = "Acceso concedido.",
                    usuario = new
                    {
                        idUsuario = usuario.IdUsuario,
                        nombreCompleto = usuario.NombreCompleto,
                        correo = usuario.Correo,
                        rol = usuario.Rol
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error del servidor: {ex.Message}" });
            }
        }

        // POST: api/Auth/registro
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
                bool existe = await _usuarioService.ExisteCorreoAsync(request.Correo);
                if (existe)
                {
                    return BadRequest(new { mensaje = "Este correo electrónico ya se encuentra registrado." });
                }

                await _usuarioService.RegistrarUsuarioAsync(request);

                return Ok(new { mensaje = "¡Cuenta creada exitosamente en SQL Server! Ya puedes iniciar sesión." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al registrar usuario: {ex.Message}" });
            }
        }
    }
}