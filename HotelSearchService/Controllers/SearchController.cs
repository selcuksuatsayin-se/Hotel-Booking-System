//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using HotelSearchService.Data;
//using HotelSearchService.Dtos;

//namespace HotelSearchService.Controllers
//{
//    [Route("api/v1/[controller]")]
//    [ApiController]
//    public class SearchController : ControllerBase
//    {
//        private readonly AppDbContext _context;

//        public SearchController(AppDbContext context)
//        {
//            _context = context;
//        }

//        // GET: api/v1/Search?city=Bodrum&startDate=2026-06-01&endDate=2026-06-05&guests=2
//        [HttpGet]
//        public async Task<ActionResult<IEnumerable<SearchResultDto>>> Search(
//            [FromQuery] string? city, // 'city' parametresi artık genel arama terimi olarak kullanılıyor
//            [FromQuery] DateOnly startDate,
//            [FromQuery] DateOnly endDate,
//            [FromQuery] int guests)
//        {
//            // 1. Tarih Validasyonu
//            int totalNights = endDate.DayNumber - startDate.DayNumber;
//            if (totalNights <= 0) return BadRequest("Geçersiz tarih aralığı. Çıkış tarihi girişten sonra olmalıdır.");

//            // 2. Temel Sorgu (Base Query)
//            // Availability tablosundan başlayarak yukarı (Room -> Hotel) çıkıyoruz.
//            var query = _context.Availabilities
//                .Include(a => a.Room).ThenInclude(r => r.Hotel)
//                .Where(a => a.Date >= startDate
//                            && a.Date < endDate
//                            && a.IsVacant == true
//                            && a.AvailableStock > 0
//                            && a.Room.Capacity >= guests);

//            // 3. Gelişmiş Arama Filtresi (Şehir, İlçe, Ülke veya Otel Adı)
//            if (!string.IsNullOrEmpty(city))
//            {
//                string searchTerm = city.ToLower().Trim();

//                query = query.Where(a =>
//                    a.Room.Hotel.City.ToLower().Contains(searchTerm) ||      // Örn: Muğla
//                    a.Room.Hotel.District.ToLower().Contains(searchTerm) ||  // Örn: Bodrum
//                    a.Room.Hotel.Country.ToLower().Contains(searchTerm) ||   // Örn: Turkey
//                    a.Room.Hotel.Name.ToLower().Contains(searchTerm)         // Örn: Swiss
//                );
//            }

//            // Veritabanından veriyi çekiyoruz (Execution)
//            var availableEntries = await query.ToListAsync();

//            // 4. Müsaitlik Kontrolü (Grup Mantığı)
//            // Bir odanın listelenmesi için seçilen tarih aralığındaki TÜM günlerde boş olması gerekir.
//            var validRoomIds = availableEntries
//                .GroupBy(a => a.RoomId)
//                .Where(g => g.Count() == totalNights) // Gün sayısı kadar kayıt var mı?
//                .Select(g => g.Key)
//                .ToList();

//            if (!validRoomIds.Any()) return Ok(new List<SearchResultDto>());

//            // 5. İndirim Mantığı (User Login Kontrolü)
//            bool isUserLoggedIn = User.Identity?.IsAuthenticated ?? false;
//            decimal discountMultiplier = isUserLoggedIn ? 0.90m : 1.0m;

//            // 6. Yanıtı Oluştur (Mapping)
//            // DTO'ya İlçe (District) ve Ülke (Country) bilgilerini de ekliyoruz.
//            var results = availableEntries
//                .Where(a => validRoomIds.Contains(a.RoomId))
//                .GroupBy(a => a.Room.Hotel)
//                .Select(g => new SearchResultDto
//                {
//                    HotelId = g.Key.Id,
//                    HotelName = g.Key.Name,
//                    City = g.Key.City,
//                    District = g.Key.District, // Yeni Alan
//                    Country = g.Key.Country,   // Yeni Alan
//                    Latitude = g.Key.Latitude,
//                    Longitude = g.Key.Longitude,
//                    Rating = g.Key.Rating,

//                    // Fiyat Hesaplama: En düşük gecelik fiyatı bulup indirim uyguluyoruz
//                    PricePerNight = g.Min(a => a.NightlyPrice > 0 ? a.NightlyPrice : a.Room.BasePrice) * discountMultiplier,

