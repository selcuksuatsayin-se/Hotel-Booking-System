//using Azure.Messaging.ServiceBus; // <--- service-bus
//using HotelBookingService.Data;
//using HotelBookingService.Dtos;
//using HotelBookingService.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System.Text.Json; // <--- service-bus

//namespace HotelBookingService.Controllers
//{
//    [Route("api/v1/[controller]")]
//    [ApiController]
//    public class ReservationsController : ControllerBase
//    {
//        private readonly AppDbContext _context;
//        private readonly IConfiguration _configuration;

//        public ReservationsController(AppDbContext context, IConfiguration configuration)
//        {
//            _context = context;
//            _configuration = configuration;
//        }

//        // POST: api/v1/Reservations
//        [HttpPost]
//        public async Task<IActionResult> CreateReservation(ReservationCreateDto input)
//        {
//            using var transaction = _context.Database.BeginTransaction();
//            try
//            {
//                if (input.CheckInDate >= input.CheckOutDate) return BadRequest("Invalid dates.");

//                // 1. Get Availability
//                var availabilities = await _context.Availabilities
//                    .Where(a => a.RoomId == input.RoomId
//                           && a.Date >= input.CheckInDate
//                           && a.Date < input.CheckOutDate)
//                    .ToListAsync();

//                int nights = input.CheckOutDate.DayNumber - input.CheckInDate.DayNumber;

//                // 2. Validate Stock
//                if (availabilities.Count != nights || availabilities.Any(a => a.AvailableStock <= 0))
//                {
//                    return BadRequest("Room not available for selected dates.");
//                }

//                // 3. Decrement Stock
//                foreach (var day in availabilities)
//                {
//                    day.AvailableStock -= 1;
//                    _context.Entry(day).State = EntityState.Modified;
//                }

//                // 4. Create Reservation
//                var reservation = new Reservation
//                {
//                    RoomId = input.RoomId,
//                    UserId = input.GuestEmail ?? "Anonymous",
//                    CheckInDate = input.CheckInDate,
//                    CheckOutDate = input.CheckOutDate,
//                    TotalPrice = availabilities.Sum(a => a.NightlyPrice),
//                    Status = "Confirmed"
//                };

//                _context.Reservations.Add(reservation);
//                await _context.SaveChangesAsync();

//                await transaction.CommitAsync();

//                var connectionString = _configuration.GetConnectionString("ServiceBusConnection");
//                var queueName = _configuration["ServiceBusQueue"];

//                await using var client = new ServiceBusClient(connectionString);
//                var sender = client.CreateSender(queueName);

//                // Create the message payload
//                var messageBody = new
//                {
//                    ReservationId = reservation.Id,
//                    GuestEmail = input.GuestEmail,
//                    HotelId = input.RoomId, // Logic simplification
//                    Dates = $"{input.CheckInDate} to {input.CheckOutDate}",
//                    Status = "Confirmed"
//                };

//                var message = new ServiceBusMessage(JsonSerializer.Serialize(messageBody));

//                // Send it!
//                await sender.SendMessageAsync(message);
//                // ---------------------------

//                return Ok(new { Message = "Booking Confirmed & Queued", Id = reservation.Id });
//            }
//            catch (Exception ex)
//            {
//                await transaction.RollbackAsync();
//                return StatusCode(500, ex.Message);
//            }
//        }

//        // GET: api/v1/Reservations/MyReservations?email=...
//        [HttpGet("MyReservations")]
//        public async Task<ActionResult> GetMyReservations([FromQuery] string email)
//        {
//            var list = await _context.Reservations
//                .Where(r => r.UserId == email)
//                .OrderByDescending(r => r.CreatedAt) // Assuming you have a Created date or Id
//                .ToListAsync();

//            return Ok(list);
//        }
//    }
//}

//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using HotelBookingService.Data;
//using HotelBookingService.Models;
//using HotelBookingService.Dtos;
//using System.Security.Claims; // User Identity için
//using Azure.Messaging.ServiceBus;
//using System.Text.Json;

//namespace HotelBookingService.Controllers
//{
//    [Route("api/v1/[controller]")]
//    [ApiController]
//    public class ReservationsController : ControllerBase
//    {
//        private readonly AppDbContext _context;
//        private readonly IConfiguration _configuration;

//        public ReservationsController(AppDbContext context, IConfiguration configuration)
//        {
//            _context = context;
//            _configuration = configuration;
//        }

