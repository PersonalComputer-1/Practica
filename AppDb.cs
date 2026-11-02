using Microsoft.EntityFrameworkCore;

public class AppDb : DbContext {
    public AppDb(DbContextOptions<AppDb> o) : base(o) {}

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentItem> EquipmentItems => Set<EquipmentItem>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();
    public DbSet<Penalty> Penalties => Set<Penalty>();
    public DbSet<WarehouseLog> WarehouseLogs => Set<WarehouseLog>();

    protected override void OnModelCreating(ModelBuilder m) {
        m.Entity<User>().HasIndex(u => u.Email).IsUnique();
        m.Entity<Equipment>().Property(e => e.PricePerDay).HasPrecision(10, 2);
        m.Entity<Booking>().Property(b => b.TotalPrice).HasPrecision(10, 2);
        m.Entity<Penalty>().Property(p => p.Amount).HasPrecision(10, 2);
        m.Entity<Booking>().HasIndex(b => new { b.StartDate, b.EndDate, b.Status });
    }
}