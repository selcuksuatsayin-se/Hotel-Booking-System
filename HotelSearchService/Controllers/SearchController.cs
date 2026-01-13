using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelSearchService.Data;
using HotelSearchService.Dtos;

namespace HotelSearchService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SearchController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/v1/Search?city=Bodrum&startDate=2026-06-01&endDate=2026-06-05&guests=2
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SearchResultDto>>> Search(
            [FromQuery] string? city, // 'city' parametresi artık genel arama terimi olarak kullanılıyor
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] int guests)
        {
            // 1. Tarih Validasyonu
            int totalNights = endDate.DayNumber - startDate.DayNumber;
            if (totalNights <= 0) return BadRequest("Geçersiz tarih aralığı. Çıkış tarihi girişten sonra olmalıdır.");

            // 2. Temel Sorgu (Base Query)
            // Availability tablosundan başlayarak yukarı (Room -> Hotel) çıkıyoruz.
            var query = _context.Availabilities
                .Include(a => a.Room).ThenInclude(r => r.Hotel)
                .Where(a => a.Date >= startDate
                            && a.Date < endDate
                            && a.IsVacant == true
                            && a.AvailableStock > 0
                            && a.Room.Capacity >= guests);

            // 3. Gelişmiş Arama Filtresi (Şehir, İlçe, Ülke veya Otel Adı)
            if (!string.IsNullOrEmpty(city))
            {
                string searchTerm = city.ToLower().Trim();

                query = query.Where(a =>
                    a.Room.Hotel.City.ToLower().Contains(searchTerm) ||      // Örn: Muğla
                    a.Room.Hotel.District.ToLower().Contains(searchTerm) ||  // Örn: Bodrum
                    a.Room.Hotel.Country.ToLower().Contains(searchTerm) ||   // Örn: Turkey
                    a.Room.Hotel.Name.ToLower().Contains(searchTerm)         // Örn: Swiss
                );
            }

            // Veritabanından veriyi çekiyoruz (Execution)
            var availableEntries = await query.ToListAsync();

            // 4. Müsaitlik Kontrolü (Grup Mantığı)
            // Bir odanın listelenmesi için seçilen tarih aralığındaki TÜM günlerde boş olması gerekir.
            var validRoomIds = availableEntries
                .GroupBy(a => a.RoomId)
                .Where(g => g.Count() == totalNights) // Gün sayısı kadar kayıt var mı?
                .Select(g => g.Key)
                .ToList();

            if (!validRoomIds.Any()) return Ok(new List<SearchResultDto>());

            // 5. İndirim Mantığı (User Login Kontrolü)
            bool isUserLoggedIn = User.Identity?.IsAuthenticated ?? false;
            decimal discountMultiplier = isUserLoggedIn ? 0.90m : 1.0m;

            // 6. Yanıtı Oluştur (Mapping)
            // DTO'ya İlçe (District) ve Ülke (Country) bilgilerini de ekliyoruz.
            var results = availableEntries
                .Where(a => validRoomIds.Contains(a.RoomId))
                .GroupBy(a => a.Room.Hotel)
                .Select(g => new SearchResultDto
                {
                    HotelId = g.Key.Id,
                    HotelName = g.Key.Name,
                    City = g.Key.City,
                    District = g.Key.District, // Yeni Alan
                    Country = g.Key.Country,   // Yeni Alan
                    Latitude = g.Key.Latitude,
                    Longitude = g.Key.Longitude,
                    Rating = g.Key.Rating,

                    // Fiyat Hesaplama: En düşük gecelik fiyatı bulup indirim uyguluyoruz
                    PricePerNight = g.Min(a => a.NightlyPrice > 0 ? a.NightlyPrice : a.Room.BasePrice) * discountMultiplier,

                    AvailableRooms = g.Select(a => a.Room).Distinct().Select(r => new RoomReadDto
                    {
                        Id = r.Id,
                        Title = r.Title,
                        Capacity = r.Capacity,
                        BasePrice = r.BasePrice * discountMultiplier,
                        IsAvailable = true
                    }).ToList()
                })
                .ToList();

            return Ok(results);
        }
    }
}