using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAdminService.Data;
using HotelAdminService.Models;
using HotelAdminService.Dtos;

namespace HotelAdminService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HotelsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/v1/Hotels
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HotelReadDto>>> GetHotels()
        {
            var hotels = await _context.Hotels
                .Select(h => new HotelReadDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    City = h.City,
                    Address = h.Address,
                    Latitude = h.Latitude,
                    Longitude = h.Longitude,
                    Description = h.Description,
                    Rating = h.Rating
                })
                .ToListAsync();

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

            return NoContent();
        }
    }
}