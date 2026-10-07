using Vinculo.Models;

namespace Vinculo.Models.ViewModels
{
    public class CoincidenciasViewModel
    {
        public Donacion Donacion { get; set; }
        public Empresa Empresa { get; set; }
        public List<SolicitudConDistancia> SolicitudesCompatibles { get; set; } = new List<SolicitudConDistancia>();
    }

    public class SolicitudConDistancia
    {
        public Solicitud Solicitud { get; set; }
        public double DistanciaKm { get; set; }
    }
}