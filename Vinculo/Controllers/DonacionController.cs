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
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            // Buscamos la empresa y sus donaciones asociadas
            var empresa = await _context.Empresas
                .Include(e => e.Donaciones)
                .FirstOrDefaultAsync(e => e.UsuarioId == user.Id);

            if (empresa == null) return NotFound("Debe completar su perfil antes de ver sus donaciones.");

            // Ordenamos por fecha descendente (las más nuevas primero)
            var donaciones = empresa.Donaciones?.OrderByDescending(d => d.Fecha).ToList() ?? new List<Donacion>();

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

            // Buscar solicitudes que tengan el mismo tipo de equipamiento y estén pendientes
            var solicitudesCompatibles = await _context.Solicitudes
                .Include(s => s.Institucion)
                    .ThenInclude(i => i.Domicilio)
                .Include(s => s.Equipamientos)
                .Where(s => s.Estado == EstadoSolicitud.Pendiente &&
                            s.Equipamientos.Any(e => e.TipoEquipamiento == donacion.Tipo))
                .ToListAsync();

            // Crear el ViewModel (puedes crear esta clase en la carpeta ViewModels)
            var model = new CoincidenciasViewModel
            {
                Donacion = donacion,
                Empresa = empresa,
                SolicitudesCompatibles = solicitudesCompatibles
            };

            return View(model);
        }
    }
}