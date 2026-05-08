using Microsoft.EntityFrameworkCore;
using AuthDemo.Entities;

namespace AuthDemo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options)
    {
        
    }

    public DbSet<User> Users => Set<User>();
}