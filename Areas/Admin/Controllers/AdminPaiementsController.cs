using HotelBookingMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminPaiementsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminPaiementsController(AppDbContext context)
        {
            _context = context;
        }

        // LISTE
        public async Task<IActionResult> Index(string statut = "", string mode = "")
        {
            var paiements = _context.Paiements
                .Include(p => p.Reservation)
                    .ThenInclude(r => r.Client)
                .Include(p => p.Reservation)
                    .ThenInclude(r => r.Hotel)
                .AsQueryable();

            // Filtre par statut
            if (!string.IsNullOrEmpty(statut))
            {
                paiements = paiements.Where(p => p.Statut == statut);
            }

            // Filtre par mode
            if (!string.IsNullOrEmpty(mode))
            {
                paiements = paiements.Where(p => p.Mode == mode);
            }

            var liste = await paiements
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();

            ViewBag.StatutFiltre = statut;
            ViewBag.ModeFiltre = mode;

            // Calculer les statistiques
            ViewBag.TotalPaiements = liste.Sum(p => p.Montant);
            ViewBag.NbPaiements = liste.Count;

            return View(liste);
        }

        // DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var paiement = await _context.Paiements
                .Include(p => p.Reservation)
                    .ThenInclude(r => r.Client)
                .Include(p => p.Reservation)
                    .ThenInclude(r => r.Hotel)
                .Include(p => p.Reservation)
                    .ThenInclude(r => r.Lignes)
                        .ThenInclude(l => l.Chambre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (paiement == null) return NotFound();

            return View(paiement);
        }
    }
}
