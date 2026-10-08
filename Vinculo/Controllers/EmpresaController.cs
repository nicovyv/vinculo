using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Vinculo.Data;
using Vinculo.Models;
using Vinculo.Models.ViewModels;
using Vinculo.Services;

namespace Vinculo.Controllers
{
    [Authorize(Roles = "Empresa")]
    public class EmpresaController : Controller
    {
        private readonly VinculoDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly NominatimService _nominatimService;


        // constructor
        public EmpresaController(VinculoDbContext context, UserManager<Usuario> userManager, SignInManager<Usuario> signInManager, NominatimService nominatimService)
        {
            _context = context;
            _userManager = userManager;
            _nominatimService = nominatimService;
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
                CodigoPostal = empresa.Domicilio?.CodigoPostal,
                Latitud = empresa.Domicilio?.Latitud,
                Longitud = empresa.Domicilio?.Longitud
            };

            return View(model);
        }



        [HttpGet]
        public async Task<IActionResult> MisDatos()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas
                .Include(e => e.Domicilio)
                .FirstOrDefaultAsync(e => e.UsuarioId == user.Id);

            // Si es la primera vez que ingresa, le mandamos el formulario vacío
            if (empresa == null)
                return View(new PerfilEmpresaViewModel());

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

        // POST: Empresa/MisDatos
        // POST: Empresa/MisDatos
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MisDatos(PerfilEmpresaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Usuario");

            var empresa = await _context.Empresas
                .Include(e => e.Domicilio)
                .FirstOrDefaultAsync(e => e.UsuarioId == user.Id);

            if (empresa == null)
            {
                return NotFound("Perfil corporativo no encontrado.");
            }

            // Actualizamos solo los datos permitidos
            empresa.Telefono = model.Telefono;
            empresa.Email = model.Email;

            if (empresa.Domicilio != null)
            {
                empresa.Domicilio.Calle = model.Calle;
                empresa.Domicilio.Numero = model.Numero;
                empresa.Domicilio.Localidad = model.Localidad;
                empresa.Domicilio.Provincia = model.Provincia;
                empresa.Domicilio.CodigoPostal = model.CodigoPostal;
                empresa.Domicilio.Latitud = model.Latitud;
                empresa.Domicilio.Longitud = model.Longitud;
            }

            _context.Empresas.Update(empresa);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MisDatos));
        }


        [HttpGet]
        public async Task<IActionResult> BuscarUbicacion(string calle, string numero, string localidad, string provincia, string? codigoPostal)
        {
            if (string.IsNullOrWhiteSpace(calle) || string.IsNullOrWhiteSpace(numero) || string.IsNullOrWhiteSpace(localidad) || string.IsNullOrWhiteSpace(provincia))
                return BadRequest(new { mensaje = "Completá calle, número, localidad y provincia." });

            var coordenadas = await _nominatimService.BuscarCoordenadasAsync(calle, numero, localidad, provincia, codigoPostal);
            if (coordenadas == null) return NotFound(new { mensaje = "No se encontró una ubicación para el domicilio ingresado." });

            return Json(new { latitud = coordenadas.Value.Latitud, longitud = coordenadas.Value.Longitud });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDireccion(double latitud, double longitud)
        {
            var resultado = await _nominatimService.ObtenerDireccionAsync(latitud, longitud);
            if (resultado?.Address == null) return NotFound(new { mensaje = "No se pudo determinar la dirección." });

            var direccion = resultado.Address;
            var localidad = direccion.City ?? direccion.Town ?? direccion.Village ?? direccion.Municipality;

            return Json(new { calle = direccion.Road, numero = direccion.HouseNumber, localidad = localidad, provincia = direccion.State, codigoPostal = direccion.Postcode });
        }


    }
}