using Microsoft.EntityFrameworkCore;
using TPLinkWebUI.Models.History;

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
        }
    }
}