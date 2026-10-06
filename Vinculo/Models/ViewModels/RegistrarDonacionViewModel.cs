using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Vinculo.Models.Enums;

namespace Vinculo.Models.ViewModels
{
    public class RegistrarDonacionViewModel
    {
        [Required(ErrorMessage = "Seleccione el tipo de equipamiento.")]
        [Display(Name = "Tipo de Equipamiento")]
        public TipoEquipamiento Tipo { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(200)]
        public string Descripcion { get; set; }

        [Display(Name = "Fotografía del equipo (Opcional)")]
        public IFormFile? FotoArchivo { get; set; }
    }
}