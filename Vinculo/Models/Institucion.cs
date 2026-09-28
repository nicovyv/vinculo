using System.ComponentModel.DataAnnotations;

namespace Vinculo.Models
{
    public class Institucion
    {
        public int Id { get; set; }
        [Required]
        public string Nombre { get; set; }
        [Required]
        public string Cuit { get; set; }
        [Required]
        public string Telefono { get; set; }
        [Required]
        public Domicilio Domicilio { get; set; }
        [Required]
        public string Email { get; set; }
        public List<Solicitud>? Solicitudes { get; set; }

    }
}
