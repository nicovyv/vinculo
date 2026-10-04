using System.ComponentModel.DataAnnotations;
using Vinculo.Models.Enums;

namespace Vinculo.Models
{
    public class Donacion
    {
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }
        public Empresa Empresa { get; set; }

        [Required]
        public TipoEquipamiento Tipo { get; set; }

        [Required]
        [StringLength(200)]
        public string Descripcion { get; set; }

        // ruta relativa de la imagen (ej: /uploads/xyz.jpg)
        public string? FotoRuta { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        [Required]
        public EstadoDonacion Estado { get; set; }

    }
}
