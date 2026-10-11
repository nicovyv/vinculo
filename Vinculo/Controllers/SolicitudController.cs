
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
    [Authorize(Roles = "Institucion")]
    public class SolicitudController : Controller
    {
        private readonly VinculoDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public SolicitudController(
            VinculoDbContext context,
            UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }

            var solicitudes = await _context.Solicitudes
                .AsNoTracking()
                .Include(s => s.Equipamientos)
                .Include(s => s.Observaciones)
                .Where(s => s.InstitucionId == institucion.Id)
                .OrderByDescending(s => s.Fecha)
                .ToListAsync();

            var model = solicitudes
                .Select(s => new SolicitudViewModel
                {
                    Id = s.Id,
                    NumeroReferencia = s.NumeroReferencia,
                    Fecha = s.Fecha,
                    Estado = s.Estado,

                    Detalles = s.Equipamientos
                        .Select(d => new DetalleSolicitudViewModel
                        {
                            TipoEquipamiento = d.TipoEquipamiento,
                            Cantidad = d.Cantidad
                        })
                        .ToList(),

                    Observaciones = s.Observaciones
                        .OrderByDescending(o => o.Fecha)
                        .Select(o => new ObservacionViewModel
                        {
                            Mensaje = o.Mensaje,
                            Fecha = o.Fecha
                        })
                        .ToList()
                })
                .ToList();

            return View(model);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }

            var solicitud = await _context.Solicitudes
                .AsNoTracking()
                .Include(s => s.Equipamientos)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.InstitucionId == institucion.Id);

            if (solicitud == null)
            {
                return NotFound();
            }

            var model = new SolicitudViewModel
            {
                Id = solicitud.Id,
                NumeroReferencia = solicitud.NumeroReferencia,
                Fecha = solicitud.Fecha,
                Estado = solicitud.Estado,

                Detalles = solicitud.Equipamientos
                    .Select(d => new DetalleSolicitudViewModel
                    {
                        TipoEquipamiento = d.TipoEquipamiento,
                        Cantidad = d.Cantidad
                    })
                    .ToList()
            };

            return View(model);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }

            var model = new SolicitudViewModel
            {
                Fecha = DateTime.Today,
                Detalles = new List<DetalleSolicitudViewModel>()
            };

            ViewBag.Institucion = institucion;

            return View(model);
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SolicitudViewModel model)
        {
            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }

            model.Detalles ??= new List<DetalleSolicitudViewModel>();

            model.Detalles = model.Detalles
                .GroupBy(d => d.TipoEquipamiento)
                .Select(g => new DetalleSolicitudViewModel
                {
                    TipoEquipamiento = g.Key,
                    Cantidad = g.Sum(d => d.Cantidad)
                })
                .ToList();

            if (!model.Detalles.Any())
            {
                ModelState.AddModelError(
                    nameof(model.Detalles),
                    "Debe agregar al menos un equipamiento.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Institucion = institucion;
                return View(model);
            }

            var solicitud = new Solicitud
            {
                NumeroReferencia = GenerarNumeroReferencia(),
                InstitucionId = institucion.Id,
                Fecha = DateTime.Now,
                Estado = EstadoSolicitud.Pendiente,

                Equipamientos = model.Detalles
                    .Select(d => new DetalleSolicitud
                    {
                        TipoEquipamiento = d.TipoEquipamiento,
                        Cantidad = d.Cantidad
                    })
                    .ToList()
            };

            _context.Solicitudes.Add(solicitud);

            // Registrar la creación en el historial.
            var observacion = new Observacion
            {
                Mensaje =
                    $"La institución creó la solicitud {solicitud.NumeroReferencia}.",
                Fecha = DateTime.Now,
                Solicitud = solicitud
            };

            _context.Observaciones.Add(observacion);

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] =
                $"La solicitud {solicitud.NumeroReferencia} fue creada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }

            var solicitud = await _context.Solicitudes
                .AsNoTracking()
                .Include(s => s.Equipamientos)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.InstitucionId == institucion.Id);

            if (solicitud == null)
            {
                return NotFound();
            }

            // Solo se pueden editar solicitudes pendientes.
            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["MensajeError"] =
                    "La solicitud no puede modificarse porque ya no se encuentra pendiente.";

                return RedirectToAction(nameof(Index));
            }

            var model = new SolicitudViewModel
            {
                Id = solicitud.Id,
                NumeroReferencia = solicitud.NumeroReferencia,
                Fecha = solicitud.Fecha,
                Estado = solicitud.Estado,

                Detalles = solicitud.Equipamientos
                    .Select(d => new DetalleSolicitudViewModel
                    {
                        TipoEquipamiento = d.TipoEquipamiento,
                        Cantidad = d.Cantidad
                    })
                    .ToList()
            };

            return View(model);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            SolicitudViewModel model)
        {
            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }

            var solicitud = await _context.Solicitudes
                .Include(s => s.Equipamientos)
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.InstitucionId == institucion.Id);

            if (solicitud == null)
            {
                return NotFound();
            }

            // La solicitud debe seguir pendiente.
            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["MensajeError"] =
                    "La solicitud no puede modificarse porque ya no se encuentra pendiente.";

                return RedirectToAction(nameof(Details), new { id });
            }

            model.Detalles ??= new List<DetalleSolicitudViewModel>();

            // Eliminar cantidades inválidas.
            model.Detalles = model.Detalles
                .Where(d => d.Cantidad > 0)
                .ToList();

            // Unificar equipamientos repetidos.
            model.Detalles = model.Detalles
                .GroupBy(d => d.TipoEquipamiento)
                .Select(g => new DetalleSolicitudViewModel
                {
                    TipoEquipamiento = g.Key,
                    Cantidad = g.Sum(d => d.Cantidad)
                })
                .ToList();

            // Debe quedar al menos un equipamiento.
            if (!model.Detalles.Any())
            {
                ModelState.AddModelError(
                    nameof(model.Detalles),
                    "Debe existir al menos un equipamiento.");
            }

            if (!ModelState.IsValid)
            {
                model.Id = solicitud.Id;
                model.NumeroReferencia = solicitud.NumeroReferencia;
                model.Fecha = solicitud.Fecha;
                model.Estado = solicitud.Estado;

                return View(model);
            }

            // Reemplazar los detalles anteriores.
            _context.RemoveRange(solicitud.Equipamientos);

            solicitud.Equipamientos = model.Detalles
                .Select(d => new DetalleSolicitud
                {
                    SolicitudId = solicitud.Id,
                    TipoEquipamiento = d.TipoEquipamiento,
                    Cantidad = d.Cantidad
                })
                .ToList();

            // Registrar la modificación en el historial.
            var observacion = new Observacion
            {
                Mensaje =
                    $"La institución modificó los equipamientos de la solicitud {solicitud.NumeroReferencia}.",
                Fecha = DateTime.Now,
                Solicitud = solicitud
            };

            _context.Observaciones.Add(observacion);

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] =
                $"La solicitud {solicitud.NumeroReferencia} fue modificada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // CANCELAR SOLICITUD - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }

            var solicitud = await _context.Solicitudes
                .FirstOrDefaultAsync(s =>
                    s.Id == id &&
                    s.InstitucionId == institucion.Id);

            if (solicitud == null)
            {
                return NotFound();
            }

            if (solicitud.Estado == EstadoSolicitud.Cancelada)
            {
                TempData["MensajeError"] =
                    "La solicitud ya se encuentra cancelada.";

                return RedirectToAction(nameof(Index));
            }

            // Solo se permite cancelar solicitudes pendientes
            // o que estén en coordinación.
            if (solicitud.Estado != EstadoSolicitud.Pendiente &&
                solicitud.Estado != EstadoSolicitud.EnCoordinacion)
            {
                TempData["MensajeError"] =
                    "La solicitud no puede cancelarse porque ya fue concretada.";

                return RedirectToAction(nameof(Index));
            }

            // No cancelar si existen asignaciones activas.
            var tieneAsignacionesActivas = await _context.Asignaciones
                .AnyAsync(a =>
                    a.SolicitudId == solicitud.Id &&
                    a.Estado == EstadoAsignacion.Activa);

            if (tieneAsignacionesActivas)
            {
                TempData["MensajeError"] =
                    "No se puede cancelar la solicitud mientras tenga asignaciones activas. Primero deben resolverse esas asignaciones.";

                return RedirectToAction("Index", "Solicitud");
            }

            solicitud.Estado = EstadoSolicitud.Cancelada;

            // Registrar la cancelación en el historial.
            var observacion = new Observacion
            {
                Mensaje =
                    $"La institución canceló la solicitud {solicitud.NumeroReferencia}.",
                Fecha = DateTime.Now,
                Solicitud = solicitud
            };

            _context.Observaciones.Add(observacion);

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] =
                $"La solicitud {solicitud.NumeroReferencia} fue cancelada correctamente.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // OBTENER INSTITUCIÓN ACTUAL
        // =========================================================

        private async Task<Institucion?> ObtenerInstitucionActual()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await _context.Instituciones
                .Include(i => i.Domicilio)
                .FirstOrDefaultAsync(i => i.UsuarioId == userId);
        }

        // =========================================================
        // GENERAR NÚMERO DE REFERENCIA
        // =========================================================

        private string GenerarNumeroReferencia()
        {
            return $"SOL-{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}