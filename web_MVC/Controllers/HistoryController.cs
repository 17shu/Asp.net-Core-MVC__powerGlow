using Microsoft.AspNetCore.Mvc;


namespace web_MVC.Controllers
{
    public class HistoryController:Controller
    {
        public IActionResult Index()
        {
            ViewData["ShowSidebar"] = false;
            return View();
        }

    }
}
