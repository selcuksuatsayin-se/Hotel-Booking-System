using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HotelAdminService.Models
{
    public class Room
    {
        public int Id { get; set; }

        public int HotelId { get; set; }
        [JsonIgnore]
        public Hotel? Hotel { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty; // e.g., "Standard Room with Sea View"

        public decimal BasePrice { get; set; } // Price before dynamic prediction

        public int Capacity { get; set; } // Max people (e.g., 2 guests)

        // Total physical rooms of this type in the hotel
        public int TotalCount { get; set; } 

        [JsonIgnore]
        public List<Availability> Availabilities { get; set; } = new List<Availability>();
    }
}