//        // POST: api/v1/Reservations
//        // [Authorize] etiketi YOK! Çünkü Guest'ler de erişebilmeli.
//        //[HttpPost]
//        //public async Task<IActionResult> CreateReservation(ReservationCreateDto input)
//        //{
//        //    if (input.CheckInDate >= input.CheckOutDate) return BadRequest("Geçersiz tarih aralığı.");

//        //    // 1. Oda Bilgisini ve Fiyatını DB'den Çek (Güvenlik İçin)
//        //    // Availability kontrolü normalde burada yapılır ama basitleştiriyoruz.
//        //    var room = await _context.Rooms.FindAsync(input.RoomId);
//        //    if (room == null) return NotFound("Oda bulunamadı.");

//        //    // 2. İndirim Mantığı (Backend Tarafında)
//        //    bool isUserLoggedIn = User.Identity?.IsAuthenticated ?? false;
//        //    decimal finalPrice = room.BasePrice; // Varsayılan Tam Fiyat

//        //    if (isUserLoggedIn)
//        //    {
//        //        finalPrice = room.BasePrice * 0.90m; // %10 İndirim
//        //    }

//        //    // Gece Sayısı Hesabı
//        //    int nights = input.CheckOutDate.DayNumber - input.CheckInDate.DayNumber;
//        //    decimal totalPrice = finalPrice * nights;

//        //    // 3. Rezervasyonu Kaydet
//        //    var reservation = new Reservation
//        //    {
//        //        RoomId = input.RoomId,
//        //        // Eğer giriş yaptıysa Token'dan gelen maili al, yapmadıysa inputtan gelen maili al
//        //        UserId = isUserLoggedIn ? (User.Identity.Name ?? GetEmailFromClaims()) : input.GuestEmail,
//        //        CheckInDate = input.CheckInDate,
//        //        CheckOutDate = input.CheckOutDate,
//        //        TotalPrice = totalPrice,
//        //        Status = "Confirmed",
//        //        CreatedAt = DateTime.UtcNow
//        //    };

//        //    _context.Reservations.Add(reservation);
//        //    await _context.SaveChangesAsync();

//        //    // 4. Service Bus'a Mesaj At (Notification için)
//        //    await SendToQueue(reservation, input.GuestEmail ?? GetEmailFromClaims());

//        //    return Ok(new { Message = "Rezervasyon Başarılı", ReservationId = reservation.Id, Price = totalPrice });
//        //}


//        [HttpPost]
//        public async Task<IActionResult> CreateReservation(ReservationCreateDto input)
//        {
//            if (input.CheckInDate >= input.CheckOutDate) return BadRequest("Geçersiz tarih aralığı.");

//            // 1. Oda Var mı?
//            var room = await _context.Rooms.FindAsync(input.RoomId);
//            if (room == null) return NotFound("Oda bulunamadı.");

//            // --- YENİ FİYAT HESAPLAMA MANTIĞI ---
//            decimal totalBasePrice = 0;
//            var currentDate = input.CheckInDate;

//            // Döngü: Giriş tarihinden Çıkış tarihine kadar gün gün geziyoruz
//            while (currentDate < input.CheckOutDate)
//            {
//                // O gün için Admin özel fiyat girmiş mi?
//                var specialPrice = await _context.Availabilities
//                    .Where(a => a.RoomId == input.RoomId && a.Date == currentDate)
//                    .Select(a => a.NightlyPrice)
//                    .FirstOrDefaultAsync();

//                if (specialPrice > 0)
//                {
//                    // Özel fiyat varsa onu ekle (Örn: 130)
//                    totalBasePrice += specialPrice;
//                }
//                else
//                {
//                    // Yoksa odanın standart fiyatını ekle (Örn: 210)
//                    totalBasePrice += room.BasePrice;
//                }

//                currentDate = currentDate.AddDays(1);
//            }
//            // ------------------------------------

//            // 2. İndirim Mantığı (%10)
//            // Token kontrolü veya senin basit test mantığın
//            bool isUserLoggedIn = User.Identity?.IsAuthenticated ?? false;

//            // Eğer Azure'dan gelen token varsa veya claimlerde isim varsa logged-in say
//            if (!string.IsNullOrEmpty(input.GuestEmail) && (User.Identity?.Name == input.GuestEmail))
//            {
//                // Bazen frontend email'i guestEmail alanına da atıyor olabilir, kontrol edelim.
//                isUserLoggedIn = true;
//            }

