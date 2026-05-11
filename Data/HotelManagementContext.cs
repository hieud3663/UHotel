using Microsoft.EntityFrameworkCore;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public class HotelManagementContext : DbContext
    {
        public HotelManagementContext(DbContextOptions<HotelManagementContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<RoomCategory> RoomCategories { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Pricing> Pricings { get; set; }
        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<HotelService> HotelServices { get; set; }
        public DbSet<ReservationForm> ReservationForms { get; set; }
        public DbSet<HistoryCheckin> HistoryCheckins { get; set; }
        public DbSet<HistoryCheckOut> HistoryCheckOuts { get; set; }
        public DbSet<RoomChangeHistory> RoomChangeHistories { get; set; }
        public DbSet<RoomUsageService> RoomUsageServices { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<ConfirmationReceipt> ConfirmationReceipts { get; set; }
        public DbSet<RoomTask> RoomTasks { get; set; }
        public DbSet<RoomTaskHistory> RoomTaskHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<ReservationForm>()
                .HasOne(r => r.Employee)
                .WithMany(e => e.ReservationForms)
                .HasForeignKey(r => r.EmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationForm>()
                .HasOne(r => r.Room)
                .WithMany(ro => ro.ReservationForms)
                .HasForeignKey(r => r.RoomID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReservationForm>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.ReservationForms)
                .HasForeignKey(r => r.CustomerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoryCheckin>()
                .HasOne(h => h.ReservationForm)
                .WithOne(r => r.HistoryCheckin)
                .HasForeignKey<HistoryCheckin>(h => h.ReservationFormID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HistoryCheckOut>()
                .HasOne(h => h.ReservationForm)
                .WithOne(r => r.HistoryCheckOut)
                .HasForeignKey<HistoryCheckOut>(h => h.ReservationFormID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomUsageService>()
                .HasOne(r => r.HotelService)
                .WithMany(h => h.RoomUsageServices)
                .HasForeignKey(r => r.HotelServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomUsageService>()
                .HasOne(r => r.ReservationForm)
                .WithMany(rf => rf.RoomUsageServices)
                .HasForeignKey(r => r.ReservationFormID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.ReservationForm)
                .WithMany(r => r.Invoices)
                .HasForeignKey(i => i.ReservationFormID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomTask>()
                .HasOne(t => t.Room)
                .WithMany(r => r.RoomTasks)
                .HasForeignKey(t => t.RoomID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomTask>()
                .HasOne(t => t.CreatedByEmployee)
                .WithMany(e => e.CreatedRoomTasks)
                .HasForeignKey(t => t.CreatedByEmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomTask>()
                .HasOne(t => t.AssignedEmployee)
                .WithMany(e => e.AssignedRoomTasks)
                .HasForeignKey(t => t.AssignedEmployeeID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RoomTaskHistory>()
                .HasOne(h => h.RoomTask)
                .WithMany(t => t.Histories)
                .HasForeignKey(h => h.RoomTaskID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RoomTaskHistory>()
                .HasOne(h => h.ChangedByEmployee)
                .WithMany(e => e.RoomTaskHistories)
                .HasForeignKey(h => h.ChangedByEmployeeID)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure computed columns
            modelBuilder.Entity<RoomUsageService>()
                .Property(r => r.TotalPrice)
                .HasComputedColumnSql("[quantity] * [unitPrice]", stored: true);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.TotalDue)
                .HasComputedColumnSql("[roomCharge] + [servicesCharge]", stored: true);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.NetDue)
                .HasComputedColumnSql("([roomCharge] + [servicesCharge]) * 1.1", stored: true);
        }
    }
}
