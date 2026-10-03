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
        private readonly SignInManager<Usuario> _signInManager; // 1. Agregamos SignInManager

        // 2. Lo inyectamos en el constructor
        public EmpresaController(VinculoDbContext context, UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Empresa/Index
        public async Task<IActionResult> Index()
        {
            // Obtener el usuario actualmente logueado (leyendo la cookie)
            var user = await _userManager.GetUserAsync(User);

            // 3. Si no hay cookie, buscamos al usuario Y LO LOGUEAMOS REALMENTE
            if (user == null)
            {
                user = await _userManager.FindByEmailAsync("empresa@vinculo.com");

                if (user != null)
                {
                    // Esto crea la cookie de sesión en tu navegador con el rol correspondiente
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Recargamos la página para que el _Layout.cshtml lea la cookie nueva y pinte el menú
                    return RedirectToAction(nameof(Index));
                }
            }

            if (user == null)
                return Unauthorized("El usuario no está autenticado.");

            var empresa = await _context.Empresas
                .Include(e => e.Domicilio)
                .FirstOrDefaultAsync(e => e.UsuarioId == user.Id);

            if (empresa == null)
                return NotFound("No se encontró la empresa asociada a este usuario.");

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