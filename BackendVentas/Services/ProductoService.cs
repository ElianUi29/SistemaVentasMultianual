using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;
using BackendVentas.Models;

namespace BackendVentas.Services
{
    public class ProductoService
    {
        private readonly Conexion _conexion;

        public ProductoService(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Listar inventario
        public async Task<List<ProductoModel>> ListarProductosAsync()
        {
            var lista = new List<ProductoModel>();

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
                            Codigo = reader["Codigo"].ToString() ?? string.Empty,
                            Nombre = reader["Nombre"].ToString() ?? string.Empty,
                            Categoria = reader["Categoria"].ToString() ?? string.Empty,
                            Precio = Convert.ToDecimal(reader["Precio"]),
                            Stock = stock,
                            Estado = stock <= 5 ? "Bajo Stock" : "Disponible"
                        });
                    }
                }
            }

            return lista;
        }

        // 2. Obtener producto por ID
        public async Task<ProductoModel?> ObtenerProductoAsync(int id)
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
                            return new ProductoModel
                            {
                                IdProducto = Convert.ToInt32(reader["IdProducto"]),
                                Codigo = reader["Codigo"].ToString() ?? string.Empty,
                                Nombre = reader["Nombre"].ToString() ?? string.Empty,
                                Categoria = reader["Categoria"].ToString() ?? string.Empty,
                                Precio = Convert.ToDecimal(reader["Precio"]),
                                Stock = Convert.ToInt32(reader["Stock"]),
                                Estado = reader["Estado"].ToString() ?? "Disponible"
                            };
                        }
                    }
                }
            }

            return null;
        }

        // 3. Registrar nuevo producto
        public async Task<(bool Exito, string Mensaje)> GuardarProductoAsync(ProductoModel model)
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
                    int count = Convert.ToInt32(await check.ExecuteScalarAsync());
                    if (count > 0)
                        return (false, "Ya existe un producto con ese código.");
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

            return (true, "Producto registrado con éxito.");
        }

        // 4. Actualizar producto existente
        public async Task<bool> ActualizarProductoAsync(int id, ProductoModel model)
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
                    return filas > 0;
                }
            }
        }

        // 5. Eliminar producto físicamente
        public async Task<bool> EliminarProductoAsync(int id)
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
                    return filas > 0;
                }
            }
        }
    }
}