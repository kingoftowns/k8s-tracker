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
            modelBuilder.Entity<Cluster>()
                .HasIndex(c => c.ClusterName)
                .IsUnique();

            modelBuilder.Entity<Ingress>()
                .HasIndex(i => new { i.ClusterId, i.Namespace, i.IngressName })
                .IsUnique();

            modelBuilder.Entity<Service>()
                .HasIndex(s => new { s.ClusterId, s.Namespace, s.ServiceName })
                .IsUnique();

            modelBuilder.Entity<Ingress>()
                .Property(i => i.Hosts)
                .HasColumnType("text[]");

            modelBuilder.Entity<Service>()
                .Property(s => s.Ports)
                .HasColumnType("integer[]");
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity && e.State is EntityState.Added or EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    ((BaseEntity)entityEntry.Entity).CreatedAt = DateTime.UtcNow;
                }

                ((BaseEntity)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}