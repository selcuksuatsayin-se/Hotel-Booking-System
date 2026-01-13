using System.ComponentModel.DataAnnotations;

namespace HotelSearchService.Models
{
    public class Availability
    {
        public int Id { get; set; }

        public int RoomId { get; set; }
        public Room? Room { get; set; }

        [Required]
        public DateOnly Date { get; set; } // Stores just the date (yyyy-MM-dd)

        // How many rooms are left for this specific night?
        public int AvailableStock { get; set; }

        // Admin switch: "Dolu (Vacant)" vs "Bos (Occupied)" requirement
        public bool IsVacant { get; set; } = true;

        // Optional: Store the predicted price for this specific date here
        public decimal NightlyPrice { get; set; }
    }
}