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

                return RedirectToAction(nameof(Details), new { id });
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

            // =========================================================
            // VALIDAR QUE SIGA PENDIENTE
            // =========================================================

            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["MensajeError"] =
                    "La solicitud no puede modificarse porque ya no se encuentra pendiente.";

                return RedirectToAction(nameof(Details), new { id });
            }

            model.Detalles ??= new List<DetalleSolicitudViewModel>();


            // =========================================================
            // ELIMINAR CANTIDADES INVÁLIDAS
            // =========================================================

            model.Detalles = model.Detalles
                .Where(d => d.Cantidad > 0)
                .ToList();


            // =========================================================
            // UNIFICAR EQUIPAMIENTOS REPETIDOS
            // =========================================================

            model.Detalles = model.Detalles
                .GroupBy(d => d.TipoEquipamiento)
                .Select(g => new DetalleSolicitudViewModel
                {
                    TipoEquipamiento = g.Key,
                    Cantidad = g.Sum(d => d.Cantidad)
                })
                .ToList();


            // =========================================================
            // VALIDAR QUE EXISTA AL MENOS UN EQUIPAMIENTO
            // =========================================================

            if (!model.Detalles.Any())
            {
                ModelState.AddModelError(
                    nameof(model.Detalles),
                    "Debe existir al menos un equipamiento.");
            }


            // =========================================================
            // SI HAY ERRORES
            // =========================================================

            if (!ModelState.IsValid)
            {
                model.Id = solicitud.Id;
                model.NumeroReferencia = solicitud.NumeroReferencia;
                model.Fecha = solicitud.Fecha;
                model.Estado = solicitud.Estado;

                return View(model);
            }


            // =========================================================
            // REEMPLAZAR DETALLES
            // =========================================================

            _context.RemoveRange(solicitud.Equipamientos);

            solicitud.Equipamientos = model.Detalles
                .Select(d => new DetalleSolicitud
                {
                    SolicitudId = solicitud.Id,
                    TipoEquipamiento = d.TipoEquipamiento,
                    Cantidad = d.Cantidad
                })
                .ToList();


            await _context.SaveChangesAsync();


            // =========================================================
            // MENSAJE
            // =========================================================

            TempData["MensajeExito"] =
                $"La solicitud {solicitud.NumeroReferencia} fue modificada correctamente.";


            // =========================================================
            // VOLVER A DETAILS
            // =========================================================

            return RedirectToAction(
                nameof(Details),
                new { id = solicitud.Id });
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