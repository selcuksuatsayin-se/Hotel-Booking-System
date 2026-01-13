namespace HotelAdminService.Dtos
{
    public class AvailabilityBulkUpdateDto
    {
        public int HotelId { get; set; }
        public string RoomTitle { get; set; } // "Standard Room" veya "Deluxe Room"
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public bool IsVacant { get; set; } // True = Dolu(Vacant), False = Boş(Occupied/Closed)
    }
}