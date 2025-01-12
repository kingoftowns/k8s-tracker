using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

public class IngressService : IIngressService
{
    private readonly ApplicationDbContext _context;

    public IngressService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Ingress>> GetAllIngressesAsync()
    {
        return await _context.Ingresses
            .Include(i => i.Cluster)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ingress>> GetIngressesByClusterAsync(string clusterName)
    {
        return await _context.Ingresses
            .Include(i => i.Cluster)
            .Where(i => i.Cluster.ClusterName == clusterName)
            .ToListAsync();
    }

    public async Task<Ingress?> GetIngressAsync(int id)
    {
        return await _context.Ingresses
            .Include(i => i.Cluster)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Ingress> CreateIngressAsync(IngressCreateDto ingressDto)
    {
        var cluster = await _context.Clusters
            .FirstOrDefaultAsync(c => c.ClusterName == ingressDto.ClusterName);

        if (cluster == null)
        {
            throw new NotFoundException($"Cluster '{ingressDto.ClusterName}' not found");
        }

        // Check for duplicate ingress in the same namespace
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
            Hosts = ingressDto.Hosts
        };

        _context.Ingresses.Add(ingress);
        await _context.SaveChangesAsync();

        return ingress;
    }

    public async Task<Ingress> UpdateIngressAsync(int id, IngressCreateDto ingressDto)
    {
        var ingress = await _context.Ingresses.FindAsync(id);
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

        ingress.ClusterId = cluster.Id;
        ingress.Namespace = ingressDto.Namespace;
        ingress.IngressName = ingressDto.IngressName;
        ingress.Hosts = ingressDto.Hosts;

        await _context.SaveChangesAsync();

        return ingress;
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
}