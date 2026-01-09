using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAdminService.Data;
using HotelAdminService.Dtos;

namespace HotelAdminService.Controllers
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

        // GET: api/v1/Search?city=Bodrum&startDate=2025-06-01&endDate=2025-06-05&guests=2
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SearchResultDto>>> Search(
            [FromQuery] string city,
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] int guests)
        {
            int totalNights = endDate.DayNumber - startDate.DayNumber;
            if (totalNights <= 0) return BadRequest("Invalid date range.");

            // 1. Find rooms that have availability entries for this range
            var availableRooms = await _context.Availabilities
                .Include(a => a.Room).ThenInclude(r => r.Hotel)
                .Where(a => a.Room.Hotel.City.ToLower().Contains(city)
                            && a.Date >= startDate
                            && a.Date < endDate
                            && a.IsVacant == true
                            && a.AvailableStock > 0
                            && a.Room.Capacity >= guests)
                .ToListAsync();

            // 2. Filter: Room must be available for ALL nights requested
            var validRoomIds = availableRooms
                .GroupBy(a => a.RoomId)
                .Where(g => g.Count() == totalNights)
                .Select(g => g.Key)
                .ToList();

            if (!validRoomIds.Any()) return Ok(new List<SearchResultDto>());

            // 3. Discount Logic (10% if logged in)
            bool isUserLoggedIn = Request.Headers.ContainsKey("Authorization"); // Simplified check
            decimal discountMultiplier = isUserLoggedIn ? 0.90m : 1.0m;

            // 4. Build Response
            var results = availableRooms
                .Where(a => validRoomIds.Contains(a.RoomId))
                .GroupBy(a => a.Room.Hotel)
                .Select(g => new SearchResultDto
                {
                    HotelId = g.Key.Id,
                    HotelName = g.Key.Name,
                    City = g.Key.City,
                    Latitude = g.Key.Latitude,
                    Longitude = g.Key.Longitude,
                    Rating = g.Key.Rating,
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