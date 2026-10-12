using System.ComponentModel.DataAnnotations;
using Vinculo.Models.Enums;

namespace Vinculo.Models
{
    public class DetalleDonacion
    {
        public int Id { get; set; }

        [Required]
        public int DonacionId { get; set; }

        [Required]
        public Donacion Donacion { get; set; }

        [Required]
        public TipoEquipamiento TipoEquipamiento { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }
    }
}