using Microsoft.AspNetCore.Mvc;

namespace web_MVC.Controllers
{
    public class LoginController: Controller
    {
        public IActionResult Index()
        {
            ViewData["ShowSidebar"] = false;
            return View();
        }

        public IActionResult Register()
        {
            ViewData["ShowSidebar"] = false;
            return View();
        }
    }
}
