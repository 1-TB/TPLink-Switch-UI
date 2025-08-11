using Microsoft.EntityFrameworkCore;
using TPLinkWebUI.Models.History;
using TPLinkWebUI.Models;

namespace TPLinkWebUI.Data
{
    public class SwitchHistoryContext : DbContext
    {
        public SwitchHistoryContext(DbContextOptions<SwitchHistoryContext> options) : base(options)
        {
        }

        public DbSet<PortHistoryEntry> PortHistory { get; set; }
        public DbSet<CableDiagnosticHistoryEntry> CableDiagnosticHistory { get; set; }
        public DbSet<SystemInfoHistoryEntry> SystemInfoHistory { get; set; }
        public DbSet<VlanHistoryEntry> VlanHistory { get; set; }
        public DbSet<SwitchConnectivityHistoryEntry> SwitchConnectivityHistory { get; set; }
        public DbSet<PortStatisticsHistoryEntry> PortStatisticsHistory { get; set; }
        public DbSet<UserActivityHistoryEntry> UserActivityHistory { get; set; }
        
        // User management
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Port History configuration
            modelBuilder.Entity<PortHistoryEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PortNumber);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ChangeType);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.PortNumber, e.Timestamp });
            });

            // Cable Diagnostic History configuration
            modelBuilder.Entity<CableDiagnosticHistoryEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PortNumber);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.TestTrigger);
                entity.HasIndex(e => new { e.PortNumber, e.Timestamp });
            });

            // System Info History configuration
            modelBuilder.Entity<SystemInfoHistoryEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ChangeType);
            });

            // VLAN History configuration
            modelBuilder.Entity<VlanHistoryEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.VlanId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ChangeType);
                entity.HasIndex(e => new { e.VlanId, e.Timestamp });
            });

            // Switch Connectivity History configuration
            modelBuilder.Entity<SwitchConnectivityHistoryEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.IsReachable);
                entity.HasIndex(e => e.IpAddress);
            });

            // Port Statistics History configuration
            modelBuilder.Entity<PortStatisticsHistoryEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.PortNumber);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ChangeType);
                entity.HasIndex(e => new { e.PortNumber, e.Timestamp });
            });

            // User Activity History configuration
            modelBuilder.Entity<UserActivityHistoryEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Username);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ActionType);
                entity.HasIndex(e => e.IsSuccess);
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.IsActive);
            });

            // User Session configuration
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SessionToken).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsActive);
                
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Sessions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}