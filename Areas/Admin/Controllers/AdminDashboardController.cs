using HotelBookingMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _context;

        public AdminDashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                Hotels = await _context.Hotels.CountAsync(),
                Chambres = await _context.Chambres.CountAsync(),
                Clients = await _context.Utilisateurs.CountAsync(u => u.Role == "Client"),
                Reservations = await _context.Reservations.CountAsync(),
                TotalPaiements = await _context.Paiements.SumAsync(p => (decimal?)p.Montant) ?? 0m
            };

            ViewBag.Stats = stats;

            var dernieresReservations = await _context.Reservations
                .Include(r => r.Hotel)
                .Include(r => r.Client)
                .OrderByDescending(r => r.DateCreation)
                .Take(10)
                .ToListAsync();

            return View(dernieresReservations);
        }
    }
}