//            decimal finalTotalPrice = totalBasePrice;

//            if (isUserLoggedIn)
//            {
//                finalTotalPrice = totalBasePrice * 0.90m; // %10 İndirim
//            }

//            // 3. Rezervasyonu Kaydet
//            var reservation = new Reservation
//            {
//                RoomId = input.RoomId,
//                UserId = isUserLoggedIn ? (User.Identity.Name ?? GetEmailFromClaims()) : input.GuestEmail,
//                CheckInDate = input.CheckInDate,
//                CheckOutDate = input.CheckOutDate,
//                TotalPrice = finalTotalPrice, // Hesaplanan yeni toplam fiyat
//                Status = "Confirmed",
//                CreatedAt = DateTime.UtcNow
//            };

//            _context.Reservations.Add(reservation);
//            await _context.SaveChangesAsync();

//            // 4. Service Bus
//            await SendToQueue(reservation, input.GuestEmail ?? GetEmailFromClaims());

//            return Ok(new { Message = "Rezervasyon Başarılı", ReservationId = reservation.Id, Price = finalTotalPrice });
//        }

//        // GET: api/v1/Reservations/MyReservations
//        [Authorize] // SADECE GİRİŞ YAPANLAR GÖREBİLİR
//        [HttpGet("MyReservations")]
//        public async Task<ActionResult> GetMyReservations()
//        {
//            // Token'dan email'i otomatik alıyoruz. Parametre olarak almaya gerek yok (Güvenlik).
//            string email = GetEmailFromClaims();

//            if (string.IsNullOrEmpty(email))
//                email = User.Identity?.Name; // Bazen buraya düşer

//            if (string.IsNullOrEmpty(email)) return Unauthorized();

//            var reservations = await _context.Reservations
//                .Include(r => r.Room)
//                .ThenInclude(rm => rm.Hotel)
//                .Where(r => r.UserId == email)
//                .OrderByDescending(r => r.CreatedAt)
//                .Select(r => new
//                {
//                    Id = r.Id,
//                    HotelName = r.Room.Hotel.Name,
//                    City = r.Room.Hotel.City,
//                    RoomTitle = r.Room.Title,
//                    CheckIn = r.CheckInDate,
//                    CheckOut = r.CheckOutDate,
//                    TotalPrice = r.TotalPrice,
//                    Status = r.Status
//                })
//                .ToListAsync();

//            return Ok(reservations);
//        }

//        // Yardımcı Metodlar
//        private string GetEmailFromClaims()
//        {
//            // Azure AD genelde "preferred_username" veya "emails" claim'i kullanır
//            return User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
//                   ?? User.Claims.FirstOrDefault(c => c.Type == "emails")?.Value
//                   ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
//        }

//        private async Task SendToQueue(Reservation reservation, string email)
//        {
//            try
//            {
//                var connectionString = _configuration.GetConnectionString("ServiceBusConnection");
//                var queueName = _configuration["ServiceBusQueue"];
//                await using var client = new ServiceBusClient(connectionString);
//                var sender = client.CreateSender(queueName);

//                var messageBody = new
//                {
//                    ReservationId = reservation.Id,
//                    GuestEmail = email,
//                    Dates = $"{reservation.CheckInDate} to {reservation.CheckOutDate}",
//                    Status = "Confirmed"
//                };
//                var message = new ServiceBusMessage(JsonSerializer.Serialize(messageBody));
//                await sender.SendMessageAsync(message);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Queue Error: " + ex.Message);
//            }
//        }
//    }
//}








//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using HotelBookingService.Data;
//using HotelBookingService.Models;
//using HotelBookingService.Dtos;
//using System.Security.Claims; 
//using Azure.Messaging.ServiceBus;
//using System.Text.Json;

//namespace HotelBookingService.Controllers
//{
//    [Route("api/v1/[controller]")]
//    [ApiController]
//    public class ReservationsController : ControllerBase
//    {
//        private readonly AppDbContext _context;
//        private readonly IConfiguration _configuration;

//        public ReservationsController(AppDbContext context, IConfiguration configuration)
//        {
//            _context = context;
//            _configuration = configuration;
//        }

//        [HttpPost]
//        public async Task<IActionResult> CreateReservation(ReservationCreateDto input)
//        {
//            if (input.CheckInDate >= input.CheckOutDate) return BadRequest("Geçersiz tarih aralığı.");

