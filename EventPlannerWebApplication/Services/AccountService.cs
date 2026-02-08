using EventPlannerWebApplication.Data;
using EventPlannerWebApplication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EventPlannerWebApplication.Services
{
    public interface IAccountService
    {
        Task<string?> ValidateProfileAsync(string name, string email, string password, string confirmPassword, int userId = -1);
        Task SendVerificationCodeAsync(ISession session, string name, string email, string password, string confirmPassword, int userId = -1);
        Task<User?> ConfirmCodeAsync(ISession session, string code);
        Task<User?> LoginAsync(string email, string password);
        Task UpdateDirectAsync(User user, string? newPassword = null);
        Task<User?> GetUserByIdAsync(int userId);
    }

    public class AccountService : IAccountService
    {
        private readonly EventPlannerDbContext _context;
        private readonly EmailService _emailService;

        public AccountService(EventPlannerDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<string?> ValidateProfileAsync(string name, string email, string password, string confirmPassword, int userId = -1)
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

            var emailExists = await _context.Users.AnyAsync(u => u.Email == email && u.Id != userId);
            if (emailExists)
                return "Почта уже занята";

            return null;
        }

        public async Task SendVerificationCodeAsync(ISession session, string name, string email, string password, string confirmPassword, int userId = -1)
        {
            var error = await ValidateProfileAsync(name, email, password, confirmPassword, userId);
            if (error != null)
            {
                throw new InvalidOperationException(error);
            }

            var code = new Random().Next(100000, 999999).ToString();

            session.SetString("RegisterCode", code);
            session.SetString("RegisterName", name);
            session.SetString("RegisterEmail", email);

            if (!string.IsNullOrEmpty(password))
            {
                session.SetString("RegisterPassword", HashPassword(password));
            }
            else
            {
                session.Remove("RegisterPassword");
            }

            await _emailService.SendAsync(email, "Код подтверждения", $"Ваш код: {code}");
        }

        public async Task<User?> ConfirmCodeAsync(ISession session, string code)
        {
            var sessionCode = session.GetString("RegisterCode");

            if (string.IsNullOrEmpty(sessionCode) || code != sessionCode)
                return null;

            var userId = session.GetInt32("UserId");

            User? user;

            if (userId == null)
            {
                // РЕГИСТРАЦИЯ
                user = new User
                {
                    Name = session.GetString("RegisterName")!,
                    Email = session.GetString("RegisterEmail")!,
                    Password = session.GetString("RegisterPassword")!
                };
                _context.Users.Add(user);
            }
            else
            {
                // ОБНОВЛЕНИЕ ПРОФИЛЯ
                user = await _context.Users.FindAsync(userId.Value);
                if (user == null) return null;

                user.Name = session.GetString("RegisterName")!;
                user.Email = session.GetString("RegisterEmail")!;

                var newPass = session.GetString("RegisterPassword");
                if (!string.IsNullOrEmpty(newPass))
                    user.Password = newPass;
            }

            await _context.SaveChangesAsync();

            session.Clear();

            return user;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var hash = HashPassword(password);
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == hash);
        }

        // Быстрое обновление без кода
        public async Task UpdateDirectAsync(User user, string? newPassword = null)
        {
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword.Length < 6)
                    throw new InvalidOperationException("Пароль минимум 6 символов");
                user.Password = HashPassword(newPassword);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }
}
