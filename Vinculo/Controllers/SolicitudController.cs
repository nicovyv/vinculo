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

        // GET: Solicitud
        public async Task<IActionResult> Index()
        {
            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }

            var solicitudes = await _context.Solicitudes
                .Include(s => s.Equipamientos)
                .Where(s => s.InstitucionId == institucion.Id)
                .OrderByDescending(s => s.Fecha)
                .ToListAsync();

            var model = solicitudes.Select(s => new SolicitudViewModel
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
            }).ToList();

            return View(model);
        }

        // GET: Solicitud/Details/5
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

        // GET: Solicitud/Create
        public IActionResult Create()
        {
            var model = new SolicitudViewModel
            {
                Fecha = DateTime.Now,
                Estado = EstadoSolicitud.Pendiente
            };

            // Agregamos un detalle inicial para que el formulario
            // tenga una fila de equipamiento disponible.
            model.Detalles.Add(new DetalleSolicitudViewModel());

            return View(model);
        }

        // POST: Solicitud/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SolicitudViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }

            if (model.Detalles == null || !model.Detalles.Any())
            {
                ModelState.AddModelError(
                    "Detalles",
                    "Debe agregar al menos un equipamiento.");

                return View(model);
            }

            var detallesValidos = model.Detalles
                .Where(d => d.Cantidad > 0)
                .ToList();

            if (!detallesValidos.Any())
            {
                ModelState.AddModelError(
                    "Detalles",
                    "Debe agregar al menos un equipamiento con cantidad mayor a 0.");

                return View(model);
            }

            var solicitud = new Solicitud
            {
                NumeroReferencia = GenerarNumeroReferencia(),
                InstitucionId = institucion.Id,
                Fecha = DateTime.Now,
                Estado = EstadoSolicitud.Pendiente,
                Equipamientos = detallesValidos
                    .Select(d => new DetalleSolicitud
                    {
                        TipoEquipamiento = d.TipoEquipamiento,
                        Cantidad = d.Cantidad
                    })
                    .ToList()
            };

            _context.Solicitudes.Add(solicitud);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<Institucion?> ObtenerInstitucionActual()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            return await _context.Instituciones
                .FirstOrDefaultAsync(i => i.UsuarioId == userId);
        }

        private string GenerarNumeroReferencia()
        {
            return $"SOL-{DateTime.Now:yyyyMMddHHmmss}";
        }
    }

}
