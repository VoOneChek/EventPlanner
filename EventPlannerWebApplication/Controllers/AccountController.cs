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
        public async Task<IActionResult> Register(string name, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                TempData["ModalError"] = "Пароли не совпадают";
                return RedirectToAction("Index", "Home");
            }

            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["ModalError"] = "Пользователь с такой почтой уже существует";
                return RedirectToAction("Index", "Home");
            }

            var code = new Random().Next(100000, 999999).ToString();

            HttpContext.Session.SetString("RegisterCode", code);
            HttpContext.Session.SetString("RegisterName", name);
            HttpContext.Session.SetString("RegisterEmail", email);
            HttpContext.Session.SetString("RegisterPassword", HashPassword(password));

            await _emailService.SendAsync(email, "Код подтверждения", $"Ваш код: {code}");

            return RedirectToAction("ConfirmCode");
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

            var user = new User
            {
                Name = HttpContext.Session.GetString("RegisterName")!,
                Email = HttpContext.Session.GetString("RegisterEmail")!,
                Password = HttpContext.Session.GetString("RegisterPassword")!
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            HttpContext.Session.Clear();

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
    }
}
