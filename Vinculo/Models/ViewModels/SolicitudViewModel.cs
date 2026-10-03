using System.ComponentModel.DataAnnotations;
using Vinculo.Models.Enums;

namespace Vinculo.Models.ViewModels
{
    public class SolicitudViewModel
    {
        public int Id { get; set; }

        public string? NumeroReferencia { get; set; }

        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        public EstadoSolicitud Estado { get; set; }

        public List<DetalleSolicitudViewModel> Detalles { get; set; } = new();
    }

    public class DetalleSolicitudViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un tipo de equipamiento")]
        [Display(Name = "Tipo de equipamiento")]
        public TipoEquipamiento TipoEquipamiento { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }
    }
}