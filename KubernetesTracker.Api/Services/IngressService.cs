using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

public class IngressService : IIngressService
{
    private readonly ApplicationDbContext _context;

    public IngressService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<IngressResponseDto>> GetAllIngressesAsync()
    {
        var ingresses = await _context.Ingresses
            .Include(i => i.Cluster)
            .AsSplitQuery()
            .ToListAsync();

        return ingresses.Select(ToResponseDto);
    }

    public async Task<IEnumerable<IngressResponseDto>> GetIngressesByClusterAsync(string clusterName)
    {
        var ingresses = await _context.Ingresses
            .Include(i => i.Cluster)
            .AsSplitQuery()
            .Where(i => i.Cluster.ClusterName == clusterName)
            .ToListAsync();

        return ingresses.Select(ToResponseDto);
    }

    public async Task<IngressResponseDto?> GetIngressAsync(int id)
    {
        var ingress = await _context.Ingresses
            .Include(i => i.Cluster)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id);

        return ingress == null ? null : ToResponseDto(ingress);
    }

    public async Task<IngressResponseDto> CreateIngressAsync(IngressCreateDto ingressDto)
    {
        var cluster = await _context.Clusters
            .FirstOrDefaultAsync(c => c.ClusterName == ingressDto.ClusterName);

        if (cluster == null)
        {
            throw new NotFoundException($"Cluster '{ingressDto.ClusterName}' not found");
        }

        var existingIngress = await _context.Ingresses
            .AnyAsync(i => i.Cluster.Id == cluster.Id && 
                          i.Namespace == ingressDto.Namespace && 
                          i.IngressName == ingressDto.IngressName);

        if (existingIngress)
        {
            throw new DbUpdateException(
                $"Ingress '{ingressDto.IngressName}' already exists in namespace '{ingressDto.Namespace}'",
                new Exception("Unique constraint violation"));
        }

        var ingress = new Ingress
        {
            ClusterId = cluster.Id,
            Namespace = ingressDto.Namespace,
            IngressName = ingressDto.IngressName,
            Hosts = ingressDto.Hosts,
            Cluster = cluster
        };

        _context.Ingresses.Add(ingress);
        await _context.SaveChangesAsync();

        return ToResponseDto(ingress);
    }

    public async Task<IngressResponseDto> UpdateIngressAsync(int id, IngressCreateDto ingressDto)
    {
        var ingress = await _context.Ingresses
            .Include(i => i.Cluster)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (ingress == null)
        {
            throw new NotFoundException($"Ingress with ID {id} not found");
        }

        var cluster = await _context.Clusters
            .FirstOrDefaultAsync(c => c.ClusterName == ingressDto.ClusterName);

        if (cluster == null)
        {
            throw new NotFoundException($"Cluster '{ingressDto.ClusterName}' not found");
        }

        // Check if update would create a duplicate
        var existingIngress = await _context.Ingresses
            .AnyAsync(i => i.Id != id &&
                          i.Cluster.Id == cluster.Id && 
                          i.Namespace == ingressDto.Namespace && 
                          i.IngressName == ingressDto.IngressName);

        if (existingIngress)
        {
            throw new DbUpdateException(
                $"Ingress '{ingressDto.IngressName}' already exists in namespace '{ingressDto.Namespace}'",
                new Exception("Unique constraint violation"));
        }

        ingress.ClusterId = cluster.Id;
        ingress.Cluster = cluster;
        ingress.Namespace = ingressDto.Namespace;
        ingress.IngressName = ingressDto.IngressName;
        ingress.Hosts = ingressDto.Hosts;

        await _context.SaveChangesAsync();

        return ToResponseDto(ingress);
    }

    public async Task DeleteIngressAsync(int id)
    {
        var ingress = await _context.Ingresses.FindAsync(id);
        if (ingress == null)
        {
            throw new NotFoundException($"Ingress with ID {id} not found");
        }

        _context.Ingresses.Remove(ingress);
        await _context.SaveChangesAsync();
    }

    private static IngressResponseDto ToResponseDto(Ingress ingress) => new()
    {
        Id = ingress.Id,
        Namespace = ingress.Namespace,
        IngressName = ingress.IngressName,
        Hosts = ingress.Hosts,
        ClusterName = ingress.Cluster.ClusterName,
        CreatedAt = ingress.CreatedAt,
        UpdatedAt = ingress.UpdatedAt
    };
}
