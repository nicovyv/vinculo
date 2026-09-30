using System.ComponentModel.DataAnnotations;

namespace Vinculo.Models.ViewModels
{
    public class PerfilEmpresaViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La Razón Social es obligatoria")]
        [Display(Name = "Razón Social")]
        public string RazonSocial { get; set; }

        [Required(ErrorMessage = "El CUIT es obligatorio")]
        public string Cuit { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Display(Name = "Teléfono de contacto")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        public string Email { get; set; }

        // --- Datos del Domicilio ---
        [Required(ErrorMessage = "La calle es obligatoria")]
        public string Calle { get; set; }

        [Required(ErrorMessage = "El número es obligatorio")]
        public string Numero { get; set; }

        [Required(ErrorMessage = "La localidad es obligatoria")]
        public string Localidad { get; set; }

        [Required(ErrorMessage = "La provincia es obligatoria")]
        public string Provincia { get; set; }

        [Required(ErrorMessage = "El código postal es obligatorio")]
        [Display(Name = "Código Postal")]
        public string CodigoPostal { get; set; }
    }
}