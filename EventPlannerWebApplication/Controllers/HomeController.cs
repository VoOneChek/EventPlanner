using EventPlannerWebApplication.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlannerWebApplication.Controllers
{
    public class HomeController: Controller
    {
        private readonly EventPlannerDbContext _context;

        public HomeController(EventPlannerDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
                return RedirectToAction("Menu", "Home");

            return View();
        }

        public IActionResult Menu()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId.HasValue)
            {
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);

                ViewBag.User = user;
            }
            else
                return RedirectToAction("Index", "Home");

            return View();
        }
    }
}
