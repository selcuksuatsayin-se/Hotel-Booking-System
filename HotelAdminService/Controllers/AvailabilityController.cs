using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAdminService.Data;
using HotelAdminService.Models;

namespace HotelAdminService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AvailabilityController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AvailabilityController(AppDbContext context)
        {
            _context = context;
        }

        public class AvailabilityDto
        {
            public int RoomId { get; set; }
            public DateOnly StartDate { get; set; }
            public DateOnly EndDate { get; set; }
            public int AvailableStock { get; set; }
            public decimal Price { get; set; }
            public bool IsVacant { get; set; }
        }

        // POST: api/v1/Availability/BulkUpdate
        [HttpPost("BulkUpdate")]
        public async Task<IActionResult> BulkUpdate([FromBody] AvailabilityDto request)
        {
            if (request.StartDate > request.EndDate)
                return BadRequest("Start date must be before end date.");

            // Loop through every day in the range
            for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
            {
                var existing = await _context.Availabilities
                    .FirstOrDefaultAsync(a => a.RoomId == request.RoomId && a.Date == date);

                if (existing != null)
                {
                    existing.AvailableStock = request.AvailableStock;
                    existing.IsVacant = request.IsVacant;
                    existing.NightlyPrice = request.Price;
                    _context.Entry(existing).State = EntityState.Modified;
                }
                else
                {
                    _context.Availabilities.Add(new Availability
                    {
                        RoomId = request.RoomId,
                        Date = date,
                        AvailableStock = request.AvailableStock,
                        IsVacant = request.IsVacant,
                        NightlyPrice = request.Price
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Availability updated successfully." });
        }

        // GET: api/v1/Availability/5
        // View availability for a specific room
        [HttpGet("{roomId}")]
        public async Task<ActionResult> GetAvailability(int roomId, [FromQuery] DateOnly? start, [FromQuery] DateOnly? end)
        {
            var query = _context.Availabilities.Where(a => a.RoomId == roomId);

            if (start.HasValue) query = query.Where(a => a.Date >= start);
            if (end.HasValue) query = query.Where(a => a.Date <= end);

            var list = await query.OrderBy(a => a.Date).ToListAsync();
            return Ok(list);
        }
    }
}