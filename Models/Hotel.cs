namespace HotelBookingMVC.Models
{
    public class Hotel
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;
        public string Pays { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImagePrincipaleUrl { get; set; } = string.Empty;
        public int NombreEtoiles { get; set; } = 3;
        public bool EstActif { get; set; } = true;

        // Navigation properties
        public ICollection<Chambre> Chambres { get; set; } = new List<Chambre>();
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
