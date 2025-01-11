using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KubernetesTracker.Api.Data;

namespace KubernetesTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class clustersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public clustersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cluster>>> GetClusters()
    {
        return await _context.Clusters
            .Include(c => c.Ingresses)
            .Include(c => c.Services)
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Cluster>> GetCluster(int id)
    {
        var cluster = await _context.Clusters
            .Include(c => c.Ingresses)
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cluster == null)
        {
            return NotFound();
        }

        return cluster;
    }

    [HttpGet("name/{name}")]
    public async Task<ActionResult<Cluster>> GetClusterByName(string name)
    {
        var cluster = await _context.Clusters
            .Include(c => c.Ingresses)
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.ClusterName == name);

        if (cluster == null)
        {
            return NotFound($"Cluster '{name}' not found");
        }

        return cluster;
    }

[HttpPost]
public async Task<ActionResult<Cluster>> CreateCluster(ClusterCreateDto clusterDto)
{
    var cluster = new Cluster
    {
        ClusterName = clusterDto.ClusterName,
        ApiserverVersion = clusterDto.ApiserverVersion,
        KubeletVersions = clusterDto.KubeletVersions.Distinct().ToList(),
        KernelVersions = clusterDto.KernelVersions.Distinct().ToList(),
        Ingresses = new List<Ingress>(),
        Services = new List<Service>()
    };

    try
    {
        _context.Clusters.Add(cluster);
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique constraint") == true)
    {
        return Conflict($"A cluster with name '{clusterDto.ClusterName}' already exists");
    }

    return CreatedAtAction(
        nameof(GetCluster),
        new { id = cluster.Id },
        cluster);
}

[HttpPut("{id}")]
public async Task<IActionResult> UpdateCluster(int id, ClusterCreateDto clusterDto)
{
    var cluster = await _context.Clusters.FindAsync(id);
    
    if (cluster == null)
    {
        return NotFound();
    }

    cluster.ClusterName = clusterDto.ClusterName;
    cluster.ApiserverVersion = clusterDto.ApiserverVersion;
    cluster.KubeletVersions = clusterDto.KubeletVersions.Distinct().ToList();
    cluster.KernelVersions = clusterDto.KernelVersions.Distinct().ToList();

    try
    {
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        if (!ClusterExists(id))
        {
            return NotFound();
        }
        throw;
    }
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique constraint") == true)
    {
        return Conflict($"A cluster with name '{clusterDto.ClusterName}' already exists");
    }

    return NoContent();
}

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCluster(int id)
    {
        var cluster = await _context.Clusters.FindAsync(id);
        if (cluster == null)
        {
            return NotFound();
        }

        _context.Clusters.Remove(cluster);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ClusterExists(int id)
    {
        return _context.Clusters.Any(e => e.Id == id);
    }
}
