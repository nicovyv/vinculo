
using System;
using System.Collections.Generic;
using Vinculo.Models.Enums;

namespace Vinculo.Models.ViewModels
{
    public class AdminAsignacionViewModel
    {
        public List<AdminAsignacionItemViewModel> Asignaciones { get; set; } = new();

        public string? Busqueda { get; set; }

        public string? EstadoFiltro { get; set; }
    }

    public class AdminAsignacionItemViewModel
    {
        public int Id { get; set; }

        public string NumeroReferencia { get; set; } = string.Empty;

        public string ReferenciaDonacion { get; set; } = string.Empty;

        public string ReferenciaSolicitud { get; set; } = string.Empty;

        public string Empresa { get; set; } = string.Empty;

        public string Institucion { get; set; } = string.Empty;

        public TipoEquipamiento Tipo { get; set; }

        public DateTime Fecha { get; set; }

        public EstadoAsignacion Estado { get; set; }
    }
}