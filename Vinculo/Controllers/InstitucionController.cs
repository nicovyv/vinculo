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
    public class InstitucionController : Controller
    {
        private readonly VinculoDbContext _context;
        private readonly UserManager<Usuario> _userManager;

    public InstitucionController(
        VinculoDbContext context,
        UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Institucion/Index
        // Dashboard principal de la institución
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

            var model = new InstitucionDashboardViewModel
            {
                InstitucionId = institucion.Id,

                NombreInstitucion = institucion.Nombre,

                TotalSolicitudes = solicitudes.Count,

                SolicitudesPendientes = solicitudes.Count(
                    s => s.Estado == EstadoSolicitud.Pendiente),

                SolicitudesEnCoordinacion = solicitudes.Count(
                    s => s.Estado == EstadoSolicitud.EnCoordinacion),

                SolicitudesConcretadas = solicitudes.Count(
                    s => s.Estado == EstadoSolicitud.Concretado),

                EquipamientosSolicitados = solicitudes
                    .SelectMany(s => s.Equipamientos)
                    .Sum(d => d.Cantidad),

                SolicitudesRecientes = solicitudes
                    .Take(5)
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
                    .ToList()
            };

            return View(model);
        }

        // GET: Institucion/MisDatos
        [HttpGet]
        public async Task<IActionResult> MisDatos()
        {
            var institucion = await ObtenerInstitucionActual();

            if (institucion == null)
            {
                return NotFound();
            }

            var model = new PerfilInstitucionViewModel
            {
                Id = institucion.Id,
                Nombre = institucion.Nombre,
                Cuit = institucion.Cuit,
                Telefono = institucion.Telefono,
                Email = institucion.Email,

                Calle = institucion.Domicilio?.Calle,
                Numero = institucion.Domicilio?.Numero,
                Localidad = institucion.Domicilio?.Localidad,
                Provincia = institucion.Domicilio?.Provincia,
                CodigoPostal = institucion.Domicilio?.CodigoPostal
            };

            return View(model);
        }

        // POST: Institucion/MisDatos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MisDatos(
            PerfilInstitucionViewModel model)
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

            // Datos de la institución
            institucion.Nombre = model.Nombre;
            institucion.Cuit = model.Cuit;
            institucion.Telefono = model.Telefono;
            institucion.Email = model.Email;

            // Datos del domicilio
            if (institucion.Domicilio == null)
            {
                institucion.Domicilio = new Domicilio();
            }

            institucion.Domicilio.Calle = model.Calle;
            institucion.Domicilio.Numero = model.Numero;
            institucion.Domicilio.Localidad = model.Localidad;
            institucion.Domicilio.Provincia = model.Provincia;
            institucion.Domicilio.CodigoPostal = model.CodigoPostal;

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] =
                "Los datos de la institución fueron actualizados correctamente.";

            return RedirectToAction(nameof(MisDatos));
        }

        // Obtiene la institución asociada al usuario actualmente autenticado.
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
    }
}