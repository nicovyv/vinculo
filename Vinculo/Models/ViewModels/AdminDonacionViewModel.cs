
using Vinculo.Models.Enums;

namespace Vinculo.Models.ViewModels
{
    public class AdminDonacionViewModel
    {
        public List<AdminDonacionItemViewModel> Donaciones { get; set; }
            = new();

        public string? Busqueda { get; set; }

        public string? EstadoFiltro { get; set; }
    }

    public class AdminDonacionItemViewModel
    {
        public int Id { get; set; }

        public string NumeroReferencia { get; set; } = string.Empty;

        public string Empresa { get; set; } = string.Empty;

        public TipoEquipamiento Tipo { get; set; }

        public int Cantidad { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public string? FotoRuta { get; set; }

        public DateTime Fecha { get; set; }

        public EstadoDonacion Estado { get; set; }
    }
}