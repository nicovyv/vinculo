using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinculo.Data;
using Vinculo.Models;
using Vinculo.Models.Enums;
using Vinculo.Models.ViewModels;
using Vinculo.Services;

namespace Vinculo.Controllers
{
    [Authorize(Roles = "Institucion")]
    public class InstitucionController : Controller
    {
        private readonly VinculoDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly NominatimService _nominatimService;

        public InstitucionController(
         VinculoDbContext context,
         UserManager<Usuario> userManager,
         NominatimService nominatimService)
        {
            _context = context;
            _userManager = userManager;
            _nominatimService = nominatimService;
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
                SolicitudesCanceladas = solicitudes.Count(
                    s => s.Estado == EstadoSolicitud.Cancelada),

                EquipamientosSolicitados = solicitudes
                    .Where(s => s.Estado != EstadoSolicitud.Cancelada)
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
                CodigoPostal = institucion.Domicilio?.CodigoPostal,
                Latitud = institucion.Domicilio?.Latitud,
                Longitud = institucion.Domicilio?.Longitud
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
            institucion.Domicilio.Latitud = model.Latitud;
            institucion.Domicilio.Longitud = model.Longitud;

            await _context.SaveChangesAsync();

            TempData["MensajeExito"] =
                "Los datos de la institución fueron actualizados correctamente.";

            return RedirectToAction(nameof(MisDatos));
        }

        // GET: Institucion/BuscarUbicacion
        [HttpGet]
        public async Task<IActionResult> BuscarUbicacion(
            string calle,
            string numero,
            string localidad,
            string provincia,
            string? codigoPostal)
        {
            if (string.IsNullOrWhiteSpace(calle) ||
                string.IsNullOrWhiteSpace(numero) ||
                string.IsNullOrWhiteSpace(localidad) ||
                string.IsNullOrWhiteSpace(provincia))
            {
                return BadRequest(new
                {
                    mensaje = "Completá calle, número, localidad y provincia."
                });
            }

            var coordenadas = await _nominatimService.BuscarCoordenadasAsync(
                calle,
                numero,
                localidad,
                provincia,
                codigoPostal);

            if (coordenadas == null)
            {
                return NotFound(new
                {
                    mensaje = "No se encontró una ubicación para el domicilio ingresado."
                });
            }

            return Json(new
            {
                latitud = coordenadas.Value.Latitud,
                longitud = coordenadas.Value.Longitud
            });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDireccion(double latitud, double longitud)
        {
            var resultado = await _nominatimService
                .ObtenerDireccionAsync(latitud, longitud);

            if (resultado?.Address == null)
            {
                return NotFound(new
                {
                    mensaje = "No se pudo determinar la dirección."
                });
            }

            var direccion = resultado.Address;

            var localidad =
                direccion.City ??
                direccion.Town ??
                direccion.Village ??
                direccion.Municipality;

            return Json(new
            {
                calle = direccion.Road,
                numero = direccion.HouseNumber,
                localidad = localidad,
                provincia = direccion.State,
                codigoPostal = direccion.Postcode
            });
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