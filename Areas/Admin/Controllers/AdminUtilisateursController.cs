using HotelBookingMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingMVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminUtilisateursController : Controller
    {
        private readonly AppDbContext _context;

        public AdminUtilisateursController(AppDbContext context)
        {
            _context = context;
        }

        // LISTE
        public async Task<IActionResult> Index(string role = "")
        {
            var utilisateurs = _context.Utilisateurs.AsQueryable();

            // Filtre par rôle si fourni
            if (!string.IsNullOrEmpty(role))
            {
                utilisateurs = utilisateurs.Where(u => u.Role == role);
            }

            var liste = await utilisateurs
                .OrderByDescending(u => u.DateCreation)
                .ToListAsync();

            ViewBag.RoleFiltre = role;
            return View(liste);
        }

        // DETAILS
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(m => m.Id == id);

            if (utilisateur == null) return NotFound();

            // Charger l'historique des réservations
            var reservations = await _context.Reservations
                .Include(r => r.Hotel)
                .Where(r => r.ClientId == id)
                .OrderByDescending(r => r.DateCreation)
                .ToListAsync();

            ViewBag.Reservations = reservations;
            return View(utilisateur);
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null) return NotFound();

            RemplirListesRoles(utilisateur.Role);
            return View(utilisateur);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Utilisateur utilisateur)
        {
            if (id != utilisateur.Id) return NotFound();

            ModelState.Remove("MotDePasseHash");

            if (ModelState.IsValid)
            {
                try
                {
                    var userDb = await _context.Utilisateurs.FindAsync(id);
                    if (userDb == null) return NotFound();

                    userDb.Nom = utilisateur.Nom;
                    userDb.Prenom = utilisateur.Prenom;
                    userDb.Email = utilisateur.Email;
                    userDb.Role = utilisateur.Role;
                    userDb.EstActif = utilisateur.EstActif;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Utilisateur {utilisateur.Prenom} {utilisateur.Nom} modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UtilisateurExists(utilisateur.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            RemplirListesRoles(utilisateur.Role);
            return View(utilisateur);
        }

        // DELETE GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(m => m.Id == id);

            if (utilisateur == null) return NotFound();

            var nbReservations = await _context.Reservations
                .Where(r => r.ClientId == id)
                .CountAsync();

            ViewBag.NbReservations = nbReservations;
            return View(utilisateur);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur != null)
            {
                // Vérifier s'il a des réservations
                var nbReservations = await _context.Reservations
                    .Where(r => r.ClientId == id)
                    .CountAsync();

                if (nbReservations > 0)
                {
                    TempData["Error"] = $"Impossible de supprimer {utilisateur.Prenom} {utilisateur.Nom} : cet utilisateur a {nbReservations} réservation(s). Désactivez le compte au lieu de le supprimer.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Utilisateurs.Remove(utilisateur);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Utilisateur {utilisateur.Prenom} {utilisateur.Nom} supprimé !";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool UtilisateurExists(int id)
        {
            return _context.Utilisateurs.Any(e => e.Id == id);
        }

        private void RemplirListesRoles(string? roleActuel = null)
        {
            var roles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Client", Text = "Client" },
                new SelectListItem { Value = "Admin", Text = "Administrateur" }
            };

            ViewBag.Roles = new SelectList(roles, "Value", "Text", roleActuel);
        }
    }
}
