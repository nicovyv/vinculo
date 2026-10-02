using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinculo.Data;
using Vinculo.Models;
using Vinculo.Models.ViewModels;

namespace Vinculo.Controllers
{
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

        public async Task<IActionResult> MiPerfil()
        {
            // Obtener el ID del usuario actualmente logueado
            // var user = await _userManager.GetUserAsync(User);
            var user = await _userManager.FindByEmailAsync("institucion@vinculo.com");

            var institucion = await _context.Instituciones
                .Include(i => i.Domicilio)
                .FirstOrDefaultAsync(i => i.UsuarioId == user.Id);

            return View(new PerfilInstitucionViewModel());

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
    }
}