namespace HotelBookingMVC.Models
{
    public class Facture
    {
        public int Id { get; set; }
        public int ReservationId { get; set; }
        public string NumeroFacture { get; set; } = string.Empty;
        public DateTime DateEmission { get; set; }
        public string CheminPdf { get; set; } = string.Empty;

        // Navigation properties
        public Reservation Reservation { get; set; } = null!;
    }
}
