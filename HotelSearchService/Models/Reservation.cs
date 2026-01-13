using System.ComponentModel.DataAnnotations;

namespace HotelSearchService.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        // ID from the external IAM (AWS Cognito / Azure AD)
        public string UserId { get; set; } = string.Empty;

        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Status: "Confirmed", "Cancelled"
        public string Status { get; set; } = "Confirmed";
    }
}