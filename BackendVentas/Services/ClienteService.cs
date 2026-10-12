using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;
using BackendVentas.Models;

namespace BackendVentas.Services
{
    public class ClienteService
    {
        private readonly Conexion _conexion;

        public ClienteService(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Obtener lista de clientes activos
        public async Task<List<ClienteModel>> ListarClientesAsync()
        {
            var lista = new List<ClienteModel>();

            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();
                var query = "SELECT IdCliente, Documento, Nombre, Telefono, Direccion FROM Clientes WHERE Estado = 1";

                using (var cmd = new SqlCommand(query, con))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lista.Add(new ClienteModel
                        {
                            IdCliente = Convert.ToInt32(reader["IdCliente"]),
                            Documento = reader["Documento"].ToString() ?? string.Empty,
                            Nombre = reader["Nombre"].ToString() ?? string.Empty,
                            Telefono = reader["Telefono"] == DBNull.Value ? null : reader["Telefono"].ToString(),
                            Direccion = reader["Direccion"] == DBNull.Value ? null : reader["Direccion"].ToString()
                        });
                    }
                }
            }

            return lista;
        }

        // 2. Guardar un nuevo cliente en SQL Server
        public async Task GuardarClienteAsync(ClienteModel cliente)
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();
                var query = @"INSERT INTO Clientes (Documento, Nombre, Telefono, Direccion, Estado) 
                              VALUES (@doc, @nom, @tel, @dir, 1)";

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@doc", cliente.Documento.Trim());
                    cmd.Parameters.AddWithValue("@nom", cliente.Nombre.Trim());
                    cmd.Parameters.AddWithValue("@tel", (object?)cliente.Telefono?.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@dir", (object?)cliente.Direccion?.Trim() ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}