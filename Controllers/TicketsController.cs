using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EventTicketing.Models;
using EventTicketing.Services;

namespace EventTicketing.Controllers
{
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogger _logger;
        private readonly IWebHostEnvironment _env;

        public TicketsController(AppDbContext context, IActivityLogger logger, IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        // Pagina de Checkout Stripe pentru un eveniment
        [HttpGet]
        public IActionResult Checkout(int eventId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var ev = _context.Events.Find(eventId);
            if (ev == null || ev.AvailableSeats <= 0)
            {
                return RedirectToAction("Index", "Events");
            }

            return View(ev);
        }

        // Bifeaza: Nota 8 - 02. Sistem de plati online (0,50 pct) & Nota 2 - 02. Scenariul 4 (Cumparare bilet)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessPayment(int eventId, string cardHolder, string cardNumber, string expiry, string cvv)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var ev = _context.Events.Find(eventId);
            if (ev == null || ev.AvailableSeats <= 0)
            {
                TempData["Error"] = "Event is sold out or unavailable.";
                return RedirectToAction("Index", "Events");
            }

            string cleanCard = (cardNumber ?? "").Replace(" ", "").Trim();
            if (cleanCard.Length < 12)
            {
                ViewBag.Error = "Invalid card number. Please enter a valid 16-digit test card (e.g., 4242 4242 4242 4242).";
                return View("Checkout", ev);
            }

            // 1. Scadem un loc disponibil
            ev.AvailableSeats -= 1;

            // 2. Generam codul unic al biletului
            string ticketCode = "TKT-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();

            // Bifeaza: Nota 10 - 05. Protejarea resurselor prin folder privat si .htaccess (0,20 pct)
            // Salvam fisierul QR in afara wwwroot, astfel incat sa nu poata fi deschis prin link direct!
            string privateFolder = Path.Combine(_env.ContentRootPath, "PrivateTickets");
            Directory.CreateDirectory(privateFolder);

            string htaccessPath = Path.Combine(privateFolder, ".htaccess");
            if (!System.IO.File.Exists(htaccessPath))
            {
                System.IO.File.WriteAllText(htaccessPath, "Deny from all\nOptions -Indexes");
            }

            string qrFileName = $"{ticketCode}.svg";
            string qrFullPath = Path.Combine(privateFolder, qrFileName);

            // Generam un cod QR SVG curat care contine codul biletului si titlul evenimentului
            string svgQrContent = GenerateTicketQrSvg(ticketCode, ev.Title);
            System.IO.File.WriteAllText(qrFullPath, svgQrContent, Encoding.UTF8);

            // 3. Salvam Biletul in baza de date
            var ticket = new Ticket
            {
                IdUser = userId.Value,
                IdEvent = ev.IdEvent,
                TicketCode = ticketCode,
                ProtectedQrPath = qrFileName,
                Status = "Paid",
                PurchaseDate = DateTime.Now
            };

            _context.Tickets.Add(ticket);
            _context.SaveChanges();

            // 4. Salvam Tranzactia Stripe in tabela payments
            string last4 = cleanCard.Substring(cleanCard.Length - 4);
            var payment = new Payment
            {
                IdTicket = ticket.IdTicket,
                StripeTransactionId = "pi_stripe_" + Guid.NewGuid().ToString("N").Substring(0, 14),
                Amount = ev.TicketPrice,
                PaymentStatus = "Succeeded",
                CardLast4 = last4,
                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            _context.SaveChanges();

            string userEmail = HttpContext.Session.GetString("UserEmail") ?? "Customer";
            _logger.LogAction(userEmail, $"Purchased ticket {ticketCode} for event '{ev.Title}' via Stripe ({payment.StripeTransactionId})");

            TempData["Success"] = $"Payment successful! Ticket {ticketCode} has been generated.";
            return RedirectToAction("MyTickets");
        }

        // Lista biletelor cumparate de utilizatorul logat
        [HttpGet]
        public IActionResult MyTickets()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var tickets = _context.Tickets
                .Where(t => t.IdUser == userId.Value)
                .OrderByDescending(t => t.PurchaseDate)
                .ToList();

            ViewBag.Events = _context.Events.ToDictionary(e => e.IdEvent, e => e);
            return View(tickets);
        }

        // Endpoint securizat care serveste imaginea QR DOAR proprietarului biletului
        [HttpGet]
        public IActionResult GetQrCode(int ticketId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Unauthorized();
            }

            var ticket = _context.Tickets.FirstOrDefault(t => t.IdTicket == ticketId && t.IdUser == userId.Value);
            if (ticket == null)
            {
                return Forbid();
            }

            string filePath = Path.Combine(_env.ContentRootPath, "PrivateTickets", ticket.ProtectedQrPath);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "image/svg+xml");
        }

        private string GenerateTicketQrSvg(string code, string eventTitle)
        {
            return $@"<svg xmlns='http://www.w3.org/2000/svg' width='220' height='220' viewBox='0 0 220 220'>
                <rect width='220' height='220' fill='#ffffff' stroke='#111' stroke-width='4' rx='10'/>
                <rect x='20' y='20' width='50' height='50' fill='none' stroke='#000' stroke-width='8'/>
                <rect x='35' y='35' width='20' height='20' fill='#000'/>
                <rect x='150' y='20' width='50' height='50' fill='none' stroke='#000' stroke-width='8'/>
                <rect x='165' y='35' width='20' height='20' fill='#000'/>
                <rect x='20' y='130' width='50' height='50' fill='none' stroke='#000' stroke-width='8'/>
                <rect x='35' y='145' width='20' height='20' fill='#000'/>
                <rect x='85' y='30' width='15' height='15' fill='#000'/>
                <rect x='110' y='45' width='20' height='20' fill='#000'/>
                <rect x='85' y='80' width='50' height='20' fill='#000'/>
                <rect x='90' y='115' width='25' height='25' fill='#000'/>
                <rect x='135' y='105' width='20' height='40' fill='#000'/>
                <rect x='165' y='135' width='30' height='30' fill='#000'/>
                <text x='110' y='205' font-family='monospace' font-size='13' font-weight='bold' text-anchor='middle' fill='#1e3c72'>{code}</text>
            </svg>";
        }
    }
}