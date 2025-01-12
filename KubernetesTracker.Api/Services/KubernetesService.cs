using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

public class KubernetesService : IKubernetesService
{
    private readonly ApplicationDbContext _context;

    public KubernetesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Service>> GetAllServicesAsync()
    {
        return await _context.Services
            .Include(s => s.Cluster)
            .ToListAsync();
    }

    public async Task<IEnumerable<Service>> GetServicesByClusterAsync(string clusterName)
    {
        return await _context.Services
            .Include(s => s.Cluster)
            .Where(s => s.Cluster.ClusterName == clusterName)
            .ToListAsync();
    }

    public async Task<Service?> GetServiceAsync(int id)
    {
        return await _context.Services
            .Include(s => s.Cluster)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Service> CreateServiceAsync(ServiceCreateDto serviceDto)
    {
        var cluster = await _context.Clusters
            .FirstOrDefaultAsync(c => c.ClusterName == serviceDto.ClusterName);

        if (cluster == null)
        {
            throw new NotFoundException($"Cluster '{serviceDto.ClusterName}' not found");
        }

        // Check for duplicate service in the same namespace
        var existingService = await _context.Services
            .AnyAsync(s => s.Cluster.Id == cluster.Id && 
                          s.Namespace == serviceDto.Namespace && 
                          s.ServiceName == serviceDto.ServiceName);

        if (existingService)
        {
            throw new DbUpdateException(
                $"Service '{serviceDto.ServiceName}' already exists in namespace '{serviceDto.Namespace}'",
                new Exception("Unique constraint violation"));
        }

        var service = new Service
        {
            ClusterId = cluster.Id,
            Namespace = serviceDto.Namespace,
            ServiceName = serviceDto.ServiceName,
            ExternalIp = serviceDto.ExternalIp,
            Ports = serviceDto.Ports
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return service;
    }

    public async Task<Service> UpdateServiceAsync(int id, ServiceCreateDto serviceDto)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            throw new NotFoundException($"Service with ID {id} not found");
        }

        var cluster = await _context.Clusters
            .FirstOrDefaultAsync(c => c.ClusterName == serviceDto.ClusterName);

        if (cluster == null)
        {
            throw new NotFoundException($"Cluster '{serviceDto.ClusterName}' not found");
        }

        service.ClusterId = cluster.Id;
        service.Namespace = serviceDto.Namespace;
        service.ServiceName = serviceDto.ServiceName;
        service.ExternalIp = serviceDto.ExternalIp;
        service.Ports = serviceDto.Ports;

        await _context.SaveChangesAsync();

        return service;
    }

    public async Task DeleteServiceAsync(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            throw new NotFoundException($"Service with ID {id} not found");
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
    }
}