//            // TRANSACTION BAŞLATIYORUZ (Stok düşerken hata olursa her şeyi geri almak için)
//            using var transaction = await _context.Database.BeginTransactionAsync();

//            try
//            {
//                // 1. Odayı Bul
//                var room = await _context.Rooms.FindAsync(input.RoomId);
//                if (room == null) return NotFound("Oda bulunamadı.");

//                decimal totalBasePrice = 0;
//                var currentDate = input.CheckInDate;

//                // ---------------------------------------------------------
//                // FAZ 1: STOK KONTROLÜ (Önce tüm günlere bak, yer var mı?)
//                // ---------------------------------------------------------
//                var checkDate = input.CheckInDate;
//                while (checkDate < input.CheckOutDate)
//                {
//                    // O gün için özel stok kaydı var mı?
//                    var availability = await _context.Availabilities
//                        .FirstOrDefaultAsync(a => a.RoomId == input.RoomId && a.Date == checkDate);

//                    if (availability != null)
//                    {
//                        // Kayıt varsa ve stok 0 ise veya kapalıysa -> HATA
//                        if (!availability.IsVacant || availability.AvailableStock <= 0)
//                        {
//                            return BadRequest($"Üzgünüz, {checkDate} tarihinde odamız dolu.");
//                        }
//                    }
//                    else 
//                    {
//                        // Kayıt yoksa odanın genel kapasitesine bak
//                        if (room.TotalCount <= 0) 
//                        {
//                            return BadRequest($"Üzgünüz, {checkDate} tarihinde odamız dolu.");
//                        }
//                    }
//                    checkDate = checkDate.AddDays(1);
//                }

//                // ---------------------------------------------------------
//                // FAZ 2: STOK DÜŞÜRME & FİYAT HESAPLAMA (Decrease Capacity)
//                // ---------------------------------------------------------
//                currentDate = input.CheckInDate;
//                while (currentDate < input.CheckOutDate)
//                {
//                    var availability = await _context.Availabilities
//                        .FirstOrDefaultAsync(a => a.RoomId == input.RoomId && a.Date == currentDate);

//                    decimal dailyPrice = room.BasePrice;

//                    if (availability != null)
//                    {
//                        // VARSA: Stoğu 1 azalt
//                        availability.AvailableStock -= 1;

//                        // Stok bittiyse odayı kapat
//                        if (availability.AvailableStock == 0) availability.IsVacant = false;

//                        dailyPrice = availability.NightlyPrice;
//                        _context.Entry(availability).State = EntityState.Modified;
//                    }
//                    else
//                    {
//                        // YOKSA: Yeni kayıt oluştur ve kapasiteyi (Total - 1) yap
//                        var newAvailability = new Availability
//                        {
//                            RoomId = input.RoomId,
//                            Date = currentDate,
//                            IsVacant = true,
//                            AvailableStock = room.TotalCount - 1, // 1 tane düştük
//                            NightlyPrice = room.BasePrice
//                        };
//                        _context.Availabilities.Add(newAvailability);
//                    }

//                    totalBasePrice += dailyPrice;
//                    currentDate = currentDate.AddDays(1);
//                }

//                // ---------------------------------------------------------
//                // FAZ 3: REZERVASYON KAYDI
//                // ---------------------------------------------------------

//                // İndirim Mantığı
//                bool isUserLoggedIn = User.Identity?.IsAuthenticated ?? false;
//                if (!string.IsNullOrEmpty(input.GuestEmail) && (User.Identity?.Name == input.GuestEmail)) isUserLoggedIn = true;

//                decimal finalTotalPrice = isUserLoggedIn ? totalBasePrice * 0.90m : totalBasePrice;

//                var reservation = new Reservation
//                {
//                    RoomId = input.RoomId,
//                    UserId = isUserLoggedIn ? (User.Identity.Name ?? GetEmailFromClaims()) : input.GuestEmail,
//                    CheckInDate = input.CheckInDate,
//                    CheckOutDate = input.CheckOutDate,
//                    TotalPrice = finalTotalPrice,
//                    Status = "Confirmed",
//                    CreatedAt = DateTime.UtcNow
//                };

//                _context.Reservations.Add(reservation);
//                await _context.SaveChangesAsync();

//                // İşlemleri onayla (Veritabanına kalıcı olarak yaz)
//                await transaction.CommitAsync();

