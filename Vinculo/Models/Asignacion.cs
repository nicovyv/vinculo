using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 
using Vinculo.Models.Enums;

namespace Vinculo.Models
{
    public class Asignacion
    {
        public int Id { get; set; }

        [Required]
        public string NumeroReferencia { get; set; } 

        public int SolicitudId { get; set; }
        [Required]
        [Range(1, int.MaxValue,
            ErrorMessage = "La cantidad asignada debe ser mayor a 0")]
        public int Cantidad { get; set; }
        public Solicitud Solicitud { get; set; } = null!;

        public int DonacionId { get; set; }

        public Donacion Donacion { get; set; } = null!;


        public DateTime Fecha { get; set; }

        public EstadoAsignacion Estado { get; set; }
        public ICollection<Observacion> Observaciones { get; set; } = new List<Observacion>();
    }
}