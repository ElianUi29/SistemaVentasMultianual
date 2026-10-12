using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;
using BackendVentas.Models;

namespace BackendVentas.Services
{
    public class CajaService
    {
        private readonly Conexion _conexion;

        public CajaService(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Obtener resumen de la última caja
        public async Task<ResumenCajaDto> ObtenerResumenCajaAsync()
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();
                var query = @"
                    SELECT TOP 1 
                        IdCaja, 
                        SaldoInicial, 
                        IngresosEfectivo, 
                        EgresosGastos, 
                        TotalEfectivo, 
                        Estado 
                    FROM Cajas 
                    ORDER BY IdCaja DESC";

                using (var cmd = new SqlCommand(query, con))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new ResumenCajaDto
                        {
                            IdCaja = Convert.ToInt32(reader["IdCaja"]),
                            SaldoInicial = Convert.ToDecimal(reader["SaldoInicial"]),
                            Ingresos = Convert.ToDecimal(reader["IngresosEfectivo"]),
                            Egresos = Convert.ToDecimal(reader["EgresosGastos"]),
                            TotalEfectivo = Convert.ToDecimal(reader["TotalEfectivo"]),
                            Estado = reader["Estado"].ToString() ?? "Cerrada"
                        };
                    }
                }
            }

            return new ResumenCajaDto();
        }

        // 2. Abrir caja
        public async Task<(bool Exito, string Mensaje)> AperturaCajaAsync(AperturaModel model)
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();

                // Verificar si ya existe una abierta
                var checkQuery = "SELECT COUNT(1) FROM Cajas WHERE Estado = 'Abierta'";
                using (var checkCmd = new SqlCommand(checkQuery, con))
                {
                    int abiertas = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (abiertas > 0)
                        return (false, "Ya existe una caja abierta. Primero realiza el corte.");
                }

                var insertQuery = @"
                    INSERT INTO Cajas (IdUsuario, FechaApertura, SaldoInicial, IngresosEfectivo, EgresosGastos, TotalEfectivo, Estado)
                    VALUES (@idUsu, GETDATE(), @monto, 0.00, 0.00, @monto, 'Abierta')";

                using (var cmd = new SqlCommand(insertQuery, con))
                {
                    cmd.Parameters.AddWithValue("@idUsu", model.IdUsuario > 0 ? model.IdUsuario : 1);
                    cmd.Parameters.AddWithValue("@monto", model.SaldoInicial);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return (true, "Caja abierta con éxito.");
        }

        // 3. Registrar Movimiento e impactar caja
        public async Task<(bool Exito, string Mensaje)> RegistrarMovimientoAsync(MovimientoModel model)
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();

                // Buscar caja abierta
                var findCmd = new SqlCommand("SELECT TOP 1 IdCaja FROM Cajas WHERE Estado = 'Abierta' ORDER BY IdCaja DESC", con);
                var result = await findCmd.ExecuteScalarAsync();
                if (result == null)
                    return (false, "No hay ninguna caja abierta actualmente.");

                int idCaja = Convert.ToInt32(result);

                // Insertar movimiento
                var insMov = @"
                    INSERT INTO MovimientosCaja (IdCaja, Tipo, Concepto, Monto, Fecha)
                    VALUES (@idCaja, @tipo, @concepto, @monto, GETDATE())";

                using (var cmdMov = new SqlCommand(insMov, con))
                {
                    cmdMov.Parameters.AddWithValue("@idCaja", idCaja);
                    cmdMov.Parameters.AddWithValue("@tipo", model.Tipo);
                    cmdMov.Parameters.AddWithValue("@concepto", model.Concepto);
                    cmdMov.Parameters.AddWithValue("@monto", model.Monto);
                    await cmdMov.ExecuteNonQueryAsync();
                }

                // Actualizar saldos en Cajas
                string updateSql = model.Tipo.Equals("Ingreso", StringComparison.OrdinalIgnoreCase)
                    ? "UPDATE Cajas SET IngresosEfectivo = IngresosEfectivo + @monto, TotalEfectivo = TotalEfectivo + @monto WHERE IdCaja = @idCaja"
                    : "UPDATE Cajas SET EgresosGastos = EgresosGastos + @monto, TotalEfectivo = TotalEfectivo - @monto WHERE IdCaja = @idCaja";

                using (var cmdUpd = new SqlCommand(updateSql, con))
                {
                    cmdUpd.Parameters.AddWithValue("@monto", model.Monto);
                    cmdUpd.Parameters.AddWithValue("@idCaja", idCaja);
                    await cmdUpd.ExecuteNonQueryAsync();
                }
            }

            return (true, "Movimiento registrado con éxito.");
        }

        // 4. Listar los movimientos de la caja abierta
        public async Task<List<MovimientoCajaDto>> ListarMovimientosAsync()
        {
            var lista = new List<MovimientoCajaDto>();

            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();
                var query = @"
                    SELECT TOP 30
                        m.IdMovimiento,
                        m.Tipo,
                        m.Concepto,
                        m.Monto,
                        m.Fecha
                    FROM MovimientosCaja m
                    INNER JOIN Cajas c ON m.IdCaja = c.IdCaja
                    WHERE c.Estado = 'Abierta'
                    ORDER BY m.IdMovimiento DESC";

                using (var cmd = new SqlCommand(query, con))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lista.Add(new MovimientoCajaDto
                        {
                            IdMovimiento = Convert.ToInt32(reader["IdMovimiento"]),
                            Tipo = reader["Tipo"].ToString() ?? string.Empty,
                            Concepto = reader["Concepto"].ToString() ?? string.Empty,
                            Monto = Convert.ToDecimal(reader["Monto"]),
                            Fecha = Convert.ToDateTime(reader["Fecha"]).ToString("yyyy-MM-dd HH:mm")
                        });
                    }
                }
            }

            return lista;
        }

        // 5. Corte de caja
        public async Task<(bool Exito, string Mensaje)> CorteCajaAsync()
        {
            using (var con = _conexion.ObtenerConexion())
            {
                await con.OpenAsync();
                var cmd = new SqlCommand("UPDATE TOP (1) Cajas SET Estado = 'Cerrada', FechaCierre = GETDATE() WHERE Estado = 'Abierta'", con);
                int filas = await cmd.ExecuteNonQueryAsync();
                if (filas == 0)
                    return (false, "No hay caja abierta para cerrar.");
            }

            return (true, "Corte efectuado con éxito.");
        }
    }
}