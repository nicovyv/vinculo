using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace Vinculo.Models.ViewModels
{
    public class AdminInstitucionViewModel
    {
        public List<AdminInstitucionItemViewModel> Instituciones { get; set; }
            = new();

        public string? Busqueda { get; set; }
    }

    public class AdminInstitucionItemViewModel
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Cuit { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string Localidad { get; set; } = string.Empty;

        public string Provincia { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public int CantidadSolicitudes { get; set; }

        public int SolicitudesPendientes { get; set; }

        public int SolicitudesEnCoordinacion { get; set; }

        public int SolicitudesConcretadas { get; set; }
    }
}

