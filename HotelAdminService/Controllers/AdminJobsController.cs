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
        // This endpoint will be called by Azure Logic Apps every night
        [HttpPost("CheckCapacity")]
        public async Task<IActionResult> CheckCapacity()
        {
            _logger.LogInformation("Nightly Capacity Job Triggered via HTTP.");

            var start = DateOnly.FromDateTime(DateTime.Now);
            var end = start.AddDays(30);

            // 1. Find rooms with < 20% capacity
            var lowStockRooms = await _context.Availabilities
                .Include(a => a.Room).ThenInclude(r => r.Hotel)
                .Where(a => a.Date >= start && a.Date <= end)
                .Where(a => (double)a.AvailableStock / a.Room.TotalCount < 0.2)
                .ToListAsync();

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
                        Type = "LowCapacityAlert",
                        Hotel = item.Room.Hotel.Name,
                        Room = item.Room.Title,
                        Date = item.Date.ToString("yyyy-MM-dd"),
                        Remaining = item.AvailableStock,
                        Message = "URGENT: Stock is below 20%!"
                    };

                    var message = new ServiceBusMessage(JsonSerializer.Serialize(messageBody));
                    await sender.SendMessageAsync(message);
                    count++;
                }
                return Ok(new { Message = $"Job Complete. Sent {count} alerts." });
            }

            return Ok(new { Message = "Job Complete. No low stock found." });
        }
    }
}