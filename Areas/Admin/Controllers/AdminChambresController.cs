using HotelBookingMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminChambresController : Controller
    {
        private readonly AppDbContext _context;

        public AdminChambresController(AppDbContext context)
        {
            _context = context;
        }

        // LISTE
        public async Task<IActionResult> Index()
        {
            var chambres = await _context.Chambres
                .Include(c => c.Hotel)
                .OrderBy(c => c.Hotel.Nom)
                .ThenBy(c => c.NumeroOuCode)
                .ToListAsync();
            return View(chambres);
        }

        // CREATE GET
        public IActionResult Create()
        {
            RemplirListeHotels();
            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Chambre chambre)
        {
            ModelState.Remove("Hotel");

            if (ModelState.IsValid)
            {
                _context.Add(chambre);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Chambre {chambre.NumeroOuCode} créée avec succès !";
                return RedirectToAction(nameof(Index));
            }

            RemplirListeHotels(chambre.HotelId);
            return View(chambre);
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var chambre = await _context.Chambres.FindAsync(id);
            if (chambre == null) return NotFound();

            RemplirListeHotels(chambre.HotelId);
            return View(chambre);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Chambre chambre)
        {
            if (id != chambre.Id) return NotFound();

            ModelState.Remove("Hotel");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chambre);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Chambre {chambre.NumeroOuCode} modifiée avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChambreExists(chambre.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            RemplirListeHotels(chambre.HotelId);
            return View(chambre);
        }

        // DELETE GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var chambre = await _context.Chambres
                .Include(c => c.Hotel)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (chambre == null) return NotFound();

            return View(chambre);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chambre = await _context.Chambres.FindAsync(id);
            if (chambre != null)
            {
                // Vérifier s'il y a des réservations liées
                var nbReservations = await _context.ReservationLignes
                    .Where(rl => rl.ChambreId == id)
                    .CountAsync();

                if (nbReservations > 0)
                {
                    TempData["Error"] = $"Impossible de supprimer la chambre {chambre.NumeroOuCode} : elle est liée à {nbReservations} réservation(s). Désactivez la chambre au lieu de la supprimer.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Chambres.Remove(chambre);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Chambre {chambre.NumeroOuCode} supprimée !";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ChambreExists(int id)
        {
            return _context.Chambres.Any(e => e.Id == id);
        }

        private void RemplirListeHotels(int? hotelId = null)
        {
            var hotelsActifs = _context.Hotels
                .Where(h => h.EstActif)
                .OrderBy(h => h.Nom)
                .ToList();

            ViewBag.Hotels = new SelectList(hotelsActifs, "Id", "Nom", hotelId);
        }
    }
}
