using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelSearchService.Data;
using HotelSearchService.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace HotelSearchService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        public HotelsController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/v1/Hotels
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HotelReadDto>>> GetHotels()
        {
            const string cacheKey = "all_hotels_list_v2";

            // Try to get data from Cache
            if (!_cache.TryGetValue(cacheKey, out List<HotelReadDto> hotels))
            {
                // --- CACHE MISS ---
                var hotelsFromDb = await _context.Hotels
                    .Include(h => h.Rooms)
                    .AsNoTracking()
                    .ToListAsync();

                hotels = hotelsFromDb.Select(h => new HotelReadDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    City = h.City,
                    District = h.District, // Mapped
                    Country = h.Country,   // Mapped
                    Address = h.Address,   // Mapped
                    Latitude = h.Latitude, // Mapped
                    Longitude = h.Longitude, // Mapped
                    Description = h.Description,
                    Rating = h.Rating,
                    Rooms = h.Rooms.Select(r => new RoomReadDto
                    {
                        Id = r.Id,
                        Title = r.Title,
                        BasePrice = r.BasePrice,
                        Capacity = r.Capacity,
                        // Simple availability check for the catalog view
                        IsAvailable = r.TotalCount > 0
                    }).ToList()
                }).ToList();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(cacheKey, hotels, cacheOptions);
            }

            return Ok(hotels);
        }

        // GET: api/v1/Hotels/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<HotelReadDto>> GetHotel(int id)
        //{
        //    var hotel = await _context.Hotels
        //        .Include(h => h.Rooms) // Don't forget to include rooms for details page
        //        .FirstOrDefaultAsync(h => h.Id == id);

        //    if (hotel == null) return NotFound();

        //    return new HotelReadDto
        //    {
        //        Id = hotel.Id,
        //        Name = hotel.Name,
        //        City = hotel.City,
        //        District = hotel.District,
        //        Country = hotel.Country,
        //        Address = hotel.Address,
        //        Latitude = hotel.Latitude,
        //        Longitude = hotel.Longitude,
        //        Description = hotel.Description,
        //        Rating = hotel.Rating,
        //        Rooms = hotel.Rooms.Select(r => new RoomReadDto
        //        {
        //            Id = r.Id,
        //            Title = r.Title,
        //            BasePrice = r.BasePrice,
        //            Capacity = r.Capacity,
        //            IsAvailable = true
        //        }).ToList()
        //    };
        //}

        // GET: api/v1/Hotels/5?checkIn=2026-10-01&checkOut=2026-10-02
        [HttpGet("{id}")]
        public async Task<ActionResult<HotelReadDto>> GetHotel(
            int id,
            [FromQuery] DateOnly? checkIn,
            [FromQuery] DateOnly? checkOut)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Rooms)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null) return NotFound();

            // Odaları DTO'ya dönüştürürken fiyat kontrolü yapıyoruz
            var roomDtos = new List<RoomReadDto>();

            foreach (var room in hotel.Rooms)
            {
                decimal effectivePrice = room.BasePrice; // Varsayılan fiyat

                // Eğer tarih seçilmişse, Availability tablosundan o tarihlerin fiyatını çek
                if (checkIn.HasValue && checkOut.HasValue)
                {
                    // Seçilen tarih aralığındaki kayıtları bul
                    var availabilities = await _context.Availabilities
                        .Where(a => a.RoomId == room.Id
                                    && a.Date >= checkIn.Value
                                    && a.Date < checkOut.Value)
                        .ToListAsync();

                    // Eğer o tarihler için özel fiyat girildiyse (Availability varsa)
                    if (availabilities.Any())
                    {
                        // Basit mantık: O tarihlerdeki gecelik fiyatların ortalamasını alıyoruz.
                        effectivePrice = availabilities.Average(a => a.NightlyPrice);
                    }
                }

                roomDtos.Add(new RoomReadDto
                {
                    Id = room.Id,
                    Title = room.Title,
                    BasePrice = effectivePrice, // <--- GÜNCELLENMİŞ FİYAT
                    Capacity = room.Capacity,
                    IsAvailable = true
                });
            }

            return new HotelReadDto
            {
                Id = hotel.Id,
                Name = hotel.Name,
                City = hotel.City,
                District = hotel.District,
                Country = hotel.Country,
                Address = hotel.Address,
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude,
                Description = hotel.Description,
                Rating = hotel.Rating,
                Rooms = roomDtos // Güncellenmiş oda listesi
            };
        }


    }
}