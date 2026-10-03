using Microsoft.AspNetCore.Mvc;

namespace Vinculo.Controllers
{
    public class UsuarioController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
