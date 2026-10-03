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

            // La vista usa ViewBag.Institucion
            // para mostrar Nombre, CUIT, teléfono,
            // email y domicilio.
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
            // -----------------------------------------------------
            // 1. Obtener la institución del usuario logueado
            // -----------------------------------------------------

            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // 2. Asegurarnos de que Detalles no sea null
            // -----------------------------------------------------

            model.Detalles ??= new List<DetalleSolicitudViewModel>();


            // -----------------------------------------------------
            // 3. Tomar solamente detalles con cantidad válida
            // -----------------------------------------------------

            model.Detalles = model.Detalles
                .Where(d => d.Cantidad > 0)
                .ToList();


            // -----------------------------------------------------
            // 4. Validar que exista al menos un equipamiento
            // -----------------------------------------------------

            if (!model.Detalles.Any())
            {
                ModelState.AddModelError(
                    nameof(model.Detalles),
                    "Debe agregar al menos un equipamiento.");
            }


            // -----------------------------------------------------
            // 5. Si hay errores, volvemos a mostrar Create
            //    SIN guardar nada
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                ViewBag.Institucion = institucion;

                return View(model);
            }


            // -----------------------------------------------------
            // 6. Crear la entidad Solicitud
            // -----------------------------------------------------

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


            // -----------------------------------------------------
            // 7. Guardar en la base de datos
            // -----------------------------------------------------

            _context.Solicitudes.Add(solicitud);

            await _context.SaveChangesAsync();


            // -----------------------------------------------------
            // 8. Mensaje para el listado
            // -----------------------------------------------------

            TempData["MensajeExito"] =
                $"La solicitud {solicitud.NumeroReferencia} fue creada correctamente.";


            // -----------------------------------------------------
            // 9. MUY IMPORTANTE:
            //    después de guardar NO volvemos a Create.
            //
            //    Vamos al listado de solicitudes.
            // -----------------------------------------------------

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
