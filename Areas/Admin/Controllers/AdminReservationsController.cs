using HotelBookingMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminReservationsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminReservationsController(AppDbContext context)
        {
            _context = context;
        }

        // LISTE
        public async Task<IActionResult> Index(string statut = "")
        {
            var reservations = _context.Reservations
                .Include(r => r.Client)
                .Include(r => r.Hotel)
                .Include(r => r.Lignes)
                .AsQueryable();

            // Filtre par statut si fourni
            if (!string.IsNullOrEmpty(statut))
            {
                reservations = reservations.Where(r => r.Statut == statut);
            }

            var liste = await reservations
                .OrderByDescending(r => r.DateCreation)
                .ToListAsync();

            ViewBag.StatutFiltre = statut;
            return View(liste);
        }

        // DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Client)
                .Include(r => r.Hotel)
                .Include(r => r.Lignes)
                    .ThenInclude(l => l.Chambre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Client)
                .Include(r => r.Hotel)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound();

            RemplirListesStatut(reservation.Statut);
            return View(reservation);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Reservation reservation)
        {
            if (id != reservation.Id) return NotFound();

            ModelState.Remove("Client");
            ModelState.Remove("Hotel");
            ModelState.Remove("Lignes");

            if (ModelState.IsValid)
            {
                try
                {
                    var reservationDb = await _context.Reservations.FindAsync(id);
                    if (reservationDb == null) return NotFound();

                    reservationDb.Statut = reservation.Statut;
                    reservationDb.DateDebut = reservation.DateDebut;
                    reservationDb.DateFin = reservation.DateFin;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Réservation #{reservation.Id} modifiée avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            RemplirListesStatut(reservation.Statut);
            return View(reservation);
        }

        // DELETE GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Client)
                .Include(r => r.Hotel)
                .Include(r => r.Lignes)
                    .ThenInclude(l => l.Chambre)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null) return NotFound();

            return View(reservation);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Lignes)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation != null)
            {
                // Vérifier s'il y a des factures liées
                var nbFactures = await _context.Factures
                    .Where(f => f.ReservationId == id)
                    .CountAsync();

                if (nbFactures > 0)
                {
                    TempData["Error"] = $"Impossible de supprimer la réservation #{id} : elle a {nbFactures} facture(s) associée(s). Changez le statut à 'Annulée' au lieu de supprimer.";
                    return RedirectToAction(nameof(Index));
                }

                // Vérifier s'il y a des paiements liés
                var nbPaiements = await _context.Paiements
                    .Where(p => p.ReservationId == id)
                    .CountAsync();

                if (nbPaiements > 0)
                {
                    TempData["Error"] = $"Impossible de supprimer la réservation #{id} : elle a {nbPaiements} paiement(s) associé(s). Changez le statut à 'Annulée' au lieu de supprimer.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Reservations.Remove(reservation);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Réservation #{id} supprimée !";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }

        private void RemplirListesStatut(string? statutActuel = null)
        {
            var statuts = new List<SelectListItem>
            {
                new SelectListItem { Value = "EnAttente", Text = "En attente" },
                new SelectListItem { Value = "Confirmee", Text = "Confirmée" },
                new SelectListItem { Value = "Payee", Text = "Payée" },
                new SelectListItem { Value = "Terminee", Text = "Terminée" },
                new SelectListItem { Value = "Annulee", Text = "Annulée" }
            };

            ViewBag.Statuts = new SelectList(statuts, "Value", "Text", statutActuel);
        }
    }
}
