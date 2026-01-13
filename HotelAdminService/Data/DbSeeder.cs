using HotelAdminService.Models;

namespace HotelAdminService.Data
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Eğer veritabanında otel varsa ekleme yapma
            if (context.Hotels.Any()) return;

            var hotels = new List<Hotel>
            {
                new Hotel { Name = "Swiss Hotel Bodrum", City = "Muğla", District = "Bodrum", Country = "Turkey", Address = "Turgutreis Cad.", Rating = 9.5, Description = "Luxury stay near the sea", Latitude = 37.03, Longitude = 27.43 },
                new Hotel { Name = "Rixos Premium", City = "Muğla", District = "Bodrum", Country = "Turkey", Address = "Torba Mah.", Rating = 9.8, Description = "All inclusive premium experience", Latitude = 37.08, Longitude = 27.45 },
                new Hotel { Name = "Mövenpick Istanbul", City = "İstanbul", District = "Zeytinburnu", Country = "Turkey", Address = "Kennedy Cad.", Rating = 8.9, Description = "Magnificent view of Marmara Sea", Latitude = 40.98, Longitude = 28.90 },
                new Hotel { Name = "Hilton Izmir", City = "İzmir", District = "Konak", Country = "Turkey", Address = "Gazi Osman Paşa Bulvarı", Rating = 9.2, Description = "Heart of the city", Latitude = 38.42, Longitude = 27.13 },
                new Hotel { Name = "Akra Hotel", City = "Antalya", District = "Muratpaşa", Country = "Turkey", Address = "Lara Yolu", Rating = 9.4, Description = "Mediterranean breeze", Latitude = 36.85, Longitude = 30.75 }
            };

            context.Hotels.AddRange(hotels);
            context.SaveChanges();

            // Her otele Odalar ekle
            var rooms = new List<Room>();
            foreach (var hotel in hotels)
            {
                rooms.Add(new Room { HotelId = hotel.Id, Title = "Standard Room", Capacity = 2, TotalCount = 10, BasePrice = 100 + (hotel.Id * 10) });
                rooms.Add(new Room { HotelId = hotel.Id, Title = "Deluxe Room", Capacity = 3, TotalCount = 5, BasePrice = 200 + (hotel.Id * 10) });
            }
            context.Rooms.AddRange(rooms);
            context.SaveChanges();

            // !!! EN ÖNEMLİ KISIM !!!
            // Gelecek 60 gün için Müsaitlik (Availability) tablosunu doldur
            // Bu tablo olmazsa arama sonucu boş döner!
            var availabilities = new List<Availability>();
            var today = DateOnly.FromDateTime(DateTime.Now);

            foreach (var room in rooms)
            {
                for (int i = 0; i < 60; i++) // 2 aylık veri bas
                {
                    availabilities.Add(new Availability
                    {
                        RoomId = room.Id,
                        Date = today.AddDays(i),
                        AvailableStock = room.TotalCount, // Her gün için 5 boş oda var
                        NightlyPrice = room.BasePrice,
                        IsVacant = true
                    });
                }
            }
            context.Availabilities.AddRange(availabilities);
            context.SaveChanges();
        }
    }
}