using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Vinculo.Models;
using Vinculo.Models.ViewModels;

namespace Vinculo.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly SignInManager<Usuario> _signInManager;
        private readonly UserManager<Usuario> _userManager;

        public UsuarioController(SignInManager<Usuario> signInManager, UserManager<Usuario> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
    }
}
