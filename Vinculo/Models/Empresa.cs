using System.ComponentModel.DataAnnotations;

namespace Vinculo.Models
{
    public class Empresa
    {
        public int Id { get; set; }
        [Required]
        public string RazonSocial { get; set; }
        [Required]
        public string Cuit { get; set; }
        [Required]
        public string Telefono { get; set; }
        [Required]
        public Domicilio Domicilio { get; set; }
        [Required]
        public string Email { get; set; }
        public List<Donacion>? Donaciones { get; set; }
    }
}
