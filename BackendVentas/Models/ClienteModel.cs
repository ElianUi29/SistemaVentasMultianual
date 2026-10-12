namespace BackendVentas.Models
{
    public class ClienteModel
    {
        public int IdCliente { get; set; }
        public string Documento { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
    }
}