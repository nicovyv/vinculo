using System;
using System.Collections.Generic;

namespace Vinculo.Models.ViewModels
{
    public class InstitucionDashboardViewModel
    {
        public int InstitucionId { get; set; }

        public string NombreInstitucion { get; set; } = string.Empty;

        public int TotalSolicitudes { get; set; }

        public int SolicitudesPendientes { get; set; }

        public int SolicitudesEnCoordinacion { get; set; }

        public int SolicitudesConcretadas { get; set; }
        public int SolicitudesCanceladas { get; set; }

        public int EquipamientosSolicitados { get; set; }

        public List<SolicitudViewModel> SolicitudesRecientes { get; set; }
            = new List<SolicitudViewModel>();
    }
}