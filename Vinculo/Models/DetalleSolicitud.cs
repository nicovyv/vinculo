using System.ComponentModel.DataAnnotations;
using Vinculo.Models.Enums;

namespace Vinculo.Models
{
    public class DetalleSolicitud
    {
        public int Id { get; set; }
        [Required]
        public int SolicitudId { get; set; }
        [Required]
        public Solicitud Solicitud { get; set; }
        [Required]
        public TipoEquipamiento TipoEquipamiento { get; set; }
        [Required]
        public int Cantidad { get; set; }
        [Required]

    }
}
