namespace HotelBookingMVC.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int HotelId { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public string Statut { get; set; } = "EnAttente"; // EnAttente, Payee, Annulee
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public decimal Total { get; set; }

        // Navigation properties
        public Utilisateur Client { get; set; } = null!;
        public Hotel Hotel { get; set; } = null!;
        public ICollection<ReservationLigne> Lignes { get; set; } = new List<ReservationLigne>();
    }
}
