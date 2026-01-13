using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelAdminService.Models
{
    public class Hotel
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty; // e.g., "Bodrum"
        public string District { get; set; } = string.Empty; // İlçe (Örn: Bodrum)
        public string Country { get; set; } = "Turkey";      // Ülke

        public string Address { get; set; } = string.Empty;

        // Required for "Haritada Göster" (Show on Map) requirement
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Extra details for the UI (Mockup shows rating, description)
        public double Rating { get; set; } // e.g., 9.6
        public string Description { get; set; } = string.Empty;

        // Navigation Property
        [JsonIgnore] // Prevents infinite loops when fetching API data
        public List<Room> Rooms { get; set; } = new List<Room>();
    }
}