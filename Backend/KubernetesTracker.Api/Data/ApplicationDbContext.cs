using Microsoft.EntityFrameworkCore;

namespace KubernetesTracker.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            Clusters = Set<Cluster>();
            Ingresses = Set<Ingress>();
            Services = Set<Service>();
        }

        public DbSet<Cluster> Clusters { get; set; } = null!;
        public DbSet<Ingress> Ingresses { get; set; } = null!;
        public DbSet<Service> Services { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Cluster>(entity =>
            {
                entity.HasIndex(c => c.ClusterName)
                    .IsUnique();

                entity.Property(c => c.KubeletVersions)
                    .HasColumnType("text[]");

                entity.Property(c => c.KernelVersions)
                    .HasColumnType("text[]");
            });

            modelBuilder.Entity<Ingress>(entity =>
            {
                entity.HasIndex(i => new { i.ClusterId, i.Namespace, i.IngressName })
                    .IsUnique();

                entity.Property(i => i.Hosts)
                    .HasColumnType("text[]");

                entity.Property(i => i.Ports)
                    .HasColumnType("integer[]");
            });

            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasIndex(s => new { s.ClusterId, s.Namespace, s.ServiceName })
                    .IsUnique();

                entity.Property(s => s.Ports)
                    .HasColumnType("integer[]");
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (BaseEntity)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }

                entity.UpdatedAt = DateTime.UtcNow;
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}