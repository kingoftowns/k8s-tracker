using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KubernetesTracker.Api.Data;

namespace KubernetesTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngressController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public IngressController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ingress>>> GetIngresses()
    {
        return await _context.Ingresses
            .Include(i => i.Cluster)
            .ToListAsync();
    }

    [HttpGet("cluster/{clusterName}")]
    public async Task<ActionResult<IEnumerable<Ingress>>> GetIngressesByCluster(string clusterName)
    {
        return await _context.Ingresses
            .Include(i => i.Cluster)
            .Where(i => i.Cluster.ClusterName == clusterName)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Ingress>> CreateIngress(IngressCreateDto ingressDto)
    {
        var cluster = await _context.Clusters
            .FirstOrDefaultAsync(c => c.ClusterName == ingressDto.ClusterName);

        if (cluster == null)
        {
            return NotFound($"Cluster '{ingressDto.ClusterName}' not found");
        }

        var ingress = new Ingress
        {
            ClusterId = cluster.Id,
            Namespace = ingressDto.Namespace,
            IngressName = ingressDto.IngressName,
            Hosts = ingressDto.Hosts
        };

        _context.Ingresses.Add(ingress);
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique constraint") == true)
        {
            return Conflict($"An ingress with name '{ingressDto.IngressName}' already exists in namespace '{ingressDto.Namespace}' for cluster '{ingressDto.ClusterName}'");
        }

        return CreatedAtAction(nameof(GetIngresses), new { id = ingress.Id }, ingress);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIngress(int id, Ingress ingress)
    {
        if (id != ingress.Id)
        {
            return BadRequest();
        }

        _context.Entry(ingress).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!IngressExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteIngress(int id)
    {
        var ingress = await _context.Ingresses.FindAsync(id);
        if (ingress == null)
        {
            return NotFound();
        }

        _context.Ingresses.Remove(ingress);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool IngressExists(int id)
    {
        return _context.Ingresses.Any(e => e.Id == id);
    }
}