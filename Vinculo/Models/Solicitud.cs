using System.ComponentModel.DataAnnotations;
using Vinculo.Models.Enums;

namespace Vinculo.Models
{
    public class Solicitud
    {
        public int Id { get; set; }
        [Required]
        public string NumeroReferencia { get; set; }
        [Required]
        public int InstitucionId { get; set; }
        public Institucion Institucion { get; set; }
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }
        [Required]
        public EstadoSolicitud Estado { get; set; }
    }
}
