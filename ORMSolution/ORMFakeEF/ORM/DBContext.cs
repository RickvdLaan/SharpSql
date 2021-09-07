using Microsoft.EntityFrameworkCore;
using ORMFakeEF;

public class ORMDBContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Organisation> Organisations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            @"Server=localhost; Database=Northwind; Trusted_Connection=True; MultipleActiveResultSets=true");
    }
}