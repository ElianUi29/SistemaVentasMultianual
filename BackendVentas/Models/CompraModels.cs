namespace BackendVentas.Models
{
    public class CompraModel
    {
        public int IdProveedor { get; set; }
        public int IdUsuario { get; set; } = 1;
        public string? NumeroOC { get; set; }
        public decimal TotalCosto { get; set; }
        public string EstadoRecepcion { get; set; } = "Recibido";
    }

    public class CompraItemDto
    {
        public int IdCompra { get; set; }
        public string NumeroOC { get; set; } = string.Empty;
        public string FechaCompra { get; set; } = string.Empty;
        public string Proveedor { get; set; } = string.Empty;
        public decimal TotalCosto { get; set; }
        public string EstadoRecepcion { get; set; } = "Recibido";
    }

    public class CompraDetalleDto
    {
        public int IdCompra { get; set; }
        public string NumeroOC { get; set; } = string.Empty;
        public string FechaCompra { get; set; } = string.Empty;
        public decimal TotalCosto { get; set; }
        public string EstadoRecepcion { get; set; } = "Recibido";
        public string Proveedor { get; set; } = string.Empty;
        public string RucProveedor { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
    }
}