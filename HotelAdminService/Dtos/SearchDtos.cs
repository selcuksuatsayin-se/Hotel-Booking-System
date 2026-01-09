namespace HotelAdminService.Dtos
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
        public string HotelName { get; set; }
        public string City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Rating { get; set; }

        // Return the cheapest available room for this search
        public decimal PricePerNight { get; set; }
        public List<RoomReadDto> AvailableRooms { get; set; } = new List<RoomReadDto>();
    }
}