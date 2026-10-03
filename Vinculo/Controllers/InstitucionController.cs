using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinculo.Data;
using Vinculo.Models;
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

        // GET: Institucion/MiPerfil
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var institucion = await _context.Instituciones
                .Include(i => i.Domicilio)
                .FirstOrDefaultAsync(i => i.UsuarioId == user.Id);

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

        // POST: Institucion/MiPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MiPerfil(PerfilInstitucionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync("institucion@vinculo.com");

            if (user == null)
            {
                return NotFound();
            }

            var institucion = await _context.Instituciones
                .Include(i => i.Domicilio)
                .FirstOrDefaultAsync(i => i.UsuarioId == user.Id);

            if (institucion == null)
            {
                return NotFound();
            }

            // Actualizar datos de la institución
            institucion.Nombre = model.Nombre;
            institucion.Cuit = model.Cuit;
            institucion.Telefono = model.Telefono;
            institucion.Email = model.Email;

            // Actualizar domicilio
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

            return RedirectToAction(nameof(MiPerfil));
        }


    }
}