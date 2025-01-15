using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task<ActionResult<IEnumerable<ClusterResponseDto>>> GetClusters()
    {
        var clusters = await _clusterService.GetAllClustersAsync();
        return Ok(clusters);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClusterResponseDto>> GetCluster(int id)
    {
        var cluster = await _clusterService.GetClusterByIdAsync(id);
        if (cluster == null)
        {
            return NotFound();
        }

        return Ok(cluster);
    }

    [HttpGet("name/{name}")]
    public async Task<ActionResult<ClusterResponseDto>> GetClusterByName(string name)
    {
        var cluster = await _clusterService.GetClusterByNameAsync(name);
        if (cluster == null)
        {
            return NotFound();
        }

        return Ok(cluster);
    }

    [HttpPost]
    public async Task<ActionResult<ClusterResponseDto>> CreateCluster(ClusterCreateDto clusterDto)
    {
        try
        {
            var cluster = await _clusterService.CreateClusterAsync(clusterDto);
            return CreatedAtAction(
                nameof(GetCluster),
                new { id = cluster.Id },
                cluster);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCluster(int id, ClusterCreateDto clusterDto)
    {
        try
        {
            var cluster = await _clusterService.UpdateClusterAsync(id, clusterDto);
            return Ok(cluster);
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCluster(int id)
    {
        try
        {
            await _clusterService.DeleteClusterAsync(id);
            return Ok();
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
