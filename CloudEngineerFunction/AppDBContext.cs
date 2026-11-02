using Microsoft.EntityFrameworkCore;

namespace CloudEngineerFunction;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Das wird unsere Tabelle in der Azure SQL Datenbank
    public DbSet<SupportTicket> Tickets { get; set; }
}