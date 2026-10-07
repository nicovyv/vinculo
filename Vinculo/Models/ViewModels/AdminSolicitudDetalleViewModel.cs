using Vinculo.Models.Enums;

namespace Vinculo.Models.ViewModels
{
    public class AdminSolicitudDetalleViewModel
    {
        public int Id { get; set; }

        public string? NumeroReferencia { get; set; }

        public string Institucion { get; set; } = string.Empty;

        public string Cuit { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public EstadoSolicitud Estado { get; set; }

        public string Calle { get; set; } = string.Empty;

        public string Numero { get; set; } = string.Empty;

        public string Localidad { get; set; } = string.Empty;

        public string Provincia { get; set; } = string.Empty;

        public string CodigoPostal { get; set; } = string.Empty;

        public List<DetalleSolicitudViewModel> Equipamientos { get; set; } = new();
    }
}