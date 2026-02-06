using EventPlannerWebApplication.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace EventPlannerWebApplication.Data
{
    public class EventPlannerDbContext : DbContext
    {
        public EventPlannerDbContext(DbContextOptions<EventPlannerDbContext> options)
        : base(options)
        {
        }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<Participant> Participants => Set<Participant>();
        public DbSet<AvailabilityInterval> AvailabilityIntervals => Set<AvailabilityInterval>();
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Event>()
                .HasIndex(e => e.PublicCode)
                .IsUnique();

            modelBuilder.Entity<Event>()
                .HasIndex(e => e.OwnerCode)
                .IsUnique();

            modelBuilder.Entity<Participant>()
                .HasIndex(p => new { p.EventId, p.UserId })
                .IsUnique();

            modelBuilder.Entity<AvailabilityInterval>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_AvailabilityInterval_Time",
                    "\"StartTime\" < \"EndTime\""
                ));
            });
        }

    }
}
