using Vinculo.Models.Enums;

namespace Vinculo.Models.ViewModels
{
    public class AdminUsuarioViewModel
    {
        public List<AdminUsuarioItemViewModel> Usuarios { get; set; }
            = new List<AdminUsuarioItemViewModel>();

        public string? TipoFiltro { get; set; }

        public string? EstadoFiltro { get; set; }
    }

    public class AdminUsuarioItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public TipoUsuario TipoUsuario { get; set; }

        public DateTime FechaAlta { get; set; }

        public bool Activo { get; set; }
    }
}
