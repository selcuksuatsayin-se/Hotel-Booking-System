using System.ComponentModel.DataAnnotations;

namespace HotelAdminService.Dtos
{
    public class ReservationCreateDto
    {
        [Required]
        public int RoomId { get; set; }

        [Required]
        public DateOnly CheckInDate { get; set; }

        [Required]
        public DateOnly CheckOutDate { get; set; }

        // We usually get UserId from the Token, but for simplicity/testing:
        public string? GuestEmail { get; set; }
    }

    public class ReservationReadDto
    {
        public int Id { get; set; }
        public string HotelName { get; set; }
        public string RoomType { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }
}