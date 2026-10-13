using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Vinculo.Models.Enums;

namespace Vinculo.Models.ViewModels
{
    public class RegistrarDonacionViewModel
    {
        public int Id { get; set; }
        public string? NumeroReferencia { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Today;
        public EstadoDonacion Estado { get; set; } = EstadoDonacion.Disponible;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(200)]
        public string Descripcion { get; set; }

        [Display(Name = "Fotografía del equipo (Opcional)")]
        public IFormFile? FotoArchivo { get; set; }
        public string? FotoRutaActual { get; set; }
        public List<DetalleDonacionViewModel> Detalles { get; set; } = new List<DetalleDonacionViewModel>();
    }

    public class DetalleDonacionViewModel
    {
        [Required(ErrorMessage = "Seleccione el tipo de equipamiento.")]
        [Display(Name = "Tipo de Equipamiento")]
        public TipoEquipamiento TipoEquipamiento { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }
    }
}