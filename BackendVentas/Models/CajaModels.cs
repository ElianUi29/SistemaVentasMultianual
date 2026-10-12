namespace BackendVentas.Models
{
    public class AperturaModel
    {
        public int IdUsuario { get; set; } = 1;
        public decimal SaldoInicial { get; set; }
    }

    public class MovimientoModel
    {
        public string Tipo { get; set; } = "Ingreso"; // 'Ingreso' o 'Egreso'
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
    }

    public class ResumenCajaDto
    {
        public int IdCaja { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Egresos { get; set; }
        public decimal TotalEfectivo { get; set; }
        public string Estado { get; set; } = "Cerrada";
    }

    public class MovimientoCajaDto
    {
        public int IdMovimiento { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Fecha { get; set; } = string.Empty;
    }
}