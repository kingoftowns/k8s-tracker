using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KubernetesTracker.Api.Data;

namespace KubernetesTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ingressController : ControllerBase
{
    private readonly IIngressService _ingressService;

    public ingressController(IIngressService ingressService)
    {
        _ingressService = ingressService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ingress>>> GetIngresses()
    {
        var ingresses = await _ingressService.GetAllIngressesAsync();
        return Ok(ingresses);
    }

    [HttpGet("cluster/{clusterName}")]
    public async Task<ActionResult<IEnumerable<Ingress>>> GetIngressesByCluster(string clusterName)
    {
        var ingresses = await _ingressService.GetIngressesByClusterAsync(clusterName);
        return Ok(ingresses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Ingress>> GetIngress(int id)
    {
        var ingress = await _ingressService.GetIngressAsync(id);
        if (ingress == null)
        {
            return NotFound();
        }

        return ingress;
    }

    [HttpPost]
    public async Task<ActionResult<Ingress>> CreateIngress(IngressCreateDto ingressDto)
    {
        try
        {
            var ingress = await _ingressService.CreateIngressAsync(ingressDto);
            return CreatedAtAction(nameof(GetIngress), new { id = ingress.Id }, ingress);
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

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateIngress(int id, IngressCreateDto ingressDto)
    {
        try
        {
            await _ingressService.UpdateIngressAsync(id, ingressDto);
            return NoContent();
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
    public async Task<IActionResult> DeleteIngress(int id)
    {
        try
        {
            await _ingressService.DeleteIngressAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}