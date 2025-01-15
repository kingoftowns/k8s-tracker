using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

public class KubernetesService : IKubernetesService
{
    private readonly ApplicationDbContext _context;

    public KubernetesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ServiceResponseDto>> GetAllServicesAsync()
    {
        var services = await _context.Services
            .Include(s => s.Cluster)
            .AsSplitQuery()
            .ToListAsync();

        return services.Select(ToResponseDto);
    }

    public async Task<IEnumerable<ServiceResponseDto>> GetServicesByClusterAsync(string clusterName)
    {
        var services = await _context.Services
            .Include(s => s.Cluster)
            .AsSplitQuery()
            .Where(s => s.Cluster.ClusterName == clusterName)
            .ToListAsync();

        return services.Select(ToResponseDto);
    }

    public async Task<ServiceResponseDto?> GetServiceAsync(int id)
    {
        var service = await _context.Services
            .Include(s => s.Cluster)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id);

        return service == null ? null : ToResponseDto(service);
    }

    public async Task<ServiceResponseDto> CreateServiceAsync(ServiceCreateDto serviceDto)
    {
        var cluster = await _context.Clusters
            .FirstOrDefaultAsync(c => c.ClusterName == serviceDto.ClusterName);

        if (cluster == null)
        {
            throw new NotFoundException($"Cluster '{serviceDto.ClusterName}' not found");
        }

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
            Ports = serviceDto.Ports,
            Cluster = cluster
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return ToResponseDto(service);
    }

    public async Task<ServiceResponseDto> UpdateServiceAsync(int id, ServiceCreateDto serviceDto)
    {
        var service = await _context.Services
            .Include(s => s.Cluster)
            .FirstOrDefaultAsync(s => s.Id == id);

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

        // Check if update would create a duplicate
        var existingService = await _context.Services
            .AnyAsync(s => s.Id != id &&
                          s.Cluster.Id == cluster.Id && 
                          s.Namespace == serviceDto.Namespace && 
                          s.ServiceName == serviceDto.ServiceName);

        if (existingService)
        {
            throw new DbUpdateException(
                $"Service '{serviceDto.ServiceName}' already exists in namespace '{serviceDto.Namespace}'",
                new Exception("Unique constraint violation"));
        }

        service.ClusterId = cluster.Id;
        service.Cluster = cluster;
        service.Namespace = serviceDto.Namespace;
        service.ServiceName = serviceDto.ServiceName;
        service.ExternalIp = serviceDto.ExternalIp;
        service.Ports = serviceDto.Ports;

        await _context.SaveChangesAsync();

        return ToResponseDto(service);
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

    private static ServiceResponseDto ToResponseDto(Service service) => new()
    {
        Id = service.Id,
        Namespace = service.Namespace,
        ServiceName = service.ServiceName,
        ExternalIp = service.ExternalIp,
        Ports = service.Ports,
        ClusterName = service.Cluster.ClusterName,
        CreatedAt = service.CreatedAt,
        UpdatedAt = service.UpdatedAt
    };
}