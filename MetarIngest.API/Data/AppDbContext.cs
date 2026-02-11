/*********************************************************************************
* METAR Ingest API - Database Context
* This class represents the database context for the METAR Ingest API. It is responsible for managing the connection 
* to the database and providing access to the Observations table.
*********************************************************************************/

using Microsoft.EntityFrameworkCore;

public class AppDbContext: DbContext
{
    public DbSet<Observation> Observations { get; set; } // DbSet representing the Observations table in the database

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

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
