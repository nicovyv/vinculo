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
    public class UsuarioController : Controller
    {
        private readonly SignInManager<Usuario> _signInManager;
        private readonly UserManager<Usuario> _userManager;
        private readonly VinculoDbContext _context;
        private readonly NominatimService _nominatimService;


        public UsuarioController(SignInManager<Usuario> signInManager, UserManager<Usuario> userManager, VinculoDbContext context, NominatimService nominatimService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _nominatimService = nominatimService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {

                // Buscar usuario
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Intento de inicio de sesión no válido.");

                    return View(model);
                }

                // Verificar si está activo
                if (!user.Activo)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Tu cuenta se encuentra desactivada. Contactá al administrador.");

                    return View(model);
                }

                // Verificar contraseña
                var result = await _signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    isPersistent: false,
                    lockoutOnFailure: false);

                // Redirección  según el rol
                if (await _userManager.IsInRoleAsync(user, "Empresa"))
                    return RedirectToAction("Index", "Empresa");

                if (await _userManager.IsInRoleAsync(user, "Institucion"))
                    return RedirectToAction("Index", "Institucion");

                if (await _userManager.IsInRoleAsync(user, "Administrador"))
                    return RedirectToAction("Index", "Administrador");


                ModelState.AddModelError(string.Empty, "Intento de inicio de sesión no válido.");
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Usuario");
        }


        // --- REGISTRO ---
        [HttpGet]
        public IActionResult Registro()
        {
            // Devuelve la vista Registro.cshtml ubicada en Views/Usuario/
            return View(new RegistroOrganizacionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registro(RegistroOrganizacionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validar que el correo no exista
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "El correo electrónico ya se encuentra registrado.");
                return View(model);
            }

            // Validar que el cuit no exista
            bool cuitExiste = await _context.Empresas.AnyAsync(e => e.Cuit == model.Cuit) ||
                      await _context.Instituciones.AnyAsync(i => i.Cuit == model.Cuit);

            if (cuitExiste)
            {
                ModelState.AddModelError("Cuit", "El CUIT ingresado ya se encuentra registrado en otra organización.");
                return View(model);
            }

            // Determinar el Tipo de Usuario
            var tipoUsuario = model.TipoOrganizacion == "empresa" ? TipoUsuario.Empresa : TipoUsuario.Institucion;

            // Crear Credenciales Identity
            var newUser = new Usuario
            {
                UserName = model.Email,
                Email = model.Email,
                TipoUsuario = tipoUsuario,
                Activo = true,
                FechaAlta = DateTime.Now,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);

            if (result.Succeeded)
            {
                //  Asignar el Rol correspondiente
                await _userManager.AddToRoleAsync(newUser, tipoUsuario.ToString());

                // Crear Entidad de Domicilio
                var domicilio = new Domicilio
                {
                    Calle = model.Calle,
                    Numero = model.Numero,
                    Localidad = model.Localidad,
                    Provincia = model.Provincia,
                    CodigoPostal = model.CodigoPostal,
                    Latitud = model.Latitud,
                    Longitud = model.Longitud
                };

                // Crear Entidad Institución o Empresa
                if (tipoUsuario == TipoUsuario.Empresa)
                {
                    var empresa = new Empresa
                    {
                        UsuarioId = newUser.Id,
                        RazonSocial = model.NombreRazonSocial,
                        Cuit = model.Cuit,
                        Telefono = model.Telefono,
                        Email = model.Email,
                        PersonaContacto = model.PersonaContacto,
                        Domicilio = domicilio
                    };
                    _context.Empresas.Add(empresa);
                }
                else
                {
                    var institucion = new Institucion
                    {
                        UsuarioId = newUser.Id,
                        Nombre = model.NombreRazonSocial,
                        Cuit = model.Cuit,
                        Telefono = model.Telefono,
                        Email = model.Email,
                        PersonaContacto = model.PersonaContacto,
                        Domicilio = domicilio
                    };
                    _context.Instituciones.Add(institucion);
                }

                // Guardar las nuevas tablas relacionadas
                await _context.SaveChangesAsync();

                //  Iniciar sesión automáticamente después de registrarse
                await _signInManager.SignInAsync(newUser, isPersistent: false);

                // Redirigir al panel correspondiente
                if (tipoUsuario == TipoUsuario.Empresa)
                    return RedirectToAction("Index", "Empresa");
                else
                    return RedirectToAction("Index", "Institucion");
            }

            // Capturar errores de Identity (ej. contraseñas débiles)
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
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
            var loc = direccion.City ?? direccion.Town ?? direccion.Village ?? direccion.Municipality;

            return Json(new { calle = direccion.Road, numero = direccion.HouseNumber, localidad = loc, provincia = direccion.State, codigoPostal = direccion.Postcode });
        }



    }
}
