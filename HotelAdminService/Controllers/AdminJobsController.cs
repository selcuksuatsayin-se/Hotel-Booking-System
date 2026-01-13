using Azure.Messaging.ServiceBus;
using HotelAdminService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelAdminService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AdminJobsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminJobsController> _logger;

        public AdminJobsController(AppDbContext context, IConfiguration configuration, ILogger<AdminJobsController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // POST: api/v1/AdminJobs/CheckCapacity
        [HttpPost("CheckCapacity")]
        public async Task<IActionResult> CheckCapacity()
        {
            _logger.LogInformation("Nightly Capacity Job Triggered via HTTP.");

            var start = DateOnly.FromDateTime(DateTime.Now);

            // DÜZELTME 1: Test tarihlerini yakalayabilmek için süreyi 1 yıla çıkardık.
            var end = start.AddDays(365);

            // 1. Find rooms with < 20% capacity
            var lowStockRooms = await _context.Availabilities
                .Include(a => a.Room).ThenInclude(r => r.Hotel)
                .Where(a => a.Date >= start && a.Date <= end)
                // DÜZELTME 2: Bölme hatası riskine karşı çarpma mantığı (Stock * 5 < Total ise %20'den azdır)
                // Örn: Stok 1, Total 10 -> 1*5 < 10 -> 5 < 10 (TRUE)
                .Where(a => (a.AvailableStock * 5) < a.Room.TotalCount)
                .ToListAsync();

            _logger.LogInformation($"Found {lowStockRooms.Count} rooms with low stock.");

            if (lowStockRooms.Any())
            {
                var connectionString = _configuration.GetConnectionString("ServiceBusConnection");
                var queueName = _configuration["ServiceBusQueue"];
                await using var client = new ServiceBusClient(connectionString);
                var sender = client.CreateSender(queueName);

                int count = 0;
                foreach (var item in lowStockRooms)
                {
                    var messageBody = new
                    {
                        Type = "LowCapacityAlert", // Worker bu Type'ı bekliyor
                        Hotel = item.Room.Hotel.Name,
                        Room = item.Room.Title,
                        Date = item.Date.ToString("yyyy-MM-dd"),
                        Remaining = item.AvailableStock,
                        Total = item.Room.TotalCount,
                        Message = $"URGENT: Stock is below 20% ({item.AvailableStock}/{item.Room.TotalCount})!"
                    };

                    var message = new ServiceBusMessage(JsonSerializer.Serialize(messageBody));
                    await sender.SendMessageAsync(message);
                    count++;
                }

                string resultMsg = $"Job Complete. Sent {count} alerts.";
                _logger.LogInformation(resultMsg);
                return Ok(new { Message = resultMsg });
            }

            return Ok(new { Message = "Job Complete. No low stock found within the next 365 days." });
        }
    }
}