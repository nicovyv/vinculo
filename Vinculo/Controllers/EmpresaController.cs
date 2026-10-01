using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Vinculo.Data; 
using Vinculo.Models;
using Vinculo.Models.ViewModels;

namespace Vinculo.Controllers
{
    public class EmpresaController : Controller
    {
        private readonly VinculoDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public EmpresaController(VinculoDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Empresas/MiPerfil
        //[Authorize(Roles = "Empresa")]
        public async Task<IActionResult> MiPerfil()
        {
            // Obtener el ID del usuario actualmente logueado
            // var user = await _userManager.GetUserAsync(User);
            var user = await _userManager.FindByEmailAsync("empresa@vinculo.com");

            var empresa = await _context.Empresas
                .Include(e => e.Domicilio)
                .FirstOrDefaultAsync(e => e.UsuarioId == user.Id);

            //if (empresa == null) return NotFound();
            return View(new PerfilEmpresaViewModel());

            // Mapear la entidad al ViewModel para mostrarlo en pantalla
            var model = new PerfilEmpresaViewModel
            {
                Id = empresa.Id,
                RazonSocial = empresa.RazonSocial,
                Cuit = empresa.Cuit,
                Telefono = empresa.Telefono,
                Email = empresa.Email,
                Calle = empresa.Domicilio?.Calle,
                Numero = empresa.Domicilio?.Numero,
                Localidad = empresa.Domicilio?.Localidad,
                Provincia = empresa.Domicilio?.Provincia,
                CodigoPostal = empresa.Domicilio?.CodigoPostal
            };

            return View(model);
        }
    }
}
