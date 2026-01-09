using System.ComponentModel.DataAnnotations;

namespace HotelAdminService.Dtos
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
        public int Id { get; set; } // <--- We send this back to the user
        public string Name { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Description { get; set; }
        public double Rating { get; set; }
    }
}