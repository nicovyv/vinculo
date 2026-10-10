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
            // DONACIONES
            // =========================

            var donacionesDisponibles = await _context.Donaciones
                .CountAsync(d => d.Estado == EstadoDonacion.Disponible);

            var donacionesAsignadas = await _context.Donaciones
                .CountAsync(d => d.Estado == EstadoDonacion.Asignada);

            var donacionesEntregadas = await _context.Donaciones
                .CountAsync(d => d.Estado == EstadoDonacion.Entregada);


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
                DonacionesDisponibles = donacionesDisponibles,
                DonacionesAsignadas = donacionesAsignadas,
                DonacionesEntregadas = donacionesEntregadas,

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
        // OBTENER DETALLE DE USUARIO PARA EL MODAL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ObtenerDetalleUsuario(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            string tipoTexto;
            string entidadAsociada;

            switch (usuario.TipoUsuario)
            {
                case TipoUsuario.Empresa:
                    tipoTexto = "Empresa";

                    entidadAsociada = await _context.Empresas
                        .AsNoTracking()
                        .Where(e => e.UsuarioId == usuario.Id)
                        .Select(e => e.RazonSocial)
                        .FirstOrDefaultAsync()
                        ?? "Sin empresa asociada";
                    break;

                case TipoUsuario.Institucion:
                    tipoTexto = "Institución";

                    entidadAsociada = await _context.Instituciones
                        .AsNoTracking()
                        .Where(i => i.UsuarioId == usuario.Id)
                        .Select(i => i.Nombre)
                        .FirstOrDefaultAsync()
                        ?? "Sin institución asociada";
                    break;

                case TipoUsuario.Administrador:
                    tipoTexto = "Administrador";
                    entidadAsociada = "No corresponde";
                    break;

                default:
                    tipoTexto = usuario.TipoUsuario.ToString();
                    entidadAsociada = "Sin entidad asociada";
                    break;
            }

            return Json(new
            {
                id = usuario.Id,
                email = usuario.Email ?? "",
                tipoUsuario = tipoTexto,
                entidadAsociada = entidadAsociada,
                fechaAltaTexto = usuario.FechaAlta.ToString("dd/MM/yyyy HH:mm"),
                activo = usuario.Activo
            });
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

        // =========================================================
        // OBTENER DETALLE DE INSTITUCIÓN PARA EL MODAL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ObtenerDetalleInstitucion(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institucion = await _context.Instituciones
                .AsNoTracking()
                .Include(i => i.Domicilio)
                .Include(i => i.Usuario)
                .Include(i => i.Solicitudes)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (institucion == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = institucion.Id,
                nombre = institucion.Nombre,
                cuit = institucion.Cuit,
                email = institucion.Email,
                telefono = institucion.Telefono,
                personaContacto = institucion.PersonaContacto ?? "",
                activo = institucion.Usuario.Activo,

                calle = institucion.Domicilio?.Calle ?? "",
                numero = institucion.Domicilio?.Numero ?? "",
                localidad = institucion.Domicilio?.Localidad ?? "",
                provincia = institucion.Domicilio?.Provincia ?? "",
                codigoPostal = institucion.Domicilio?.CodigoPostal ?? "",

                cantidadSolicitudes = institucion.Solicitudes.Count,
                solicitudesPendientes = institucion.Solicitudes.Count(
                    s => s.Estado == EstadoSolicitud.Pendiente),
                solicitudesEnCoordinacion = institucion.Solicitudes.Count(
                    s => s.Estado == EstadoSolicitud.EnCoordinacion),
                solicitudesConcretadas = institucion.Solicitudes.Count(
                    s => s.Estado == EstadoSolicitud.Concretado),
                solicitudesCanceladas = institucion.Solicitudes.Count(
                    s => s.Estado == EstadoSolicitud.Cancelada)
            });
        }

        // =========================================================
        // ADMINISTRACIÓN DE SOLICITUDES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Solicitudes(
        string? busqueda,
        string? estado)
        {
            // =========================
            // OBTENER SOLICITUDES
            // =========================

            var query = _context.Solicitudes
                .Include(s => s.Institucion)
                .Include(s => s.Equipamientos)
                .AsQueryable();


            // =========================
            // BÚSQUEDA
            // =========================

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                query = query.Where(s =>
                    s.NumeroReferencia.Contains(busqueda) ||
                    s.Institucion.Nombre.Contains(busqueda));
            }


            // =========================
            // FILTRO POR ESTADO
            // =========================

            if (!string.IsNullOrWhiteSpace(estado))
            {
                if (Enum.TryParse<EstadoSolicitud>(
                    estado,
                    true,
                    out var estadoSolicitud))
                {
                    query = query.Where(s =>
                        s.Estado == estadoSolicitud);
                }
            }


            // =========================
            // PROYECCIÓN
            // =========================

            var solicitudes = await query
                .OrderByDescending(s => s.Fecha)
                .Select(s => new AdminSolicitudItemViewModel
                {
                    Id = s.Id,

                    NumeroReferencia = s.NumeroReferencia,

                    Institucion = s.Institucion.Nombre,

                    Fecha = s.Fecha,

                    Estado = s.Estado,

                    CantidadEquipamientos = s.Equipamientos
                        .Sum(e => e.Cantidad),

                    CantidadTipos = s.Equipamientos
                        .Select(e => e.TipoEquipamiento)
                        .Distinct()
                        .Count()
                })
                .ToListAsync();


            // =========================
            // VIEW MODEL
            // =========================

            var model = new AdminSolicitudViewModel
            {
                Solicitudes = solicitudes,

                Busqueda = busqueda,

                EstadoFiltro = estado
            };


            return View(model);

        }
        // =========================================================
        // ADMINISTRACIÓN DE EMPRESAS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Empresas(string? busqueda)
        {
            var query = _context.Empresas
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                query = query.Where(e =>
                    e.RazonSocial.Contains(busqueda) ||
                    e.Cuit.Contains(busqueda));
            }

            var empresas = await query
                .OrderBy(e => e.RazonSocial)
                .Select(e => new AdminEmpresaItemViewModel
                {
                    Id = e.Id,
                    RazonSocial = e.RazonSocial,
                    Cuit = e.Cuit,
                    Telefono = e.Telefono,
                    Email = e.Email,
                    PersonaContacto = e.PersonaContacto
                })
                .ToListAsync();

            var modelo = new AdminEmpresaViewModel
            {
                Empresas = empresas,
                Busqueda = busqueda
            };

            return View(modelo);
        }

        // =========================================================
        // ADMINISTRACIÓN DE DONACIONES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Donaciones(
            string? busqueda,
            string? estado)
        {
            var query = _context.Donaciones
                .AsNoTracking()
                .Include(d => d.Empresa)
                .AsQueryable();

            // Búsqueda por referencia o empresa
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                query = query.Where(d =>
                    d.NumeroReferencia.Contains(busqueda) ||
                    d.Empresa.RazonSocial.Contains(busqueda));
            }

            // Filtro por estado
            if (!string.IsNullOrWhiteSpace(estado) &&
                Enum.TryParse<EstadoDonacion>(
                    estado,
                    true,
                    out var estadoDonacion))
            {
                query = query.Where(d =>
                    d.Estado == estadoDonacion);
            }

            var donaciones = await query
                .OrderByDescending(d => d.Fecha)
                .Select(d => new AdminDonacionItemViewModel
                {
                    Id = d.Id,
                    NumeroReferencia = d.NumeroReferencia,
                    Empresa = d.Empresa.RazonSocial,
                    Tipo = d.Tipo,
                    Cantidad = d.Cantidad,
                    Descripcion = d.Descripcion,
                    FotoRuta = d.FotoRuta,
                    Fecha = d.Fecha,
                    Estado = d.Estado
                })
                .ToListAsync();

            var model = new AdminDonacionViewModel
            {
                Donaciones = donaciones,
                Busqueda = busqueda,
                EstadoFiltro = estado
            };

            return View(model);
        }
        // =========================================================
        // OBTENER DETALLE DE SOLICITUD PARA EL MODAL
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ObtenerDetalleSolicitud(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var solicitud = await _context.Solicitudes
                .AsNoTracking()
                .Include(s => s.Institucion)
                    .ThenInclude(i => i.Domicilio)
                .Include(s => s.Equipamientos)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            var estadoTexto = solicitud.Estado switch
            {
                EstadoSolicitud.Pendiente => "Pendiente",
                EstadoSolicitud.EnCoordinacion => "En coordinación",
                EstadoSolicitud.Concretado => "Concretada",
                EstadoSolicitud.Cancelada => "Cancelada",
                _ => solicitud.Estado.ToString()
            };

            return Json(new
            {
                id = solicitud.Id,
                numeroReferencia = solicitud.NumeroReferencia,
                institucion = solicitud.Institucion.Nombre,
                cuit = solicitud.Institucion.Cuit,
                email = solicitud.Institucion.Email,
                telefono = solicitud.Institucion.Telefono,
                fechaTexto = solicitud.Fecha.ToString("dd/MM/yyyy"),
                estadoTexto = estadoTexto,

                calle = solicitud.Institucion.Domicilio?.Calle ?? "",
                numero = solicitud.Institucion.Domicilio?.Numero ?? "",
                localidad = solicitud.Institucion.Domicilio?.Localidad ?? "",
                provincia = solicitud.Institucion.Domicilio?.Provincia ?? "",
                codigoPostal = solicitud.Institucion.Domicilio?.CodigoPostal ?? "",

                equipamientos = solicitud.Equipamientos
                    .Select(e => new
                    {
                        tipoEquipamiento = e.TipoEquipamiento.ToString(),
                        cantidad = e.Cantidad
                    })
                    .ToList()
            });
        }


        // =========================================================
        // DETALLE DE Asignaciones
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Asignaciones(
            string? busqueda,
            string? estado)
        {
            var query = _context.Asignaciones
                .AsNoTracking()
                .Include(a => a.Donacion)
                    .ThenInclude(d => d.Empresa)
                .Include(a => a.Solicitud)
                    .ThenInclude(s => s.Institucion)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                busqueda = busqueda.Trim();

                query = query.Where(a =>
                    a.NumeroReferencia.Contains(busqueda) ||
                    a.Donacion.NumeroReferencia.Contains(busqueda) ||
                    a.Solicitud.NumeroReferencia.Contains(busqueda) ||
                    a.Donacion.Empresa.RazonSocial.Contains(busqueda) ||
                    a.Solicitud.Institucion.Nombre.Contains(busqueda));
            }

            if (!string.IsNullOrWhiteSpace(estado) &&
                Enum.TryParse<EstadoAsignacion>(
                    estado, true, out var estadoAsignacion))
            {
                query = query.Where(a => a.Estado == estadoAsignacion);
            }

            var asignaciones = await query
                .OrderByDescending(a => a.Fecha)
                .Select(a => new AdminAsignacionItemViewModel
                {
                    Id = a.Id,
                    NumeroReferencia = a.NumeroReferencia,
                    ReferenciaDonacion = a.Donacion.NumeroReferencia,
                    ReferenciaSolicitud = a.Solicitud.NumeroReferencia,
                    Empresa = a.Donacion.Empresa.RazonSocial,
                    Institucion = a.Solicitud.Institucion.Nombre,
                    Tipo = a.Donacion.Tipo,
                    Fecha = a.Fecha,
                    Estado = a.Estado
                })
                .ToListAsync();

            var modelo = new AdminAsignacionViewModel
            {
                Asignaciones = asignaciones,
                Busqueda = busqueda,
                EstadoFiltro = estado
            };

            return View(modelo);
        }

    }
}