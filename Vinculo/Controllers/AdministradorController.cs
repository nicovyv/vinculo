using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinculo.Data;
using Vinculo.Models;
using Vinculo.Models.Enums;
using Vinculo.Models.ViewModels;

namespace Vinculo.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdministradorController : Controller
    {
        private readonly VinculoDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public AdministradorController(
            VinculoDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =========================================================
        // DASHBOARD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // =========================
            // USUARIOS
            // =========================

            var totalUsuarios = await _userManager.Users
                .CountAsync();

            var totalInstituciones = await _userManager.Users
                .CountAsync(u => u.TipoUsuario == TipoUsuario.Institucion);

            var totalEmpresas = await _userManager.Users
                .CountAsync(u => u.TipoUsuario == TipoUsuario.Empresa);

            var totalAdministradores = await _userManager.Users
                .CountAsync(u => u.TipoUsuario == TipoUsuario.Administrador);

            var usuariosActivos = await _userManager.Users
                .CountAsync(u => u.Activo);

            var usuariosInactivos = await _userManager.Users
                .CountAsync(u => !u.Activo);


            // =========================
            // SOLICITUDES
            // =========================

            var solicitudesPendientes = await _context.Solicitudes
                .CountAsync(s =>
                    s.Estado == EstadoSolicitud.Pendiente);

            var solicitudesEnCoordinacion = await _context.Solicitudes
                .CountAsync(s =>
                    s.Estado == EstadoSolicitud.EnCoordinacion);

            var solicitudesConcretadas = await _context.Solicitudes
                .CountAsync(s =>
                    s.Estado == EstadoSolicitud.Concretado);

            var totalSolicitudes = await _context.Solicitudes
                .CountAsync();


            // =========================
            // SOLICITUDES RECIENTES
            // =========================

            var solicitudesRecientes = await _context.Solicitudes
                .Include(s => s.Institucion)
                .Include(s => s.Equipamientos)
                .OrderByDescending(s => s.Fecha)
                .Take(5)
                .Select(s => new AdminSolicitudResumenViewModel
                {
                    Id = s.Id,

                    NumeroReferencia = s.NumeroReferencia,

                    Institucion = s.Institucion.Nombre,

                    Fecha = s.Fecha,

                    Estado = s.Estado.ToString(),

                    CantidadEquipamientos = s.Equipamientos
                        .Sum(e => e.Cantidad)
                })
                .ToListAsync();


            // =========================
            // DASHBOARD
            // =========================

            var model = new AdminDashboardViewModel
            {
                // Usuarios
                TotalUsuarios = totalUsuarios,
                TotalInstituciones = totalInstituciones,
                TotalEmpresas = totalEmpresas,
                TotalAdministradores = totalAdministradores,

                UsuariosActivos = usuariosActivos,
                UsuariosInactivos = usuariosInactivos,

                // Solicitudes
                SolicitudesPendientes = solicitudesPendientes,
                SolicitudesEnCoordinacion = solicitudesEnCoordinacion,
                SolicitudesConcretadas = solicitudesConcretadas,
                TotalSolicitudes = totalSolicitudes,

                // Donaciones
                // Todavía no conectadas con la lógica
                // de administración.
                DonacionesDisponibles = 0,
                DonacionesAsignadas = 0,
                DonacionesEntregadas = 0,

                // Actividad
                SolicitudesRecientes = solicitudesRecientes
            };

            return View(model);
        }


        // =========================================================
        // ADMINISTRACIÓN DE USUARIOS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Usuarios(
            string? tipo,
            string? estado)
        {
            // =========================
            // OBTENER USUARIOS
            // =========================

            var query = _userManager.Users
                .AsQueryable();


            // =========================
            // FILTRO POR TIPO
            // =========================

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                if (Enum.TryParse<TipoUsuario>(
                    tipo,
                    true,
                    out var tipoUsuario))
                {
                    query = query.Where(u =>
                        u.TipoUsuario == tipoUsuario);
                }
            }


            // =========================
            // FILTRO POR ESTADO
            // =========================

            if (!string.IsNullOrWhiteSpace(estado))
            {
                if (estado.Equals(
                    "Activo",
                    StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(u => u.Activo);
                }
                else if (estado.Equals(
                    "Inactivo",
                    StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(u => !u.Activo);
                }
            }


            // =========================
            // PROYECCIÓN
            // =========================

            var usuarios = await query
                .OrderBy(u => u.Email)
                .Select(u => new AdminUsuarioItemViewModel
                {
                    Id = u.Id,

                    Email = u.Email ?? string.Empty,

                    TipoUsuario = u.TipoUsuario,

                    FechaAlta = u.FechaAlta,

                    Activo = u.Activo
                })
                .ToListAsync();


            // =========================
            // VIEW MODEL
            // =========================

            var model = new AdminUsuarioViewModel
            {
                Usuarios = usuarios,

                TipoFiltro = tipo,

                EstadoFiltro = estado
            };


            return View(model);
        }


        // =========================================================
        // CAMBIAR ESTADO DE USUARIO
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstadoUsuario(
            string id)
        {
            // =========================
            // VALIDAR ID
            // =========================

            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction(nameof(Usuarios));
            }


            // =========================
            // BUSCAR USUARIO
            // =========================

            var usuario = await _userManager
                .FindByIdAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }


            // =========================
            // EVITAR AUTO-DESACTIVACIÓN
            // =========================

            var usuarioActualId =
                _userManager.GetUserId(User);

            if (usuario.Id == usuarioActualId)
            {
                TempData["Error"] =
                    "No podés desactivar tu propio usuario.";

                return RedirectToAction(nameof(Usuarios));
            }


            // =========================
            // CAMBIAR ESTADO
            // =========================

            usuario.Activo = !usuario.Activo;


            // =========================
            // GUARDAR
            // =========================

            var resultado =
                await _userManager.UpdateAsync(usuario);


            if (!resultado.Succeeded)
            {
                TempData["Error"] =
                    "No se pudo actualizar el estado del usuario.";

                return RedirectToAction(nameof(Usuarios));
            }


            // =========================
            // MENSAJE
            // =========================

            if (usuario.Activo)
            {
                TempData["Success"] =
                    "El usuario fue activado correctamente.";
            }
            else
            {
                TempData["Success"] =
                    "El usuario fue desactivado correctamente.";
            }


            return RedirectToAction(nameof(Usuarios));
        }
    
        // =========================================================
        // ADMINISTRACIÓN DE INSTITUCIONES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Instituciones(string? busqueda)
        {
            var query = _context.Instituciones
                .Include(i => i.Domicilio)
                .Include(i => i.Usuario)
                .Include(i => i.Solicitudes)
                .AsQueryable();

            // =========================
            // BÚSQUEDA
            // =========================

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                query = query.Where(i =>
                    i.Nombre.Contains(busqueda) ||
                    i.Cuit.Contains(busqueda) ||
                    i.Email.Contains(busqueda));
            }

            // =========================
            // PROYECCIÓN
            // =========================

            var instituciones = await query
                .OrderBy(i => i.Nombre)
                .Select(i => new AdminInstitucionItemViewModel
                {
                    Id = i.Id,

                    Nombre = i.Nombre,

                    Cuit = i.Cuit,

                    Email = i.Email,

                    Telefono = i.Telefono,

                    Localidad = i.Domicilio.Localidad,

                    Provincia = i.Domicilio.Provincia,

                    Activo = i.Usuario.Activo,

                    CantidadSolicitudes = i.Solicitudes.Count(),

                    SolicitudesPendientes = i.Solicitudes.Count(
                        s => s.Estado == EstadoSolicitud.Pendiente),

                    SolicitudesEnCoordinacion = i.Solicitudes.Count(
                        s => s.Estado == EstadoSolicitud.EnCoordinacion),

                    SolicitudesConcretadas = i.Solicitudes.Count(
                        s => s.Estado == EstadoSolicitud.Concretado)
                })
                .ToListAsync();

            // =========================
            // VIEW MODEL
            // =========================

            var model = new AdminInstitucionViewModel
            {
                Instituciones = instituciones,

                Busqueda = busqueda
            };

            return View(model);
        }
    }
}