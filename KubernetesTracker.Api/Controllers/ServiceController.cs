using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KubernetesTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class serviceController : ControllerBase
{
    private readonly IKubernetesService _kubernetesService;

    public serviceController(IKubernetesService kubernetesService)
    {
        _kubernetesService = kubernetesService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceResponseDto>>> GetServices()
    {
        var services = await _kubernetesService.GetAllServicesAsync();
        return Ok(services);
    }

    [HttpGet("cluster/{clusterName}")]
    public async Task<ActionResult<IEnumerable<ServiceResponseDto>>> GetServicesByCluster(string clusterName)
    {
        var services = await _kubernetesService.GetServicesByClusterAsync(clusterName);
        return Ok(services);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResponseDto>> GetService(int id)
    {
        var service = await _kubernetesService.GetServiceAsync(id);
        if (service == null)
        {
            return NotFound($"Service with ID {id} not found");
        }

        return Ok(service);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceResponseDto>> CreateService(ServiceCreateDto serviceDto)
    {
        try
        {
            var service = await _kubernetesService.CreateServiceAsync(serviceDto);
            return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
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
    public async Task<IActionResult> UpdateService(int id, ServiceCreateDto serviceDto)
    {
        try
        {
            var service = await _kubernetesService.UpdateServiceAsync(id, serviceDto);
            return Ok(service);
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
    public async Task<IActionResult> DeleteService(int id)
    {
        try
        {
            await _kubernetesService.DeleteServiceAsync(id);
            return Ok();
        }
        catch (NotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}