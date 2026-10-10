using System.ComponentModel.DataAnnotations;

namespace Vinculo.Models
{
    public class Observacion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Mensaje { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;

        // Relaciones opcionales: una observación puede
        // vincularse con una o varias entidades.

        public int? DonacionId { get; set; }
        public Donacion? Donacion { get; set; }

        public int? SolicitudId { get; set; }
        public Solicitud? Solicitud { get; set; }

        public int? AsignacionId { get; set; }
        public Asignacion? Asignacion { get; set; }
    }
}
