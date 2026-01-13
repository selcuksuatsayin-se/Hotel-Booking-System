using System.Text.Json.Serialization;

namespace HotelBookingService.Dtos
{
    public class PredictionRequestDto
    {
        // "August", "September", etc.
        [JsonPropertyName("month")]
        public string Month { get; set; } = "August";

        // "A" (Standard), "D" (Deluxe), etc.
        [JsonPropertyName("roomType")]
        public string RoomType { get; set; } = "A";

        // How many days in advance is the booking?
        [JsonPropertyName("leadTime")]
        public int LeadTime { get; set; }

        [JsonPropertyName("guests")]
        public int Guests { get; set; }
    }

    public class PredictionResponseDto
    {
        [JsonPropertyName("suggested_price")]
        public decimal SuggestedPrice { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "EUR";

        [JsonPropertyName("details")]
        public string Details { get; set; } = string.Empty;
    }
}