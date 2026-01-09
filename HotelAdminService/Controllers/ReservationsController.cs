using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAdminService.Data;
using HotelAdminService.Models;
using HotelAdminService.Dtos;

namespace HotelAdminService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/v1/Reservations
        [HttpPost]
        public async Task<IActionResult> CreateReservation(ReservationCreateDto input)
        {
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                if (input.CheckInDate >= input.CheckOutDate) return BadRequest("Invalid dates.");

                // 1. Get Availability
                var availabilities = await _context.Availabilities
                    .Where(a => a.RoomId == input.RoomId
                           && a.Date >= input.CheckInDate
                           && a.Date < input.CheckOutDate)
                    .ToListAsync();

                int nights = input.CheckOutDate.DayNumber - input.CheckInDate.DayNumber;

                // 2. Validate Stock
                if (availabilities.Count != nights || availabilities.Any(a => a.AvailableStock <= 0))
                {
                    return BadRequest("Room not available for selected dates.");
                }

                // 3. Decrement Stock
                foreach (var day in availabilities)
                {
                    day.AvailableStock -= 1;
                    _context.Entry(day).State = EntityState.Modified;
                }

                // 4. Create Reservation
                var reservation = new Reservation
                {
                    RoomId = input.RoomId,
                    UserId = input.GuestEmail ?? "Anonymous",
                    CheckInDate = input.CheckInDate,
                    CheckOutDate = input.CheckOutDate,
                    TotalPrice = availabilities.Sum(a => a.NightlyPrice),
                    Status = "Confirmed"
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { Message = "Booking Successful", ReservationId = reservation.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }

        // GET: api/v1/Reservations/MyReservations?email=...
        [HttpGet("MyReservations")]
        public async Task<ActionResult> GetMyReservations([FromQuery] string email)
        {
            var list = await _context.Reservations
                .Where(r => r.UserId == email)
                .OrderByDescending(r => r.CreatedAt) // Assuming you have a Created date or Id
                .ToListAsync();

            return Ok(list);
        }
    }
}