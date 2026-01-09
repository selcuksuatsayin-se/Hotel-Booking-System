using System.ComponentModel.DataAnnotations;

namespace HotelAdminService.Dtos
{
    public class RoomCreateDto
    {
        [Required]
        public int HotelId { get; set; } // Link to the hotel

        [Required]
        public string Title { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal BasePrice { get; set; }

        [Range(1, 20)]
        public int Capacity { get; set; }

        public int TotalCount { get; set; } // Total physical rooms
    }

    public class RoomReadDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal BasePrice { get; set; }
        public int Capacity { get; set; }
        public bool IsAvailable { get; set; } // Calculated field for search results
    }
}