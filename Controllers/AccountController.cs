using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventTicketing.Models;
using EventTicketing.Services;

namespace EventTicketing.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogger _logger;

        public AccountController(AppDbContext context, IActivityLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(string fullName, string email, string phoneNumber, string password, string role)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(password))
                {
                    throw new ArgumentException("All fields are required.");
                }

                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ViewBag.Error = "Invalid email format (example: user@domain.com).";
                    return View();
                }

                if (!Regex.IsMatch(phoneNumber, @"^\+?[0-9]{10,15}$"))
                {
                    ViewBag.Error = "Invalid phone number! Enter between 10 and 15 digits.";
                    return View();
                }

                if (_context.Users.Any(u => u.Email == email))
                {
                    ViewBag.Error = "This email is already registered.";
                    return View();
                }

                var newUser = new User
                {
                    FullName = fullName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    PasswordHash = HashPassword(password),
                    Role = (role == "Organizer") ? "Organizer" : "Customer",
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                _logger.LogAction(email, $"User registered with role {newUser.Role}");

                TempData["Success"] = "Account created successfully! Please log in.";
                return RedirectToAction("Login");
            }
            catch (ArgumentException ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogAction(email ?? "Unknown", $"Database error on register: {ex.Message}");
                ViewBag.Error = "A database error occurred while saving your account.";
                return View();
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter both email and password.";
                return View();
            }

            string hashedPassword = HashPassword(password);
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.PasswordHash == hashedPassword);

            if (user == null)
            {
                _logger.LogAction(email, "Failed login attempt");
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", user.IdUser);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role);

            _logger.LogAction(user.Email, "User logged in successfully");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? "Unknown";
            _logger.LogAction(email, "User logged out");

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}