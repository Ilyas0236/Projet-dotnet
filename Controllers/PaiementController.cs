using HotelBookingMVC.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelBookingMVC.Controllers
{
    [Authorize(Roles = "Client,Admin")]
    public class PaiementController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PaiementController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Paiement/Checkout/5
        public async Task<IActionResult> Checkout(int reservationId)
        {
            var clientId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reservation = await _context.Reservations
                .Include(r => r.Hotel)
                .Include(r => r.Client)
                .Include(r => r.Lignes).ThenInclude(l => l.Chambre)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.ClientId == clientId);

            if (reservation == null || reservation.Statut != "EnAttente")
            {
                TempData["Error"] = "Réservation introuvable ou déjà payée.";
                return RedirectToAction("MesReservations", "Reservation");
            }

            return View(reservation);
        }

        // POST: /Paiement/Confirmer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirmer(int reservationId)
        {
            var clientId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reservation = await _context.Reservations
                .Include(r => r.Hotel)
                .Include(r => r.Client)
                .Include(r => r.Lignes).ThenInclude(l => l.Chambre)
                .FirstOrDefaultAsync(r => r.Id == reservationId && r.ClientId == clientId);

            if (reservation == null || reservation.Statut != "EnAttente")
            {
                TempData["Error"] = "Réservation introuvable ou déjà payée.";
                return RedirectToAction("MesReservations", "Reservation");
            }

            // SIMULATION PAIEMENT (toujours succès)
            var paiement = new Paiement
            {
                ReservationId = reservationId,
                DatePaiement = DateTime.UtcNow,
                Montant = reservation.Total,
                Mode = "Simule",
                Statut = "Succes",
                ReferenceTransaction = $"SIM-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
            };

            _context.Paiements.Add(paiement);
            reservation.Statut = "Payee";
            await _context.SaveChangesAsync();

            // Générer la facture PDF
            var cheminPdf = GenererFacturePdf(reservation, paiement);

            var facture = new Facture
            {
                ReservationId = reservationId,
                NumeroFacture = $"FAC-{DateTime.UtcNow:yyyyMMdd}-{reservationId:D5}",
                DateEmission = DateTime.UtcNow,
                CheminPdf = cheminPdf
            };

            _context.Factures.Add(facture);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Paiement simulé avec succès ! Facture générée.";
            return RedirectToAction("Success", new { factureId = facture.Id });
        }

        // GET: /Paiement/Success/5
        public async Task<IActionResult> Success(int factureId)
        {
            var clientId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var facture = await _context.Factures
                .Include(f => f.Reservation).ThenInclude(r => r.Hotel)
                .Include(f => f.Reservation).ThenInclude(r => r.Client)
                .FirstOrDefaultAsync(f => f.Id == factureId
                    && (f.Reservation.ClientId == clientId || User.IsInRole("Admin")));

            if (facture == null)
            {
                TempData["Error"] = "Facture introuvable.";
                return RedirectToAction("MesReservations", "Reservation");
            }

            return View(facture);
        }

        // GET: /Paiement/TelechargerFacture/5
        public async Task<IActionResult> TelechargerFacture(int factureId)
        {
            var clientId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var facture = await _context.Factures
                .Include(f => f.Reservation)
                .ThenInclude(r => r.Client)
                .FirstOrDefaultAsync(f => f.Id == factureId
                    && (f.Reservation.ClientId == clientId || User.IsInRole("Admin")));

            if (facture == null)
            {
                TempData["Error"] = "Facture introuvable.";
                return RedirectToAction("MesReservations", "Reservation");
            }

            var cheminComplet = Path.Combine(_env.WebRootPath, facture.CheminPdf);
            if (!System.IO.File.Exists(cheminComplet))
            {
                TempData["Error"] = "Fichier PDF introuvable.";
                return RedirectToAction("MesReservations", "Reservation");
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(cheminComplet);
            return File(bytes, "application/pdf", $"Facture-{facture.NumeroFacture}.pdf");
        }

        // ========== PRIVÉ : GÉNÉRATION PDF ==========
        private string GenererFacturePdf(Reservation reservation, Paiement paiement)
        {
            var dossierFactures = Path.Combine(_env.WebRootPath, "factures");
            if (!Directory.Exists(dossierFactures))
                Directory.CreateDirectory(dossierFactures);

            var nomFichier = $"facture-{reservation.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            var cheminComplet = Path.Combine(dossierFactures, nomFichier);

            using (var stream = new FileStream(cheminComplet, FileMode.Create))
            {
                var document = new Document(PageSize.A4, 50, 50, 25, 25);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                // Titre
                var titre = new Paragraph("FACTURE DE RÉSERVATION",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20));
                titre.Alignment = Element.ALIGN_CENTER;
                titre.SpacingAfter = 20;
                document.Add(titre);

                // Infos facture
                var numeroFacture = $"FAC-{DateTime.UtcNow:yyyyMMdd}-{reservation.Id:D5}";
                document.Add(new Paragraph($"Numéro : {numeroFacture}",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
                document.Add(new Paragraph($"Date : {DateTime.UtcNow:dd/MM/yyyy HH:mm}"));
                document.Add(new Paragraph($"Client : {reservation.Client.Prenom} {reservation.Client.Nom}"));
                document.Add(new Paragraph($"Email : {reservation.Client.Email}"));
                document.Add(new Paragraph("\n"));

                // Détails réservation
                document.Add(new Paragraph("DÉTAILS DE LA RÉSERVATION",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14)));
                document.Add(new Paragraph($"Hôtel : {reservation.Hotel.Nom}"));
                document.Add(new Paragraph($"Adresse : {reservation.Hotel.Adresse}, {reservation.Hotel.Ville}, {reservation.Hotel.Pays}"));
                document.Add(new Paragraph($"Période : Du {reservation.DateDebut:dd/MM/yyyy} au {reservation.DateFin:dd/MM/yyyy}"));
                document.Add(new Paragraph($"Nombre de nuits : {(reservation.DateFin - reservation.DateDebut).Days}"));
                document.Add(new Paragraph("\n"));

                // Tableau des lignes
                var table = new PdfPTable(4) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 3, 2, 1, 2 });

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var headerBg = new BaseColor(230, 230, 230); // gris clair

                table.AddCell(new PdfPCell(new Phrase("Chambre", headerFont)) { BackgroundColor = headerBg, Padding = 5 });
                table.AddCell(new PdfPCell(new Phrase("Prix/nuit", headerFont)) { BackgroundColor = headerBg, Padding = 5 });
                table.AddCell(new PdfPCell(new Phrase("Nuits", headerFont)) { BackgroundColor = headerBg, Padding = 5 });
                table.AddCell(new PdfPCell(new Phrase("Total", headerFont)) { BackgroundColor = headerBg, Padding = 5 });

                foreach (var ligne in reservation.Lignes)
                {
                    table.AddCell(new PdfPCell(new Phrase($"{ligne.Chambre.Type} ({ligne.Chambre.NumeroOuCode})")) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase($"{ligne.PrixParNuit:C}")) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(ligne.NbNuits.ToString())) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase($"{ligne.SousTotal:C}")) { Padding = 5 });
                }

                document.Add(table);
                document.Add(new Paragraph("\n"));

                // Total
                var total = new Paragraph($"TOTAL : {reservation.Total:C}",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18));
                total.Alignment = Element.ALIGN_RIGHT;
                document.Add(total);
                document.Add(new Paragraph("\n"));

                // Paiement
                document.Add(new Paragraph("INFORMATIONS DE PAIEMENT",
                    FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14)));
                document.Add(new Paragraph($"Mode de paiement : {paiement.Mode}"));
                document.Add(new Paragraph($"Référence : {paiement.ReferenceTransaction}"));
                document.Add(new Paragraph($"Statut : {paiement.Statut}"));
                document.Add(new Paragraph($"Date : {paiement.DatePaiement:dd/MM/yyyy HH:mm}"));
                document.Add(new Paragraph("\n"));

                var footer = new Paragraph("Merci pour votre réservation !",
                    FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 10));
                footer.Alignment = Element.ALIGN_CENTER;
                document.Add(footer);

                document.Close();
            }

            return $"factures/{nomFichier}";
        }
    }
}
