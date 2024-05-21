using Microsoft.EntityFrameworkCore;
using MVP_Server.Model.ENTITY;

namespace MVP_Server.DAL
{
    public class DataContext : DbContext
    {
        private const string _connectionString = @"Data Source='.\DATABASE\DB'";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReadingEntity>().HasMany<SensorDataEntity>(e => e.SensorData).WithOne(e => e.Reading).HasForeignKey(e => e.IdReading).HasPrincipalKey(e => e.Id);
            modelBuilder.Entity<SensorEntity>().HasMany<SensorDataEntity>(e => e.SensorData).WithOne(e => e.Sensor).HasForeignKey(e => e.IdSensor).HasPrincipalKey(e => e.Id);
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<SensorEntity> Sensors { get; set; }
        public DbSet<ReadingEntity> Readings { get; set; }
        public DbSet<SensorDataEntity> SensorData { get; set; }
    }
}
