using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Core.Models;

namespace SmartQueue.Core.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Queue> Queues { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Counter> Counters { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<QueueStatSnapshot> QueueStatSnapshots { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── Queue -Red
            builder.Entity<Queue>(entity =>
            {
                entity.HasKey(q => q.Id);
                entity.Property(q => q.Name).IsRequired().HasMaxLength(100);
                entity.Property(q => q.Description).HasMaxLength(500);
                entity.Property(q => q.Status).HasConversion<string>();
            });

            // ── ticket - Listic
            builder.Entity<Ticket>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Status).HasConversion<string>();

                entity.HasOne(t => t.Queue)
                      .WithMany(q => q.Tickets)
                      .HasForeignKey(t => t.QueueId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.Counter)
                      .WithMany(c => c.Tickets)
                      .HasForeignKey(t => t.CounterId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(t => t.User)
                      .WithMany(u => u.Tickets)
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── counter salter
            builder.Entity<Counter>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Status).HasConversion<string>();

                entity.HasOne(c => c.Queue)
                      .WithMany(q => q.Counters)
                      .HasForeignKey(c => c.QueueId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.AssignedUser)
                      .WithMany()
                      .HasForeignKey(c => c.AssignedUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── RefreshToken
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── QueueStatSnapshot 
            builder.Entity<QueueStatSnapshot>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.HasOne(s => s.Queue)
                      .WithMany(q => q.StatSnapshots)
                      .HasForeignKey(s => s.QueueId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(s => new { s.QueueId, s.DayOfWeek, s.HourOfDay })
                      .IsUnique();
            });
        }
    }
}

/*What this does: AppDbContext is the bridge between your C# models and PostgreSQL.
 * Each DbSet<> maps a model class to a database table. 
 * The OnModelCreating method configures relationships, constraints and delete behaviors 
 * so EF Core knows exactly how tables relate to each other.
Why QueueStatSnapshot has a unique index on {QueueId, DayOfWeek, HourOfDay}:
Each queue can only have ONE stat record per day+hour combination
— e.g. one record for "Queue 1, Monday, 9am". 
This prevents duplicates and makes lookups fast.

Why HasConversion<string>() on enums: Stores enum values as readable strings
in PostgreSQL ("Active", "Waiting" etc.) instead of integers 
— makes the database readable without needing to decode numbers.
Why DeleteBehavior.Cascade vs SetNull: Cascade means if a Queue is deleted
, all its Tickets and Counters are deleted too. SetNull means if a Counter is deleted,
the ticket's CounterId becomes null instead of deleting the ticket — preserving history.*/