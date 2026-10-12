namespace BackendVentas.Models
{
    public class ProveedorModel
    {
        public int IdProveedor { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public bool Estado { get; set; } = true;
    }
}