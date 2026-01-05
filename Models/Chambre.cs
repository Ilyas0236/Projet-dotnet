using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HotelBookingMVC.Models
{
    public class Chambre
    {
        public int Id { get; set; }
        public int HotelId { get; set; }
        public string NumeroOuCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Capacite { get; set; } = 2;
        public decimal PrixParNuit { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool EstActive { get; set; } = true;

        // Navigation : exclue du binding/validation
        [BindNever]
        public Hotel Hotel { get; set; } = null!;

        [BindNever]
        public ICollection<ReservationLigne> ReservationLignes { get; set; } = new List<ReservationLigne>();
    }
}
