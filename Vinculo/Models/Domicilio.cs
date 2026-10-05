using System.ComponentModel.DataAnnotations;

namespace Vinculo.Models
{
    public class Domicilio
    {
        public int Id { get; set; }
        [Required]
        public string Calle { get; set; }
        [Required]
        public string Numero { get; set; }
        [Required]
        public string Localidad { get; set; }
        [Required]
        public string Provincia { get; set; }
        [Required]
        public string CodigoPostal { get; set; }
        public double? Latitud { get; set; }

        public double? Longitud { get; set; }
    }
}
