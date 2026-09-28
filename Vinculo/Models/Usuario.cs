using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using Vinculo.Models.Enums;

namespace Vinculo.Models
{
    public class Usuario : IdentityUser
    {
        [Required]
        public TipoUsuario TipoUsuario { get; set; }
        [DataType(DataType.Date)]
        public DateTime FechaAlta { get; set; }
        public bool Activo { get; set; } = true;
    }
}
