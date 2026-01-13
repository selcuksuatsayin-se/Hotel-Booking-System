using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelAdminService.Data;
using HotelAdminService.Models;
using HotelAdminService.Dtos;

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

        // POST: api/v1/Availability/BulkUpdate
        [HttpPost("BulkUpdate")]
        public async Task<IActionResult> BulkUpdate([FromBody] AvailabilityBulkUpdateDto input)
        {
            if (input.StartDate >= input.EndDate)
                return BadRequest("Bitiş tarihi başlangıçtan sonra olmalıdır.");

            // 1. Önce Admin'in seçtiği oteldeki ilgili odayı bulalım (Örn: Swiss Hotel -> Standard Room)
            // Not: Title araması yapıyoruz çünkü admin dropdown'dan "Standard" seçti.
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.HotelId == input.HotelId && r.Title.Contains(input.RoomTitle));

            if (room == null)
                return NotFound($"Bu otelde '{input.RoomTitle}' tipinde bir oda bulunamadı.");

            // 2. Tarih döngüsü
            var currentDate = input.StartDate;
            while (currentDate < input.EndDate)
            {
                // O gün için kayıt var mı?
                var availability = await _context.Availabilities
                    .FirstOrDefaultAsync(a => a.RoomId == room.Id && a.Date == currentDate);

                if (availability != null)
                {
                    // VARSA GÜNCELLE
                    availability.NightlyPrice = input.Price;
                    availability.IsVacant = input.IsVacant;

                    // Eğer "Occupied/Kapalı" seçildiyse stoğu 0 yap, değilse girilen stoğu yaz
                    availability.AvailableStock = input.IsVacant ? input.Stock : 0;

                    _context.Entry(availability).State = EntityState.Modified;
                }
                else
                {
                    // YOKSA YENİ EKLE
                    var newAvail = new Availability
                    {
                        RoomId = room.Id,
                        Date = currentDate,
                        NightlyPrice = input.Price,
                        IsVacant = input.IsVacant,
                        AvailableStock = input.IsVacant ? input.Stock : 0
                    };
                    _context.Availabilities.Add(newAvail);
                }

                currentDate = currentDate.AddDays(1);
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Müsaitlik ve fiyatlar başarıyla güncellendi." });
        }
    }
}


