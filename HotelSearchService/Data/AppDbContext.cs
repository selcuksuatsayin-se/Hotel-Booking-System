using Microsoft.EntityFrameworkCore;
using HotelSearchService.Models;

namespace HotelSearchService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Availability> Availabilities { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Define Relationships
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Hotel)
                .WithMany(h => h.Rooms)
                .HasForeignKey(r => r.HotelId);

            modelBuilder.Entity<Availability>()
                .HasOne(a => a.Room)
                .WithMany(r => r.Availabilities)
                .HasForeignKey(a => a.RoomId);

            // Constraint: One availability record per room per day
            modelBuilder.Entity<Availability>()
                .HasIndex(a => new { a.RoomId, a.Date })
                .IsUnique();
        }
    }
}