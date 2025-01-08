using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KubernetesTracker.Api.Data;

namespace KubernetesTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ServiceController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Service>>> GetServices()
    {
        return await _context.Services
            .Include(s => s.Cluster)
            .ToListAsync();
    }

    [HttpGet("cluster/{clusterName}")]
    public async Task<ActionResult<IEnumerable<Service>>> GetServicesByCluster(string clusterName)
    {
        return await _context.Services
            .Include(s => s.Cluster)
            .Where(s => s.Cluster.ClusterName == clusterName)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Service>> CreateService(ServiceCreateDto serviceDto)
    {
        var cluster = await _context.Clusters
            .FirstOrDefaultAsync(c => c.ClusterName == serviceDto.ClusterName);

        if (cluster == null)
        {
            return NotFound($"Cluster '{serviceDto.ClusterName}' not found");
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
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique constraint") == true)
        {
            return Conflict($"A service with name '{serviceDto.ServiceName}' already exists in namespace '{serviceDto.Namespace}' for cluster '{serviceDto.ClusterName}'");
        }

        return CreatedAtAction(nameof(GetServices), new { id = service.Id }, service);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateService(int id, Service service)
    {
        if (id != service.Id)
        {
            return BadRequest();
        }

        _context.Entry(service).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ServiceExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ServiceExists(int id)
    {
        return _context.Services.Any(e => e.Id == id);
    }
}