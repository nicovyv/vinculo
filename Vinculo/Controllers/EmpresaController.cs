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
    [Authorize(Roles = "Empresa")]
    public class EmpresaController : Controller
    {
        private readonly VinculoDbContext _context;
        private readonly UserManager<Usuario> _userManager;


        // constructor
        public EmpresaController(VinculoDbContext context, UserManager<Usuario> userManager, SignInManager<Usuario> signInManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Empresa/Index
        public async Task<IActionResult> Index()
        {
            // Obtener el usuario actualmente logueado
            var user = await _userManager.GetUserAsync(User);

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