//                    AvailableRooms = g.Select(a => a.Room).Distinct().Select(r => new RoomReadDto
//                    {
//                        Id = r.Id,
//                        Title = r.Title,
//                        Capacity = r.Capacity,
//                        BasePrice = r.BasePrice * discountMultiplier,
//                        IsAvailable = true
//                    }).ToList()
//                })
//                .ToList();

//            return Ok(results);
//        }
//    }
//}



using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelSearchService.Data;
using HotelSearchService.Dtos;
using Microsoft.Extensions.Caching.Memory; // Caching

namespace HotelSearchService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache; // Cache Service
        private readonly ILogger<SearchController> _logger; // Loglama Service

        public SearchController(AppDbContext context, IMemoryCache cache, ILogger<SearchController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // GET: api/v1/Search?city=Bodrum&startDate=2026-06-01&endDate=2026-06-05&guests=2
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SearchResultDto>>> Search(
            [FromQuery] string? city,
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] int guests)
        {
            // 1. Tarih Validasyonu (Bunu cache'den önce yapıyoruz ki hata varsa boşuna cache bakmayalım)
            int totalNights = endDate.DayNumber - startDate.DayNumber;
            if (totalNights <= 0) return BadRequest("Invalid date range. The check out date must be after the check in date.");

            // 2. Kullanıcı Giriş Durumu (Cache Key için gerekli)
            bool isUserLoggedIn = User.Identity?.IsAuthenticated ?? false;

            // 3. Cache Key Oluşturma (Her parametreye göre eşsiz olmalı)
            // Örnek Key: "search_bodrum_2026-06-01_2026-06-05_2_loggedin_true"
            string searchTermKey = city?.ToLower().Trim() ?? "all";
            string cacheKey = $"search_{searchTermKey}_{startDate}_{endDate}_{guests}_loggedin_{isUserLoggedIn}";

            // 4. Cache Kontrolü
            if (!_cache.TryGetValue(cacheKey, out List<SearchResultDto> results))
            {
                // --- CACHE MISS (Veritabanına Git) ---
                _logger.LogWarning($"--> Cache MISS. The search is made in the database: {cacheKey}");

                // -----------------------------------------------------------
                // MEVCUT SORGULAMA MANTIĞI (Aynen korundu)
                // -----------------------------------------------------------

                var query = _context.Availabilities
                    .Include(a => a.Room).ThenInclude(r => r.Hotel)
                    .Where(a => a.Date >= startDate
                                && a.Date < endDate
                                && a.IsVacant == true
                                && a.AvailableStock > 0
                                && a.Room.Capacity >= guests);

                if (!string.IsNullOrEmpty(city))
                {
                    string searchTerm = city.ToLower().Trim();
                    query = query.Where(a =>
                        a.Room.Hotel.City.ToLower().Contains(searchTerm) ||
                        a.Room.Hotel.District.ToLower().Contains(searchTerm) ||
                        a.Room.Hotel.Country.ToLower().Contains(searchTerm) ||
                        a.Room.Hotel.Name.ToLower().Contains(searchTerm)
                    );
                }

                var availableEntries = await query.ToListAsync();

                var validRoomIds = availableEntries
                    .GroupBy(a => a.RoomId)
                    .Where(g => g.Count() == totalNights)
                    .Select(g => g.Key)
                    .ToList();

                if (!validRoomIds.Any())
                {
                    // Boş sonuç da olsa cache'e atalım ki tekrar tekrar boş yere sorgu atılmasın
                    results = new List<SearchResultDto>();
                }
                else
                {
                    decimal discountMultiplier = isUserLoggedIn ? 0.90m : 1.0m;

                    results = availableEntries
                        .Where(a => validRoomIds.Contains(a.RoomId))
                        .GroupBy(a => a.Room.Hotel)
                        .Select(g => new SearchResultDto
                        {
                            HotelId = g.Key.Id,
                            HotelName = g.Key.Name,
                            City = g.Key.City,
                            District = g.Key.District,
                            Country = g.Key.Country,
                            Latitude = g.Key.Latitude,
                            Longitude = g.Key.Longitude,
                            Rating = g.Key.Rating,
                            // Fiyat Hesaplama
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
                }

                // 5. Sonucu Cache'e Yaz (Örn: 5 dakika geçerli olsun)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(cacheKey, results, cacheOptions);
                // -----------------------------------------------------------
            }
            else
            {
                // --- CACHE HIT (RAM'den Getir) ---
                _logger.LogInformation($"--> Cache HIT. Results fetched from memory: {cacheKey}");
            }

            return Ok(results);
        }
    }
}