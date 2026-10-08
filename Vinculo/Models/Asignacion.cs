using System.ComponentModel.DataAnnotations;
using Vinculo.Models.Enums;

namespace Vinculo.Models
{
    public class Asignacion
    {
        public int Id { get; set; }

        public int SolicitudId { get; set; }
        public Solicitud Solicitud { get; set; } = null!;

        public int DonacionId { get; set; }
        public Donacion Donacion { get; set; } = null!;

        public int Cantidad { get; set; }

        public DateTime Fecha { get; set; }

        public EstadoAsignacion Estado { get; set; }
    }
}