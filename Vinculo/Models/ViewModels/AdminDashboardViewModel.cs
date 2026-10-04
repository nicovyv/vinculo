using System;
using System.Collections.Generic;

namespace Vinculo.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        // =========================
        // USUARIOS
        // =========================

        public int TotalUsuarios { get; set; }

        public int TotalInstituciones { get; set; }

        public int TotalEmpresas { get; set; }

        public int TotalAdministradores { get; set; }

        public int UsuariosActivos { get; set; }

        public int UsuariosInactivos { get; set; }


        // =========================
        // SOLICITUDES
        // =========================

        public int SolicitudesPendientes { get; set; }

        public int SolicitudesEnCoordinacion { get; set; }

        public int SolicitudesConcretadas { get; set; }

        public int TotalSolicitudes { get; set; }


        // =========================
        // DONACIONES
        // =========================
        // Preparado para una etapa posterior.
        // Por ahora el AdministradorController
        // los deja en 0.

        public int DonacionesDisponibles { get; set; }

        public int DonacionesAsignadas { get; set; }

        public int DonacionesEntregadas { get; set; }


        // =========================
        // ACTIVIDAD RECIENTE
        // =========================

        public List<AdminSolicitudResumenViewModel> SolicitudesRecientes { get; set; }
            = new List<AdminSolicitudResumenViewModel>();
    }


    public class AdminSolicitudResumenViewModel
    {
        public int Id { get; set; }

        public string NumeroReferencia { get; set; } = string.Empty;

        public string Institucion { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public string Estado { get; set; } = string.Empty;

        public int CantidadEquipamientos { get; set; }
    }
}