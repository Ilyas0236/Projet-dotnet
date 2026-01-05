namespace HotelBookingMVC.Models
{
    public class Paiement
    {
        public int Id { get; set; }
        public int ReservationId { get; set; }
        public DateTime DatePaiement { get; set; }
        public decimal Montant { get; set; }
        public string Mode { get; set; } = "Simule"; // Simule, Carte, PayPal
        public string Statut { get; set; } = "Succes"; // Succes, Echec
        public string ReferenceTransaction { get; set; } = string.Empty;

        // Navigation properties
        public Reservation Reservation { get; set; } = null!;
    }
}
