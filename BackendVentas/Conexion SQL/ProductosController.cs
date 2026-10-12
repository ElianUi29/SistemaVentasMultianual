using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;

namespace BackendVentas.Controllers
{
    public class ProductoModel
    {
        public int IdProducto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string Estado { get; set; } = "Disponible";
    }

    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly Conexion _conexion;

        public ProductosController(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Listar inventario
        [HttpGet("listar")]
        public async Task<IActionResult> ListarProductos()
        {
            var lista = new List<ProductoModel>();
            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    var query = @"
                        SELECT IdProducto, Codigo, Nombre, Categoria, Precio, Stock, Estado 
                        FROM Productos 
                        ORDER BY IdProducto ASC";

                    using (var cmd = new SqlCommand(query, con))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int stock = Convert.ToInt32(reader["Stock"]);
                            lista.Add(new ProductoModel
                            {
                                IdProducto = Convert.ToInt32(reader["IdProducto"]),
                                Codigo = reader["Codigo"].ToString() ?? "",
                                Nombre = reader["Nombre"].ToString() ?? "",
                                Categoria = reader["Categoria"].ToString() ?? "",
                                Precio = Convert.ToDecimal(reader["Precio"]),
                                Stock = stock,
                                Estado = stock <= 5 ? "Bajo Stock" : "Disponible"
                            });
                        }
                    }
                }
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al listar productos: {ex.Message}");
            }
        }

        // 2. Obtener producto por ID (para editar)
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerProducto(int id)
        {
            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    var query = "SELECT IdProducto, Codigo, Nombre, Categoria, Precio, Stock, Estado FROM Productos WHERE IdProducto = @id";
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return Ok(new ProductoModel
                                {
                                    IdProducto = Convert.ToInt32(reader["IdProducto"]),
                                    Codigo = reader["Codigo"].ToString() ?? "",
                                    Nombre = reader["Nombre"].ToString() ?? "",
                                    Categoria = reader["Categoria"].ToString() ?? "",
                                    Precio = Convert.ToDecimal(reader["Precio"]),
                                    Stock = Convert.ToInt32(reader["Stock"]),
                                    Estado = reader["Estado"].ToString() ?? "Disponible"
                                });
                            }
                        }
                    }
                }
                return NotFound("Producto no encontrado.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // 3. Registrar nuevo producto
        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarProducto([FromBody] ProductoModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Nombre) || model.Precio <= 0)
                return BadRequest("El nombre y un precio válido son requeridos.");

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();

                    string codigo = string.IsNullOrWhiteSpace(model.Codigo) 
                        ? $"PRD-{DateTime.Now.Ticks % 10000:0000}" 
                        : model.Codigo.Trim();

                    using (var check = new SqlCommand("SELECT COUNT(1) FROM Productos WHERE Codigo = @cod", con))
                    {
                        check.Parameters.AddWithValue("@cod", codigo);
                        int count = (int)await check.ExecuteScalarAsync();
                        if (count > 0) return BadRequest("Ya existe un producto con ese código.");
                    }

                    string estado = model.Stock <= 5 ? "Bajo Stock" : "Disponible";

                    var query = @"
                        INSERT INTO Productos (Codigo, Nombre, Categoria, Precio, Stock, Estado)
                        VALUES (@cod, @nom, @cat, @precio, @stock, @estado)";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@cod", codigo);
                        cmd.Parameters.AddWithValue("@nom", model.Nombre.Trim());
                        cmd.Parameters.AddWithValue("@cat", string.IsNullOrWhiteSpace(model.Categoria) ? "General" : model.Categoria.Trim());
                        cmd.Parameters.AddWithValue("@precio", model.Precio);
                        cmd.Parameters.AddWithValue("@stock", model.Stock);
                        cmd.Parameters.AddWithValue("@estado", estado);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Ok(new { mensaje = "Producto registrado con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // 4. Actualizar producto existente
        [HttpPut("actualizar/{id}")]
        public async Task<IActionResult> ActualizarProducto(int id, [FromBody] ProductoModel model)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(model.Nombre))
                return BadRequest("Datos inválidos.");

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    string estado = model.Stock <= 5 ? "Bajo Stock" : "Disponible";

                    var query = @"
                        UPDATE Productos 
                        SET Nombre = @nom, Categoria = @cat, Precio = @precio, Stock = @stock, Estado = @estado
                        WHERE IdProducto = @id";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@nom", model.Nombre.Trim());
                        cmd.Parameters.AddWithValue("@cat", model.Categoria.Trim());
                        cmd.Parameters.AddWithValue("@precio", model.Precio);
                        cmd.Parameters.AddWithValue("@stock", model.Stock);
                        cmd.Parameters.AddWithValue("@estado", estado);
                        int filas = await cmd.ExecuteNonQueryAsync();
                        if (filas == 0) return NotFound("Producto no encontrado.");
                    }
                }
                return Ok(new { mensaje = "Producto actualizado con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // 5. Eliminar producto físicamente
        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();

                    // Limpiar referencias en DetalleVentas si existen
                    var delDetalle = "DELETE FROM DetalleVentas WHERE IdProducto = @id";
                    using (var cmdD = new SqlCommand(delDetalle, con))
                    {
                        cmdD.Parameters.AddWithValue("@id", id);
                        await cmdD.ExecuteNonQueryAsync();
                    }

                    var query = "DELETE FROM Productos WHERE IdProducto = @id";
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        int filas = await cmd.ExecuteNonQueryAsync();
                        if (filas == 0) return NotFound("Producto no encontrado.");
                    }
                }
                return Ok(new { mensaje = "Producto eliminado con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}