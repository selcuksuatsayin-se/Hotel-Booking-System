namespace HotelSearchService.Dtos
{
    public class SearchRequestDto
    {
        public string City { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int NumberOfGuests { get; set; }
    }

    public class SearchResultDto
    {
        public int HotelId { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        // YENİ EKLENEN ALANLAR
        public string District { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Rating { get; set; }
        public decimal PricePerNight { get; set; }
        public List<RoomReadDto> AvailableRooms { get; set; } = new List<RoomReadDto>();
    }
}