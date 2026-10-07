using Vinculo.Models;

namespace Vinculo.Models.ViewModels
{
    public class CoincidenciasViewModel
    {
        public Donacion Donacion { get; set; }
        public Empresa Empresa { get; set; }
        public List<Solicitud> SolicitudesCompatibles { get; set; }
    }
}