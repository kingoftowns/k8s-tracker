using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

public class ClusterService : IClusterService
{
    private readonly ApplicationDbContext _context;

    public ClusterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClusterResponseDto>> GetAllClustersAsync()
    {
        var clusters = await _context.Clusters
            .Include(c => c.Ingresses)
            .Include(c => c.Services)
            .AsSplitQuery()
            .ToListAsync();

        return clusters.Select(ToResponseDto);
    }

    public async Task<ClusterResponseDto?> GetClusterByIdAsync(int id)
    {
        var cluster = await _context.Clusters
            .Include(c => c.Ingresses)
            .Include(c => c.Services)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id);

        return cluster == null ? null : ToResponseDto(cluster);
    }

    public async Task<ClusterResponseDto?> GetClusterByNameAsync(string name)
    {
        var cluster = await _context.Clusters
            .Include(c => c.Ingresses)
            .Include(c => c.Services)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.ClusterName == name);

        return cluster == null ? null : ToResponseDto(cluster);
    }

    public async Task<ClusterResponseDto> CreateClusterAsync(ClusterCreateDto clusterDto)
    {
        var existingCluster = await _context.Clusters
        .FirstOrDefaultAsync(c => c.ClusterName == clusterDto.ClusterName);

        if (existingCluster != null)
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
        
        return ToResponseDto(cluster);
    }

    public async Task<ClusterResponseDto> UpdateClusterAsync(int id, ClusterCreateDto clusterDto)
    {
        var cluster = await _context.Clusters
            .Include(c => c.Ingresses)
            .Include(c => c.Services)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (cluster == null)
        {
            throw new NotFoundException($"Cluster with ID {id} not found");
        }

        // Check if new name conflicts with existing cluster
        if (cluster.ClusterName != clusterDto.ClusterName)
        {
            var existingCluster = await _context.Clusters
                .FirstOrDefaultAsync(c => c.ClusterName == clusterDto.ClusterName);

            if (existingCluster != null)
            {
                throw new DbUpdateException(
                    "Violation of unique constraint", 
                    new Exception($"A cluster with name '{clusterDto.ClusterName}' already exists"));
            }
        }

        cluster.ClusterName = clusterDto.ClusterName;
        cluster.ApiserverVersion = clusterDto.ApiserverVersion;
        cluster.KubeletVersions = clusterDto.KubeletVersions.Distinct().ToList();
        cluster.KernelVersions = clusterDto.KernelVersions.Distinct().ToList();

        await _context.SaveChangesAsync();
        
        return ToResponseDto(cluster);
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

    private static ClusterResponseDto ToResponseDto(Cluster cluster) => new()
    {
        Id = cluster.Id,
        ClusterName = cluster.ClusterName,
        ApiserverVersion = cluster.ApiserverVersion,
        KubeletVersions = cluster.KubeletVersions,
        KernelVersions = cluster.KernelVersions,
        Ingresses = cluster.Ingresses.Select(i => new IngressResponseDto
        {
            Id = i.Id,
            Namespace = i.Namespace,
            IngressName = i.IngressName,
            Hosts = i.Hosts,
            ClusterName = cluster.ClusterName,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        }).ToList(),
        Services = cluster.Services.Select(s => new ServiceResponseDto
        {
            Id = s.Id,
            Namespace = s.Namespace,
            ServiceName = s.ServiceName,
            ExternalIp = s.ExternalIp,
            Ports = s.Ports,
            ClusterName = cluster.ClusterName,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }).ToList(),
        CreatedAt = cluster.CreatedAt,
        UpdatedAt = cluster.UpdatedAt
    };
}