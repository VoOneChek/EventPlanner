using EventPlannerWebApplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventPlannerWebApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAccountService _accountService;

        public HomeController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
                return RedirectToAction("Menu", "Home");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Menu()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId.HasValue)
            {
                var user = await _accountService.GetUserByIdAsync(userId.Value);

                if (user == null)
                {
                    HttpContext.Session.Clear();
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.User = user;
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}