using System.ComponentModel.DataAnnotations;

namespace HotelSearchService.Dtos
{
    // 1. DTO for CREATING a hotel (Input)
    // Notice: NO "Id" here. The user shouldn't worry about it.
    public class HotelCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        // Required for Map
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string Description { get; set; } = string.Empty;
        public double Rating { get; set; }
    }

    // 2. DTO for READING a hotel (Output)
    // This DOES have the Id, because the UI needs it to make links.
    public class HotelReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }

        // --- EKLENEN ALANLAR ---
        public string District { get; set; } // İlçe
        public string Country { get; set; }  // Ülke
        public string Address { get; set; }  // Keep this!
        public double Latitude { get; set; } // Harita Enlem
        public double Longitude { get; set; } // Harita Boylam
        public double Rating { get; set; }
        // -----------------------

        public string Description { get; set; }
        public List<RoomReadDto> Rooms { get; set; } = new List<RoomReadDto>();
    }
}