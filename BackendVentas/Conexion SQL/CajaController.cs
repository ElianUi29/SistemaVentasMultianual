using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaVentasMultianual.Data;

namespace BackendVentas.Controllers
{
    public class AperturaModel
    {
        public int IdUsuario { get; set; } = 1;
        public decimal SaldoInicial { get; set; }
    }

    public class MovimientoModel
    {
        public string Tipo { get; set; } = "Ingreso"; // 'Ingreso' o 'Egreso'
        public string Concepto { get; set; } = "";
        public decimal Monto { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class CajaController : ControllerBase
    {
        private readonly Conexion _conexion;

        public CajaController(Conexion conexion)
        {
            _conexion = conexion;
        }

        // 1. Obtener el resumen de la caja activa
        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumenCaja()
        {
            try
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
                            return Ok(new
                            {
                                idCaja = Convert.ToInt32(reader["IdCaja"]),
                                saldoInicial = Convert.ToDecimal(reader["SaldoInicial"]),
                                ingresos = Convert.ToDecimal(reader["IngresosEfectivo"]),
                                egresos = Convert.ToDecimal(reader["EgresosGastos"]),
                                totalEfectivo = Convert.ToDecimal(reader["TotalEfectivo"]),
                                estado = reader["Estado"].ToString()
                            });
                        }
                    }
                }

                return Ok(new { idCaja = 0, saldoInicial = 0m, ingresos = 0m, egresos = 0m, totalEfectivo = 0m, estado = "Cerrada" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al obtener estado de caja: {ex.Message}");
            }
        }

        // 2. Apertura de caja
        [HttpPost("apertura")]
        public async Task<IActionResult> AperturaCaja([FromBody] AperturaModel model)
        {
            if (model.SaldoInicial < 0) return BadRequest("El saldo inicial no puede ser negativo.");

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    // Verificar si ya hay una abierta
                    var checkQuery = "SELECT COUNT(1) FROM Cajas WHERE Estado = 'Abierta'";
                    using (var checkCmd = new SqlCommand(checkQuery, con))
                    {
                        int abiertas = (int)await checkCmd.ExecuteScalarAsync();
                        if (abiertas > 0) return BadRequest("Ya existe una caja abierta. Primero realiza el corte.");
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
                return Ok(new { mensaje = "Caja abierta con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // 3. Registrar un Movimiento (Ingreso o Egreso) y actualizar la caja automáticamente
        [HttpPost("movimiento")]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] MovimientoModel model)
        {
            if (model.Monto <= 0 || string.IsNullOrWhiteSpace(model.Concepto))
                return BadRequest("Concepto y monto válidos son requeridos.");

            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();

                    // Buscar caja abierta
                    int idCaja = 0;
                    var findCmd = new SqlCommand("SELECT TOP 1 IdCaja FROM Cajas WHERE Estado = 'Abierta' ORDER BY IdCaja DESC", con);
                    var result = await findCmd.ExecuteScalarAsync();
                    if (result == null) return BadRequest("No hay ninguna caja abierta actualmente.");
                    idCaja = (int)result;

                    // 1. Insertar en MovimientosCaja
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

                    // 2. Actualizar el acumulado en Cajas
                    string updateSql = model.Tipo == "Ingreso"
                        ? "UPDATE Cajas SET IngresosEfectivo = IngresosEfectivo + @monto, TotalEfectivo = TotalEfectivo + @monto WHERE IdCaja = @idCaja"
                        : "UPDATE Cajas SET EgresosGastos = EgresosGastos + @monto, TotalEfectivo = TotalEfectivo - @monto WHERE IdCaja = @idCaja";

                    using (var cmdUpd = new SqlCommand(updateSql, con))
                    {
                        cmdUpd.Parameters.AddWithValue("@monto", model.Monto);
                        cmdUpd.Parameters.AddWithValue("@idCaja", idCaja);
                        await cmdUpd.ExecuteNonQueryAsync();
                    }
                }
                return Ok(new { mensaje = "Movimiento registrado con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // 4. Listar los movimientos de la caja abierta
        [HttpGet("movimientos")]
        public async Task<IActionResult> ListarMovimientos()
        {
            var lista = new List<object>();
            try
            {
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
                            lista.Add(new
                            {
                                idMovimiento = Convert.ToInt32(reader["IdMovimiento"]),
                                tipo = reader["Tipo"].ToString(),
                                concepto = reader["Concepto"].ToString(),
                                monto = Convert.ToDecimal(reader["Monto"]),
                                fecha = Convert.ToDateTime(reader["Fecha"]).ToString("yyyy-MM-dd HH:mm")
                            });
                        }
                    }
                }
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // 5. Corte de caja
        [HttpPost("corte")]
        public async Task<IActionResult> CorteCaja()
        {
            try
            {
                using (var con = _conexion.ObtenerConexion())
                {
                    await con.OpenAsync();
                    var cmd = new SqlCommand("UPDATE TOP (1) Cajas SET Estado = 'Cerrada', FechaCierre = GETDATE() WHERE Estado = 'Abierta'", con);
                    int filas = await cmd.ExecuteNonQueryAsync();
                    if (filas == 0) return BadRequest("No hay caja abierta para cerrar.");
                }
                return Ok(new { mensaje = "Corte efectuado con éxito." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}