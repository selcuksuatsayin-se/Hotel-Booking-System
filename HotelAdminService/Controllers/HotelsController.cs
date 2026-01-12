using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAdminService.Data;
using HotelAdminService.Models;
using HotelAdminService.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace HotelAdminService.Controllers
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
            // 3. Define a unique key for this data
            const string cacheKey = "all_hotels_list";

            // 4. Try to get data from Cache
            if (!_cache.TryGetValue(cacheKey, out List<HotelReadDto> hotels))
            {
                // --- CACHE MISS (Data not found in memory) ---
                // We must query the database
                Console.WriteLine("Fetching from Database..."); // Visual log for testing

                var hotelsFromDb = await _context.Hotels.Include(h => h.Rooms).ToListAsync();

                hotels = hotelsFromDb.Select(h => new HotelReadDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    City = h.City,
                    Address = h.Address,
                    Latitude = h.Latitude,
                    Longitude = h.Longitude,
                    Description = h.Description,
                    Rating = h.Rating
                }).ToList();

                // 5. Set Cache Options (How long to keep it?)
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5)) // Expire after 5 mins
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2)); // Or if unused for 2 mins

                // 6. Save to Cache
                _cache.Set(cacheKey, hotels, cacheOptions);
            }
            else
            {
                // --- CACHE HIT ---
                Console.WriteLine("Returning from Cache!"); // Visual log for testing
            }

            return Ok(hotels);
        }

        // GET: api/v1/Hotels/5
        [HttpGet("{id}")]
        public async Task<ActionResult<HotelReadDto>> GetHotel(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);

            if (hotel == null) return NotFound();

            return new HotelReadDto
            {
                Id = hotel.Id,
                Name = hotel.Name,
                City = hotel.City,
                Address = hotel.Address,
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude,
                Description = hotel.Description,
                Rating = hotel.Rating
            };
        }

        // POST: api/v1/Hotels
        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<HotelReadDto>> CreateHotel(HotelCreateDto input)
        {
            var hotel = new Hotel
            {
                Name = input.Name,
                City = input.City,
                Address = input.Address,
                Latitude = input.Latitude,
                Longitude = input.Longitude,
                Description = input.Description,
                Rating = input.Rating
            };

            _context.Hotels.Add(hotel);
            await _context.SaveChangesAsync();

            // Return the created DTO
            var readDto = new HotelReadDto
            {
                Id = hotel.Id,
                Name = hotel.Name,
                City = hotel.City,
                Address = hotel.Address,
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude,
                Description = hotel.Description,
                Rating = hotel.Rating
            };

            _cache.Remove("all_hotels_list");

            return CreatedAtAction(nameof(GetHotel), new { id = hotel.Id }, readDto);
        }

        // PUT: api/v1/Hotels/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHotel(int id, HotelCreateDto input)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null) return NotFound();

            // Update fields
            hotel.Name = input.Name;
            hotel.City = input.City;
            hotel.Address = input.Address;
            hotel.Latitude = input.Latitude;
            hotel.Longitude = input.Longitude;
            hotel.Description = input.Description;
            hotel.Rating = input.Rating;

            await _context.SaveChangesAsync();

            _cache.Remove("all_hotels_list");

            return NoContent();
        }

        // DELETE: api/v1/Hotels/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null) return NotFound();

            _context.Hotels.Remove(hotel);
            await _context.SaveChangesAsync();

            _cache.Remove("all_hotels_list");

            return NoContent();
        }
    }
}