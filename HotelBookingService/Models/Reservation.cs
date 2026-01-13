using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // Döngüsel hatayı önlemek için gerekebilir

namespace HotelBookingService.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        public int RoomId { get; set; }

        // --- EKLENECEK KISIM (Navigation Property) ---
        // Bu özellik sayesinde .Include(r => r.Room) çalışır.
        [JsonIgnore] // API dönüşünde sonsuz döngüye girmesin diye
        public virtual Room? Room { get; set; }
        // ---------------------------------------------

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