using Microsoft.AspNetCore.Mvc;
using BackendVentas.Models;
using BackendVentas.Services;

namespace BackendVentas.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CajaController : ControllerBase
    {
        private readonly CajaService _cajaService;

        public CajaController(CajaService cajaService)
        {
            _cajaService = cajaService;
        }

        // 1. Obtener el resumen de la caja activa
        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumenCaja()
        {
            try
            {
                var resumen = await _cajaService.ObtenerResumenCajaAsync();
                return Ok(new
                {
                    idCaja = resumen.IdCaja,
                    saldoInicial = resumen.SaldoInicial,
                    ingresos = resumen.Ingresos,
                    egresos = resumen.Egresos,
                    totalEfectivo = resumen.TotalEfectivo,
                    estado = resumen.Estado
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = $"Error al obtener estado de caja: {ex.Message}" });
            }
        }

        // 2. Apertura de caja
        [HttpPost("apertura")]
        public async Task<IActionResult> AperturaCaja([FromBody] AperturaModel model)
        {
            if (model.SaldoInicial < 0)
                return BadRequest(new { mensaje = "El saldo inicial no puede ser negativo." });

            try
            {
                var resultado = await _cajaService.AperturaCajaAsync(model);
                if (!resultado.Exito)
                    return BadRequest(new { mensaje = resultado.Mensaje });

                return Ok(new { mensaje = resultado.Mensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        // 3. Registrar un Movimiento (Ingreso o Egreso)
        [HttpPost("movimiento")]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] MovimientoModel model)
        {
            if (model.Monto <= 0 || string.IsNullOrWhiteSpace(model.Concepto))
                return BadRequest(new { mensaje = "Concepto y monto válidos son requeridos." });

            try
            {
                var resultado = await _cajaService.RegistrarMovimientoAsync(model);
                if (!resultado.Exito)
                    return BadRequest(new { mensaje = resultado.Mensaje });

                return Ok(new { mensaje = resultado.Mensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        // 4. Listar los movimientos de la caja abierta
        [HttpGet("movimientos")]
        public async Task<IActionResult> ListarMovimientos()
        {
            try
            {
                var movimientos = await _cajaService.ListarMovimientosAsync();
                return Ok(movimientos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }

        // 5. Corte de caja
        [HttpPost("corte")]
        public async Task<IActionResult> CorteCaja()
        {
            try
            {
                var resultado = await _cajaService.CorteCajaAsync();
                if (!resultado.Exito)
                    return BadRequest(new { mensaje = resultado.Mensaje });

                return Ok(new { mensaje = resultado.Mensaje });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = ex.Message });
            }
        }
    }
}