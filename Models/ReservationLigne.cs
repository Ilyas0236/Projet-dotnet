namespace HotelBookingMVC.Models
{
    public class ReservationLigne
    {
        public int Id { get; set; }
        public int ReservationId { get; set; }
        public int ChambreId { get; set; }
        public decimal PrixParNuit { get; set; }
        public int NbNuits { get; set; }
        public decimal SousTotal { get; set; }

        // Navigation properties
        public Reservation Reservation { get; set; } = null!;
        public Chambre Chambre { get; set; } = null!;
    }
}
