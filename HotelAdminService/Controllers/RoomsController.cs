using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAdminService.Data;
using HotelAdminService.Models;
using HotelAdminService.Dtos;

namespace HotelAdminService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RoomsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/v1/Rooms/ByHotel/1
        [HttpGet("ByHotel/{hotelId}")]
        public async Task<ActionResult<IEnumerable<RoomReadDto>>> GetRoomsByHotel(int hotelId)
        {
            var rooms = await _context.Rooms
                .Where(r => r.HotelId == hotelId)
                .Select(r => new RoomReadDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    BasePrice = r.BasePrice,
                    Capacity = r.Capacity
                })
                .ToListAsync();

            return Ok(rooms);
        }

        // GET: api/v1/Rooms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomReadDto>> GetRoom(int id)
        {
            var r = await _context.Rooms.FindAsync(id);
            if (r == null) return NotFound();

            return new RoomReadDto
            {
                Id = r.Id,
                Title = r.Title,
                BasePrice = r.BasePrice,
                Capacity = r.Capacity
            };
        }

        // POST: api/v1/Rooms
        [HttpPost]
        public async Task<ActionResult<RoomReadDto>> CreateRoom(RoomCreateDto input)
        {
            var hotelExists = await _context.Hotels.AnyAsync(h => h.Id == input.HotelId);
            if (!hotelExists) return BadRequest("Invalid Hotel ID.");

            var room = new Room
            {
                HotelId = input.HotelId,
                Title = input.Title,
                BasePrice = input.BasePrice,
                Capacity = input.Capacity,
                TotalCount = input.TotalCount
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            var readDto = new RoomReadDto
            {
                Id = room.Id,
                Title = room.Title,
                BasePrice = room.BasePrice,
                Capacity = room.Capacity
            };

            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, readDto);
        }

        // PUT: api/v1/Rooms/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoom(int id, RoomCreateDto input)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            room.Title = input.Title;
            room.BasePrice = input.BasePrice;
            room.Capacity = input.Capacity;
            room.TotalCount = input.TotalCount;
            // Note: Usually we don't allow moving a room to a different hotel (HotelId), so we skip that.

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/v1/Rooms/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}