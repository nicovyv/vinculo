using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Vinculo.Models.ViewModels
{
    public class PerfilEmpresaViewModel
    {
        public int Id { get; set; }

        public string? RazonSocial { get; set; }

        public string? Cuit { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        public string? Telefono { get; set; }

        public string? Email { get; set; }

        [Required(ErrorMessage = "La persona de contacto es obligatoria.")]
        public string? PersonaContacto { get; set; }

        [Required(ErrorMessage = "La calle es obligatoria.")]
        public string? Calle { get; set; }

        [Required(ErrorMessage = "El número es obligatorio.")]
        public string? Numero { get; set; }

        [Required(ErrorMessage = "La localidad es obligatoria.")]
        public string? Localidad { get; set; }

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        public string? Provincia { get; set; }

        [Required(ErrorMessage = "El código postal es obligatorio.")]
        public string? CodigoPostal { get; set; }

        public double? Latitud { get; set; }

        public double? Longitud { get; set; }

        // Indicadores del Dashboard
        public int TotalDonaciones { get; set; }
        public int Disponibles { get; set; }
        public int Asignadas { get; set; }
        public int Entregadas { get; set; }
        public int EquipamientosDonados { get; set; }

        // Últimos 5 movimientos
        public List<MovimientoEmpresaViewModel> MovimientosRecientes { get; set; } = new();
    }

    public class MovimientoEmpresaViewModel
    {
        public string NumeroReferencia { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
    }
}