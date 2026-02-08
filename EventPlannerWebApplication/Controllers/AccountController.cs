using EventPlannerWebApplication.Data;
using EventPlannerWebApplication.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Cryptography;
using System.Text;

namespace EventPlannerWebApplication.Controllers
{
    public class AccountController: Controller
    {
        private readonly EventPlannerDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(EventPlannerDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost]
        public IActionResult Register(string name, string email, string password, string confirmPassword)
        {

            if (SendMessage(name, email, password, confirmPassword).Result)
                return RedirectToAction("ConfirmCode");
            else
                return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ConfirmCode()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCode(string code)
        {
            var sessionCode = HttpContext.Session.GetString("RegisterCode");

            if (sessionCode == null || code != sessionCode)
            {
                ViewBag.Error = "Неверный код";
                return View();
            }

            User? user = null;
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                user = new User
                {
                    Name = HttpContext.Session.GetString("RegisterName")!,
                    Email = HttpContext.Session.GetString("RegisterEmail")!,
                    Password = HttpContext.Session.GetString("RegisterPassword")!
                };

                _context.Users.Add(user);
            }
            else 
            { 
                user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    ViewBag.Error = "Ошибка получения данных";
                    return View();
                }

                user.Name = HttpContext.Session.GetString("RegisterName")!;
                user.Email = HttpContext.Session.GetString("RegisterEmail")!;
                if (HttpContext.Session.Keys.Contains("RegisterPassword"))
                    user.Password = HttpContext.Session.GetString("RegisterPassword")!;
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.Clear();

            if (user != null)
                HttpContext.Session.SetInt32("UserId", user.Id);
            else
                ViewBag.Error = "Ошибка сохранения данных";

            return RedirectToAction("Menu", "Home");
        }

        [HttpPost]
        public IActionResult CancelRegistration()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var hash = HashPassword(password);

            var user = _context.Users.FirstOrDefault(u =>
                u.Email == email && u.Password == hash);

            if (user == null)
            {
                TempData["ModalError"] = "Неверный логин или пароль";
                return RedirectToAction("Index", "Home");
            }

            HttpContext.Session.SetInt32("UserId", user.Id);

            return RedirectToAction("Menu", "Home");
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(
                sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Index", "Home");

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            return View(user);
        }


        [HttpPost]
        public IActionResult UpdateProfile(string name, string email, string password, string confirmPassword)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Index", "Home");

            var user = _context.Users.First(u => u.Id == userId);

            if (email != user.Email)
            {
                if (SendMessage(name, email, password, confirmPassword, user.Id).Result)
                    return RedirectToAction("ConfirmCode");
                else
                    return RedirectToAction("Index", "Home");
            }

            var error = ValidateProfile(name, email, password, confirmPassword, user.Id);
            if (error != null)
            {
                TempData["ModalError"] = error;
                return RedirectToAction("Menu", "Home");
            }

            user.Name = name;
            if (!string.IsNullOrWhiteSpace(password))
                user.Password = HashPassword(password);

            _context.SaveChanges();

            TempData["ModalSuccess"] = "Профиль успешно обновлён";
            return RedirectToAction("Menu", "Home");
        }

        #region Private Methods

        private string? ValidateProfile(string name, string email, string password, string confirmPassword, int userId = -1)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return "Имя и Email обязательны";

            if (!string.IsNullOrEmpty(password) || userId == -1)
            {
                if (password != confirmPassword)
                    return "Пароли не совпадают";

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                    return "Пароль минимум 6 символов";
            }

            if (_context.Users.Any(u => u.Email == email && u.Id != userId))
                return "Почта уже занята";

            return null;
        }

        private async Task<bool> SendMessage(string name, string email, string password, string confirmPassword, int userId = -1)
        {
            var error = ValidateProfile(name, email, password, confirmPassword, userId);
            if (error != null)
            {
                TempData["ModalError"] = error;
                return false;
            }
            var code = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("RegisterCode", code);
            HttpContext.Session.SetString("RegisterName", name);
            HttpContext.Session.SetString("RegisterEmail", email);
            if (!string.IsNullOrEmpty(password))
                HttpContext.Session.SetString("RegisterPassword", HashPassword(password));

            await _emailService.SendAsync(email, "Код подтверждения", $"Ваш код: {code}");
            return true;
        }

        #endregion
    }
}