//                // Service Bus'a Mesaj At (Notification için)
//                await SendToQueue(reservation, input.GuestEmail ?? GetEmailFromClaims());

//                return Ok(new { Message = "Rezervasyon Başarılı", ReservationId = reservation.Id, Price = finalTotalPrice });
//            }
//            catch (Exception ex)
//            {
//                // Hata olursa hiçbir şeyi kaydetme
//                await transaction.RollbackAsync();
//                return StatusCode(500, "Rezervasyon hatası: " + ex.Message);
//            }
//        }

//        // GET: api/v1/Reservations/MyReservations
//        [Authorize] 
//        [HttpGet("MyReservations")]
//        public async Task<ActionResult> GetMyReservations()
//        {
//            string email = GetEmailFromClaims();
//            if (string.IsNullOrEmpty(email)) email = User.Identity?.Name;
//            if (string.IsNullOrEmpty(email)) return Unauthorized();

//            var reservations = await _context.Reservations
//                .Include(r => r.Room)
//                .ThenInclude(rm => rm.Hotel)
//                .Where(r => r.UserId == email)
//                .OrderByDescending(r => r.CreatedAt)
//                .Select(r => new 
//                {
//                    Id = r.Id,
//                    HotelName = r.Room.Hotel.Name,
//                    City = r.Room.Hotel.City,
//                    RoomTitle = r.Room.Title,
//                    CheckIn = r.CheckInDate,
//                    CheckOut = r.CheckOutDate,
//                    TotalPrice = r.TotalPrice,
//                    Status = r.Status
//                })
//                .ToListAsync();

//            return Ok(reservations);
//        }

//        private string GetEmailFromClaims()
//        {
//            return User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value 
//                   ?? User.Claims.FirstOrDefault(c => c.Type == "emails")?.Value
//                   ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
//        }

//        private async Task SendToQueue(Reservation reservation, string email)
//        {
//             try 
//             {
//                var connectionString = _configuration.GetConnectionString("ServiceBusConnection");
//                var queueName = _configuration["ServiceBusQueue"];
//                await using var client = new ServiceBusClient(connectionString);
//                var sender = client.CreateSender(queueName);

//                var messageBody = new
//                {
//                    ReservationId = reservation.Id,
//                    GuestEmail = email,
//                    Dates = $"{reservation.CheckInDate} to {reservation.CheckOutDate}",
//                    Status = "Confirmed"
//                };
//                var message = new ServiceBusMessage(JsonSerializer.Serialize(messageBody));
//                await sender.SendMessageAsync(message);
//             }
//             catch(Exception ex) 
//             {
//                 Console.WriteLine("Queue Error: " + ex.Message);
//             }
//        }
//    }
//}




