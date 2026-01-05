using HotelBookingMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingMVC.Controllers
{
    public class HotelController : Controller
    {
        private readonly AppDbContext _context;

        public HotelController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Hotel
        public async Task<IActionResult> Index(string recherche, string ville)
        {
            var hotels = _context.Hotels.Where(h => h.EstActif);

            // Filtrer par recherche (nom ou description)
            if (!string.IsNullOrEmpty(recherche))
            {
                hotels = hotels.Where(h => h.Nom.Contains(recherche) || h.Description.Contains(recherche));
            }

            // Filtrer par ville
            if (!string.IsNullOrEmpty(ville))
            {
                hotels = hotels.Where(h => h.Ville == ville);
            }

            // Récupérer la liste des villes pour le filtre
            ViewBag.Villes = await _context.Hotels
                .Where(h => h.EstActif)
                .Select(h => h.Ville)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();

            return View(await hotels.OrderBy(h => h.Nom).ToListAsync());
        }

        // GET: /Hotel/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Chambres.Where(c => c.EstActive))
                .FirstOrDefaultAsync(h => h.Id == id && h.EstActif);

            if (hotel == null)
            {
                TempData["Error"] = "Hôtel introuvable";
                return RedirectToAction("Index");
            }

            return View(hotel);
        }
    }
}
