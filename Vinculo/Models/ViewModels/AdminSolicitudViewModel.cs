using System;
using System.Collections.Generic;
using Vinculo.Models.Enums;

namespace Vinculo.Models.ViewModels
{
    public class AdminSolicitudViewModel
    {
        public List<AdminSolicitudItemViewModel> Solicitudes { get; set; } = new List<AdminSolicitudItemViewModel>();

    public string? Busqueda { get; set; }

        public string? EstadoFiltro { get; set; }
    }

    public class AdminSolicitudItemViewModel
    {
        public int Id { get; set; }

        public string NumeroReferencia { get; set; }

        public string Institucion { get; set; }

        public DateTime Fecha { get; set; }

        public EstadoSolicitud Estado { get; set; }

        public int CantidadEquipamientos { get; set; }

        public int CantidadTipos { get; set; }
    }

}
