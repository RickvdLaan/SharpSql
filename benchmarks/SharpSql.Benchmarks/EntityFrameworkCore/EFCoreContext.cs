using EFCore;
using Microsoft.EntityFrameworkCore;

public class EFCoreContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=localhost; Database=Northwind; Trusted_Connection=True; MultipleActiveResultSets=true");
    }

    public DbSet<Order> Orders { get; set; }
}