using System.ComponentModel.DataAnnotations;

namespace Vinculo.Models.ViewModels
{
    public class RegistroOrganizacionViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar el tipo de entidad.")]
        public string TipoOrganizacion { get; set; } // Recibirá "institucion" o "empresa" desde el radio button

        [Required(ErrorMessage = "El Nombre o Razón Social es requerido.")]
        [Display(Name = "Nombre o Razón Social")]
        public string NombreRazonSocial { get; set; }

        [Required(ErrorMessage = "El CUIT es requerido.")]
        public string Cuit { get; set; }


        [Required(ErrorMessage = "La persona de contacto es requerida.")]
        [Display(Name = "Persona de Contacto Directo")]
        public string PersonaContacto { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido.")]
        public string Telefono { get; set; }

        // --- Datos del Domicilio ---
        [Required(ErrorMessage = "La calle es requerida.")]
        public string Calle { get; set; }

        [Required(ErrorMessage = "El número es requerido.")]
        public string Numero { get; set; }

        [Required(ErrorMessage = "La localidad es requerida.")]
        public string Localidad { get; set; }

        [Required(ErrorMessage = "La provincia es requerida.")]
        public string Provincia { get; set; }

        [Required(ErrorMessage = "El código postal es requerido.")]
        public string CodigoPostal { get; set; }

        public double? Latitud { get; set; }
        public double? Longitud { get; set; }

        // --- Credenciales de Usuario ---
        [Required(ErrorMessage = "El email es requerido.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida.")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Debe tener al menos 8 caracteres.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Debe confirmar la contraseña.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; }

        // --- Términos y Condiciones ---
        [Range(typeof(bool), "true", "true", ErrorMessage = "Debes aceptar los Términos y Condiciones para poder registrar tu organización.")]
        public bool AceptaTerminos { get; set; }
    }
}