using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TargetRsiOrderInstruction> TargetRsiOrderInstructions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TargetRsiOrderInstruction>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();
    }
}