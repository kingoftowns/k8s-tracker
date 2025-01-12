using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

public class ClusterService : IClusterService
{
    private readonly ApplicationDbContext _context;

    public ClusterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Cluster>> GetAllClustersAsync()
    {
        return await _context.Clusters
            .Include(c => c.Ingresses)
            .Include(c => c.Services)
            .ToListAsync();
    }

    public async Task<Cluster?> GetClusterByIdAsync(int id)
    {
        return await _context.Clusters
            .Include(c => c.Ingresses)
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cluster?> GetClusterByNameAsync(string name)
    {
        return await _context.Clusters
            .Include(c => c.Ingresses)
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.ClusterName == name);
    }

    public async Task<Cluster> CreateClusterAsync(ClusterCreateDto clusterDto)
    {
        if (await _context.Clusters.AnyAsync(c => c.ClusterName == clusterDto.ClusterName))
        {
            throw new DbUpdateException(
                $"A cluster with name '{clusterDto.ClusterName}' already exists",
                new Exception("Unique constraint violation"));
        }
        
        var cluster = new Cluster
        {
            ClusterName = clusterDto.ClusterName,
            ApiserverVersion = clusterDto.ApiserverVersion,
            KubeletVersions = clusterDto.KubeletVersions.Distinct().ToList(),
            KernelVersions = clusterDto.KernelVersions.Distinct().ToList(),
            Ingresses = new List<Ingress>(),
            Services = new List<Service>()
        };

        _context.Clusters.Add(cluster);
        await _context.SaveChangesAsync();
        
        return cluster;
    }

    public async Task<Cluster> UpdateClusterAsync(int id, ClusterCreateDto clusterDto)
    {
        var cluster = await _context.Clusters.FindAsync(id);
        if (cluster == null)
        {
            throw new NotFoundException($"Cluster with ID {id} not found");
        }

        cluster.ClusterName = clusterDto.ClusterName;
        cluster.ApiserverVersion = clusterDto.ApiserverVersion;
        cluster.KubeletVersions = clusterDto.KubeletVersions.Distinct().ToList();
        cluster.KernelVersions = clusterDto.KernelVersions.Distinct().ToList();

        await _context.SaveChangesAsync();
        
        return cluster;
    }

    public async Task DeleteClusterAsync(int id)
    {
        var cluster = await _context.Clusters.FindAsync(id);
        if (cluster == null)
        {
            throw new NotFoundException($"Cluster with ID {id} not found");
        }

        _context.Clusters.Remove(cluster);
        await _context.SaveChangesAsync();
    }
}