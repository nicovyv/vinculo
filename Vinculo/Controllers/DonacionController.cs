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
using Vinculo.Services; // Espacio de nombres de tu nuevo servicio

namespace Vinculo.Controllers
{
    [Authorize(Roles = "Empresa")]
    public class DonacionesController : Controller
    {
        private readonly VinculoDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IImagenStorage _imagenStorage;

        // Inyectamos IImagenStorage en lugar de IWebHostEnvironment
        public DonacionesController(
            VinculoDbContext context,
            UserManager<Usuario> userManager,
            IImagenStorage imagenStorage)
        {
            _context = context;
            _userManager = userManager;
            _imagenStorage = imagenStorage;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CrearDonacionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CrearDonacionViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.UsuarioId == user.Id);
            if (empresa == null) return NotFound("Debe completar su perfil corporativo antes de donar.");

            // 1. Usar el servicio para guardar la foto
            string rutaRelativaBd = null;
            if (model.FotoArchivo != null)
            {
                rutaRelativaBd = await _imagenStorage.GuardarImagenAsync(model.FotoArchivo, "donaciones");
            }

            // 2. Crear la donación con la ruta obtenida
            var nuevaDonacion = new Donacion
            {
                EmpresaId = empresa.Id,
                TipoEquipamiento = model.Tipo, // O TipoEquipamiento, según el nombre exacto en tu entidad y ViewModel
                Descripcion = model.Descripcion,
                FotoRuta = rutaRelativaBd,
                Fecha = DateTime.Now,
                Estado = EstadoDonacion.Disponible
            };

            _context.Donaciones.Add(nuevaDonacion);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}