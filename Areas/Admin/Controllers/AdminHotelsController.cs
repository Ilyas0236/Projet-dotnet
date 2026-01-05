using HotelBookingMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminHotelsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminHotelsController(AppDbContext context)
        {
            _context = context;
        }

        // Liste des hôtels
        public async Task<IActionResult> Index()
        {
            var hotels = await _context.Hotels.OrderBy(h => h.Nom).ToListAsync();
            return View(hotels);
        }

        // Formulaire de création
        public IActionResult Create()
        {
            return View();
        }

        // Création POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nom,Adresse,Ville,Pays,Description,ImagePrincipaleUrl,NombreEtoiles")] Hotel hotel)
        {
            if (ModelState.IsValid)
            {
                hotel.EstActif = true;
                _context.Add(hotel);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Hôtel '{hotel.Nom}' créé avec succès !";
                return RedirectToAction(nameof(Index));
            }
            return View(hotel);
        }

        // Formulaire de modification
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null) return NotFound();

            return View(hotel);
        }

        // Modification POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Adresse,Ville,Pays,Description,ImagePrincipaleUrl,NombreEtoiles,EstActif")] Hotel hotel)
        {
            if (id != hotel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hotel);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Hôtel '{hotel.Nom}' modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HotelExists(hotel.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(hotel);
        }

        // Page de confirmation de suppression
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var hotel = await _context.Hotels
                .Include(h => h.Chambres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (hotel == null) return NotFound();

            return View(hotel);
        }

        // Suppression confirmée
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel != null)
            {
                // Vérifier s'il y a des chambres
                var nbChambres = await _context.Chambres
                    .Where(c => c.HotelId == id)
                    .CountAsync();

                if (nbChambres > 0)
                {
                    TempData["Error"] = $"Impossible de supprimer l'hôtel '{hotel.Nom}' : il contient {nbChambres} chambre(s). Supprimez d'abord les chambres ou désactivez l'hôtel.";
                    return RedirectToAction(nameof(Index));
                }

                // Vérifier s'il y a des réservations
                var nbReservations = await _context.Reservations
                    .Where(r => r.HotelId == id)
                    .CountAsync();

                if (nbReservations > 0)
                {
                    TempData["Error"] = $"Impossible de supprimer l'hôtel '{hotel.Nom}' : il a {nbReservations} réservation(s). Désactivez l'hôtel au lieu de le supprimer.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Hotels.Remove(hotel);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Hôtel '{hotel.Nom}' supprimé !";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool HotelExists(int id)
        {
            return _context.Hotels.Any(e => e.Id == id);
        }
    }
}
