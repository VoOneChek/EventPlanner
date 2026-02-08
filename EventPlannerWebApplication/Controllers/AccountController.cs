using EventPlannerWebApplication.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventPlannerWebApplication.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost]
        public async Task<IActionResult> Register(string name, string email, string password, string confirmPassword)
        {
            try
            {
                await _accountService.SendVerificationCodeAsync(
                    HttpContext.Session,
                    name,
                    email,
                    password,
                    confirmPassword
                );
                return RedirectToAction("ConfirmCode");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ModalError"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult ConfirmCode()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCode(string code)
        {
            var user = await _accountService.ConfirmCodeAsync(HttpContext.Session, code);

            if (user == null)
            {
                ViewBag.Error = "Неверный код";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            return RedirectToAction("Menu", "Home");
        }

        [HttpPost]
        public IActionResult CancelRegistration()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _accountService.LoginAsync(email, password);

            if (user == null)
            {
                TempData["ModalError"] = "Неверный логин или пароль";
                return RedirectToAction("Index", "Home");
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            return RedirectToAction("Menu", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Home");

            var user = _accountService.GetUserByIdAsync((int)userId);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string name, string email, string password, string confirmPassword)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Index", "Home");

            var user = _accountService.GetUserByIdAsync(userId.Value).Result;

            // Если почта меняется отправляем код
            if (email != user!.Email)
            {
                try
                {
                    await _accountService.SendVerificationCodeAsync(
                        HttpContext.Session,
                        name,
                        email,
                        password,
                        confirmPassword,
                        user.Id
                    );
                    return RedirectToAction("ConfirmCode");
                }
                catch (InvalidOperationException ex)
                {
                    TempData["ModalError"] = ex.Message;
                    return RedirectToAction("Menu", "Home");
                }
            }
            else
            {
                // Если почта НЕ меняется просто валидируем и обновляем
                var error = await _accountService.ValidateProfileAsync(name, email, password, confirmPassword, user.Id);
                if (error != null)
                {
                    TempData["ModalError"] = error;
                    return RedirectToAction("Menu", "Home");
                }

                user.Name = name;
                try
                {
                    await _accountService.UpdateDirectAsync(user, password);
                    TempData["ModalSuccess"] = "Профиль успешно обновлён";
                }
                catch (InvalidOperationException ex)
                {
                    TempData["ModalError"] = ex.Message;
                }
            }

            return RedirectToAction("Menu", "Home");
        }
    }
}