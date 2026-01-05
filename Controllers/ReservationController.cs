using HotelBookingMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelBookingMVC.Controllers
{
    [Authorize(Roles = "Client,Admin")]
    public class ReservationController : Controller
    {
        private readonly AppDbContext _context;

        public ReservationController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Reservation/Create?hotelId=1&chambreId=2
        [HttpGet]
        public async Task<IActionResult> Create(int hotelId, int chambreId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            var chambre = await _context.Chambres.FindAsync(chambreId);

            if (hotel == null || chambre == null)
            {
                TempData["Error"] = "Hôtel ou chambre introuvable";
                return RedirectToAction("Index", "Hotel");
            }

            ViewBag.Hotel = hotel;
            ViewBag.Chambre = chambre;
            return View();
        }

        // POST: /Reservation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int hotelId, int chambreId, DateTime dateDebut, DateTime dateFin)
        {
            // Vérifier que les dates sont valides
            if (dateDebut < DateTime.Today)
            {
                TempData["Error"] = "La date de début ne peut pas être dans le passé";
                return RedirectToAction("Create", new { hotelId, chambreId });
            }

            if (dateFin <= dateDebut)
            {
                TempData["Error"] = "La date de fin doit être après la date de début";
                return RedirectToAction("Create", new { hotelId, chambreId });
            }

            // Vérifier la disponibilité de la chambre
            var estDisponible = !await _context.ReservationLignes
                .Include(rl => rl.Reservation)
                .AnyAsync(rl => rl.ChambreId == chambreId
                    && rl.Reservation.Statut != "Annulee"
                    && rl.Reservation.DateDebut < dateFin
                    && rl.Reservation.DateFin > dateDebut);

            if (!estDisponible)
            {
                TempData["Error"] = "Cette chambre n'est pas disponible pour ces dates";
                return RedirectToAction("Create", new { hotelId, chambreId });
            }

            // Récupérer la chambre pour le prix
            var chambre = await _context.Chambres.FindAsync(chambreId);
            var nbNuits = (dateFin - dateDebut).Days;
            var sousTotal = chambre!.PrixParNuit * nbNuits;

            // Récupérer l'ID du client connecté
            var clientId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Créer la réservation
            var reservation = new Reservation
            {
                ClientId = clientId,
                HotelId = hotelId,
                DateDebut = dateDebut,
                DateFin = dateFin,
                Statut = "EnAttente",
                Total = sousTotal,
                DateCreation = DateTime.UtcNow
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Créer la ligne de réservation
            var ligne = new ReservationLigne
            {
                ReservationId = reservation.Id,
                ChambreId = chambreId,
                PrixParNuit = chambre.PrixParNuit,
                NbNuits = nbNuits,
                SousTotal = sousTotal
            };

            _context.ReservationLignes.Add(ligne);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Réservation créée avec succès ! Procédez au paiement.";
            return RedirectToAction("MesReservations");
        }

        // GET: /Reservation/MesReservations
        public async Task<IActionResult> MesReservations()
        {
            var clientId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reservations = await _context.Reservations
                .Include(r => r.Hotel)
                .Include(r => r.Lignes).ThenInclude(l => l.Chambre)
                .Where(r => r.ClientId == clientId)
                .OrderByDescending(r => r.DateCreation)
                .ToListAsync();

            return View(reservations);
        }

        // GET: /Reservation/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var clientId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reservation = await _context.Reservations
                .Include(r => r.Hotel)
                .Include(r => r.Client)
                .Include(r => r.Lignes).ThenInclude(l => l.Chambre)
                .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == clientId);

            if (reservation == null)
            {
                TempData["Error"] = "Réservation introuvable";
                return RedirectToAction("MesReservations");
            }

            return View(reservation);
        }

        // POST: /Reservation/Annuler/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Annuler(int id)
        {
            var clientId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == clientId);

            if (reservation == null)
            {
                TempData["Error"] = "Réservation introuvable";
                return RedirectToAction("MesReservations");
            }

            if (reservation.Statut == "Payee")
            {
                TempData["Error"] = "Impossible d'annuler une réservation déjà payée. Contactez le service client.";
                return RedirectToAction("MesReservations");
            }

            reservation.Statut = "Annulee";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Réservation annulée avec succès";
            return RedirectToAction("MesReservations");
        }
    }
}
