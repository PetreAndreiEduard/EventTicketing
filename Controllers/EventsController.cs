using EventTicketing.Models;
using EventTicketing.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EventTicketing.Models;
using System;
using System.IO;
using System.Linq;

namespace EventTicketing.Controllers
{
    public class EventsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogger _logger;
        private readonly IWebHostEnvironment _env;

        public EventsController(AppDbContext context, IActivityLogger logger, IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index(string? search, int? categoryId)
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = categoryId;

            var query = _context.Events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Title.Contains(search) || e.Location.Contains(search));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                var eventIds = _context.EventCategories
                    .Where(ec => ec.IdCategory == categoryId.Value)
                    .Select(ec => ec.IdEvent);
                query = query.Where(e => eventIds.Contains(e.IdEvent));
            }

            var events = query.OrderBy(e => e.EventDate).ToList();
            return View(events);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("UserRole") != "Organizer")
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string title, string description, string location, DateTime eventDate, decimal ticketPrice, int availableSeats, int[] categoryIds, IFormFile? posterFile)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organizer")
            {
                return Unauthorized();
            }

            ViewBag.Categories = _context.Categories.ToList();

            if (posterFile == null || posterFile.Length == 0)
            {
                ViewBag.Error = "Please upload a poster image.";
                return View();
            }

            const long maxFileSize = 12 * 1024 * 1024;
            if (posterFile.Length > maxFileSize)
            {
                ViewBag.Error = "File size exceeds the 12 MB limit!";
                return View();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(posterFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ViewBag.Error = "Only .jpg, .jpeg, and .png files are allowed!";
                return View();
            }

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                posterFile.CopyTo(stream);
            }

            int organizerId = HttpContext.Session.GetInt32("UserId") ?? 0;

            var newEvent = new Event
            {
                IdOrganizer = organizerId,
                Title = title,
                Description = description,
                Location = location,
                EventDate = eventDate,
                TicketPrice = ticketPrice,
                AvailableSeats = availableSeats,
                PosterImagePath = "/uploads/" + uniqueFileName
            };

            _context.Events.Add(newEvent);
            _context.SaveChanges();

            if (categoryIds != null && categoryIds.Length > 0)
            {
                foreach (var catId in categoryIds)
                {
                    _context.EventCategories.Add(new EventCategory
                    {
                        IdEvent = newEvent.IdEvent,
                        IdCategory = catId
                    });
                }
                _context.SaveChanges();
            }

            string userEmail = HttpContext.Session.GetString("UserEmail") ?? "Organizer";
            _logger.LogAction(userEmail, $"Created event: {title}");

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organizer")
            {
                return Unauthorized();
            }

            var ev = _context.Events.Find(id);
            if (ev != null)
            {
                _context.Events.Remove(ev);
                _context.SaveChanges();

                string userEmail = HttpContext.Session.GetString("UserEmail") ?? "Organizer";
                _logger.LogAction(userEmail, $"Deleted event ID {id}: {ev.Title}");
            }

            return RedirectToAction("Index");
        }
    }
}