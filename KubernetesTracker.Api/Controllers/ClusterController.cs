using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KubernetesTracker.Api.Data;

namespace KubernetesTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class clustersController : ControllerBase
{
    private readonly IClusterService _clusterService;

    public clustersController(IClusterService clusterService)
    {
        _clusterService = clusterService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cluster>>> GetClusters()
    {
        var clusters = await _clusterService.GetAllClustersAsync();
        return Ok(clusters);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Cluster>> GetCluster(int id)
    {
        var cluster = await _clusterService.GetClusterByIdAsync(id);
        if (cluster == null)
        {
            return NotFound();
        }

        return cluster;
    }

    [HttpGet("name/{name}")]
    public async Task<ActionResult<Cluster>> GetClusterByName(string name)
    {
        var cluster = await _clusterService.GetClusterByNameAsync(name);
        if (cluster == null)
        {
            return NotFound();
        }

        return cluster;
    }

    [HttpPost]
    public async Task<ActionResult<Cluster>> CreateCluster(ClusterCreateDto clusterDto)
    {
        try
        {
            var cluster = await _clusterService.CreateClusterAsync(clusterDto);
            return CreatedAtAction(
                nameof(GetCluster),
                new { id = cluster.Id },
                cluster);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique constraint") == true)
        {
            return Conflict($"A cluster with name '{clusterDto.ClusterName}' already exists");
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCluster(int id, ClusterCreateDto clusterDto)
    {
        try
        {
            var cluster = await _clusterService.UpdateClusterAsync(id, clusterDto);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique constraint") == true)
        {
            return Conflict($"A cluster with name '{clusterDto.ClusterName}' already exists");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCluster(int id)
    {
        try
        {
            await _clusterService.DeleteClusterAsync(id);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
