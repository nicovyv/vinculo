using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Vinculo.Data;
using Vinculo.Models;
using Vinculo.Models.Enums;
using Vinculo.Models.ViewModels;
using Vinculo.Services;

namespace Vinculo.Controllers
{
    [Authorize(Roles = "Empresa")]
    public class DonacionController : Controller
    {
        private readonly VinculoDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IImagenStorage _imagenStorage;

        // Inyectamos IImagenStorage en lugar de IWebHostEnvironment
        public DonacionController(
            VinculoDbContext context,
            UserManager<Usuario> userManager,
            IImagenStorage imagenStorage)
        {
            _context = context;
            _userManager = userManager;
            _imagenStorage = imagenStorage;
        }


        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.UsuarioId == user.Id);
            if (empresa == null) return NotFound("Debe completar su perfil antes de ver sus donaciones.");

            // Consulta con "Eager Loading" para traer toda la cadena de datos hacia el Modal
            var donaciones = await _context.Donaciones
                .Include(d => d.Asignaciones)
                    .ThenInclude(a => a.Solicitud)
                        .ThenInclude(s => s.Institucion)
                            .ThenInclude(i => i.Domicilio)
                .Where(d => d.EmpresaId == empresa.Id)
                .OrderByDescending(d => d.Fecha)
                .ToListAsync();

            return View(donaciones);
        }

        [HttpGet]
        public IActionResult RegistrarDonacion()
        {
            return View(new RegistrarDonacionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarDonacion(RegistrarDonacionViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.UsuarioId == user.Id);
            if (empresa == null) return NotFound("Debe completar su perfil antes de donar.");

            string rutaRelativaBd = null;
            if (model.FotoArchivo != null)
            {
                rutaRelativaBd = await _imagenStorage.GuardarImagenAsync(model.FotoArchivo, "donaciones");
            }

            var nuevaDonacion = new Donacion
            {
                NumeroReferencia = GenerarReferenciaDonacion(), 
                EmpresaId = empresa.Id,
                Tipo = model.Tipo,
                Cantidad = model.Cantidad,
                Descripcion = model.Descripcion,
                FotoRuta = rutaRelativaBd,
                Fecha = DateTime.Now,
                Estado = EstadoDonacion.Disponible
            };

            _context.Donaciones.Add(nuevaDonacion);
            await _context.SaveChangesAsync();

            // Redirigir al motor de matching pasando el ID generado
            return RedirectToAction(nameof(Coincidencias), new { id = nuevaDonacion.Id });
        }



        [HttpGet]
        public async Task<IActionResult> Coincidencias(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas
                .Include(e => e.Domicilio)
                .FirstOrDefaultAsync(e => e.UsuarioId == user.Id);

            var donacion = await _context.Donaciones
                .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresa.Id);

            if (donacion == null) return NotFound("Donación no encontrada.");

            // 1. Buscamos las solicitudes candidatas
            var solicitudesCandidatas = await _context.Solicitudes
                .Include(s => s.Institucion)
                    .ThenInclude(i => i.Domicilio)
                .Include(s => s.Equipamientos)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente &&
                            s.Equipamientos.Any(e => e.TipoEquipamiento == donacion.Tipo))
                .ToListAsync();

            // 2. Calculamos distancia y ordenamos (El Core del proyecto)
            var solicitudesConDistancia = solicitudesCandidatas.Select(s => new SolicitudConDistancia
            {
                Solicitud = s,
                DistanciaKm = Utils.GeoUtils.CalcularDistanciaKm(
                    empresa.Domicilio?.Latitud, empresa.Domicilio?.Longitud,
                    s.Institucion.Domicilio?.Latitud, s.Institucion.Domicilio?.Longitud
                )
            })
            .OrderBy(x => x.DistanciaKm) // Se ordenan de menor a mayor distancia
            .ToList();

            var model = new CoincidenciasViewModel
            {
                Donacion = donacion,
                Empresa = empresa,
                SolicitudesCompatibles = solicitudesConDistancia
            };

            return View(model);
        }

        // Metodo Asignar donacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(int donacionId, int solicitudId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Usuario");

            // Verificamos que la empresa autenticada sea la propietaria de la donación
            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.UsuarioId == user.Id);

            if (empresa == null)
                return NotFound("Debe completar su perfil antes de asignar una donación.");

            // La donación debe pertenecer a la empresa autenticada
            var donacion = await _context.Donaciones
                .FirstOrDefaultAsync(d =>
                    d.Id == donacionId &&
                    d.EmpresaId == empresa.Id);

            if (donacion == null)
                return NotFound("Donación no encontrada.");

            // La donación solo puede asignarse si está disponible
            if (donacion.Estado != EstadoDonacion.Disponible)
            {
                TempData["Error"] = "La donación ya no está disponible para asignación.";
                return RedirectToAction(nameof(Index));
            }

            // Buscamos la solicitud y sus equipamientos
            var solicitud = await _context.Solicitudes
                .Include(s => s.Equipamientos)
                .FirstOrDefaultAsync(s => s.Id == solicitudId);

            if (solicitud == null)
                return NotFound("Solicitud no encontrada.");

            // La solicitud debe estar pendiente
            if (solicitud.Estado != EstadoSolicitud.Pendiente)
            {
                TempData["Error"] = "La solicitud ya no está pendiente de asignación.";
                return RedirectToAction(nameof(Coincidencias), new { id = donacion.Id });
            }

            // Verificamos que la solicitud realmente requiera
            // el mismo tipo de equipamiento que estamos donando
            var detalleCompatible = solicitud.Equipamientos
                .FirstOrDefault(e => e.TipoEquipamiento == donacion.Tipo);

            if (detalleCompatible == null)
            {
                TempData["Error"] = "La donación no corresponde a ningún equipamiento solicitado.";
                return RedirectToAction(nameof(Coincidencias), new { id = donacion.Id });
            }

            // Creamos la asignación
            var asignacion = new Asignacion
            {
                NumeroReferencia = GenerarReferenciaAsignacion(),
                SolicitudId = solicitud.Id,
                DonacionId = donacion.Id,
                Fecha = DateTime.Now,
                Estado = EstadoAsignacion.Activa
            };

            _context.Asignaciones.Add(asignacion);

            // Actualizamos los estados relacionados
            donacion.Estado = EstadoDonacion.Asignada;
            solicitud.Estado = EstadoSolicitud.EnCoordinacion;

            await _context.SaveChangesAsync();

            TempData["Success"] = "La donación fue asignada correctamente.";

            return RedirectToAction(nameof(Index));
        }



        private string GenerarReferenciaDonacion()
        {
            return $"DON-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private string GenerarReferenciaAsignacion()
        {
            return $"ASIG-{DateTime.Now:yyyyMMddHHmmss}";
        }

    }
}