
using Microsoft.EntityFrameworkCore;

/// <summary>
/// This class represents the database context for the METAR Ingest API. It is responsible for managing the connection 
/// to the database and providing access to the Observations table. It's based on Entity Framework Core and extends the DbContext class. 
/// </summary>
public class AppDbContext: DbContext
{
    public DbSet<Observation> Observations { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}

    /// <summary>
    /// This method applies the necessary configurations to the Observation entity.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure the entity.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the Observation entity
        modelBuilder.Entity<Observation>(entity =>
        {
            entity.HasKey(e => new { e.StationId, e.ObservationTime }); // Composite primary key on StationId and ObservationTime
            entity.Property(e => e.StationId).IsRequired().HasMaxLength(4); // ICAO code is 4 characters long
            entity.Property(e => e.ObservationTime).IsRequired();
            entity.Property(e => e.Temperature).IsRequired();
            entity.Property(e => e.RawMetar).IsRequired();
        });
    }
}
