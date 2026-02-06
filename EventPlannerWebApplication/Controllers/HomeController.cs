using Microsoft.AspNetCore.Mvc;

namespace EventPlannerWebApplication.Controllers
{
    public class HomeController: Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Menu()
        {
            return View();
        }
    }
}