using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingService.Data;
using HotelBookingService.Models;
using HotelBookingService.Dtos;
using System.Security.Claims;
using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace HotelBookingService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public ReservationsController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation(ReservationCreateDto input)
        {
            if (input.CheckInDate >= input.CheckOutDate) return BadRequest("Geçersiz tarih aralığı.");

            // TRANSACTION START
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Find the Room
                var room = await _context.Rooms.FindAsync(input.RoomId);
                if (room == null) return NotFound("Oda bulunamadı.");

                decimal totalBasePrice = 0;
                var currentDate = input.CheckInDate;

                // ---------------------------------------------------------
                // PHASE 1: STOCK CHECK (Check availability for all days first)
                // ---------------------------------------------------------
                var checkDate = input.CheckInDate;
                while (checkDate < input.CheckOutDate)
                {
                    // Is there a specific availability record for this date?
                    var availability = await _context.Availabilities
                        .FirstOrDefaultAsync(a => a.RoomId == input.RoomId && a.Date == checkDate);

                    if (availability != null)
                    {
                        // If record exists, check if vacant and stock > 0
                        if (!availability.IsVacant || availability.AvailableStock <= 0)
                        {
                            return BadRequest($"Üzgünüz, {checkDate} tarihinde odamız dolu.");
                        }
                    }
                    else
                    {
                        // If no record exists, check the general room capacity
                        if (room.TotalCount <= 0)
                        {
                            return BadRequest($"Üzgünüz, {checkDate} tarihinde odamız dolu.");
                        }
                    }
                    checkDate = checkDate.AddDays(1);
                }

                // ---------------------------------------------------------
                // PHASE 2: DECREASE STOCK & CALCULATE PRICE
                // ---------------------------------------------------------
                currentDate = input.CheckInDate;
                while (currentDate < input.CheckOutDate)
                {
                    var availability = await _context.Availabilities
                        .FirstOrDefaultAsync(a => a.RoomId == input.RoomId && a.Date == currentDate);

                    decimal dailyPrice = room.BasePrice;

                    if (availability != null)
                    {
                        // Record exists: Decrease stock by 1
                        availability.AvailableStock -= 1;

                        // If stock reaches 0, mark as occupied
                        if (availability.AvailableStock <= 0)
                        {
                            availability.AvailableStock = 0;
                            availability.IsVacant = false;
                        }

                        dailyPrice = availability.NightlyPrice;
                        _context.Entry(availability).State = EntityState.Modified;
                    }
                    else
                    {
                        // No Record: Create new record with (TotalCount - 1)
                        var newAvailability = new Availability
                        {
                            RoomId = input.RoomId,
                            Date = currentDate,
                            IsVacant = true,
                            AvailableStock = room.TotalCount - 1,
                            NightlyPrice = room.BasePrice
                        };
                        _context.Availabilities.Add(newAvailability);
                    }

                    totalBasePrice += dailyPrice;
                    currentDate = currentDate.AddDays(1);
                }

                // ---------------------------------------------------------
                // PHASE 3: CREATE RESERVATION
                // ---------------------------------------------------------

                // Discount Logic
                bool isUserLoggedIn = User.Identity?.IsAuthenticated ?? false;
                if (!string.IsNullOrEmpty(input.GuestEmail) && (User.Identity?.Name == input.GuestEmail)) isUserLoggedIn = true;

                decimal finalTotalPrice = isUserLoggedIn ? totalBasePrice * 0.90m : totalBasePrice;

                var reservation = new Reservation
                {
                    RoomId = input.RoomId,
                    UserId = isUserLoggedIn ? (User.Identity.Name ?? GetEmailFromClaims()) : input.GuestEmail,
                    CheckInDate = input.CheckInDate,
                    CheckOutDate = input.CheckOutDate,
                    TotalPrice = finalTotalPrice,
                    Status = "Confirmed",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                // Commit Transaction
                await transaction.CommitAsync();

                // Send Notification Message (For New Booking)
                await SendToQueue(reservation, input.GuestEmail ?? GetEmailFromClaims());

                return Ok(new { Message = "Rezervasyon Başarılı", ReservationId = reservation.Id, Price = finalTotalPrice });
            }
            catch (Exception ex)
            {
                // Rollback on error
                await transaction.RollbackAsync();
                return StatusCode(500, "Rezervasyon hatası: " + ex.Message);
            }
        }

        // GET: api/v1/Reservations/MyReservations
        [Authorize]
        [HttpGet("MyReservations")]
        public async Task<ActionResult> GetMyReservations()
        {
            string email = GetEmailFromClaims();
            if (string.IsNullOrEmpty(email)) email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var reservations = await _context.Reservations
                .Include(r => r.Room)
                .ThenInclude(rm => rm.Hotel)
                .Where(r => r.UserId == email)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    Id = r.Id,
                    HotelName = r.Room.Hotel.Name,
                    City = r.Room.Hotel.City,
                    RoomTitle = r.Room.Title,
                    CheckIn = r.CheckInDate,
                    CheckOut = r.CheckOutDate,
                    TotalPrice = r.TotalPrice,
                    Status = r.Status
                })
                .ToListAsync();

            return Ok(reservations);
        }

        private string GetEmailFromClaims()
        {
            return User.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                   ?? User.Claims.FirstOrDefault(c => c.Type == "emails")?.Value
                   ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        }

        private async Task SendToQueue(Reservation reservation, string email)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("ServiceBusConnection");
                var queueName = _configuration["ServiceBusQueue"];
                await using var client = new ServiceBusClient(connectionString);
                var sender = client.CreateSender(queueName);

                var messageBody = new
                {
                    ReservationId = reservation.Id,
                    GuestEmail = email,
                    Dates = $"{reservation.CheckInDate} to {reservation.CheckOutDate}",
                    Status = "Confirmed"
                };
                var message = new ServiceBusMessage(JsonSerializer.Serialize(messageBody));
                await sender.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Queue Error: " + ex.Message);
            }
        }
    }
}