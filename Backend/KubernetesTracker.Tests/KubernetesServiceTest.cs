using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace KubernetesTracker.Tests;

public class KubernetesServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly ApplicationDbContext _context;

    public KubernetesServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<Cluster> CreateTestCluster()
    {
        var cluster = new Cluster
        {
            ClusterName = "test-cluster",
            ApiserverVersion = "1.0.0",
            KubeletVersions = new List<string> { "1.0.0" },
            KernelVersions = new List<string> { "5.0.0" }
        };

        _context.Clusters.Add(cluster);
        await _context.SaveChangesAsync();
        return cluster;
    }

    [Fact]
    public async Task CreateService_Success()
    {
        // Arrange
        var service = new KubernetesService(_context);
        await CreateTestCluster();

        var dto = new ServiceCreateDto
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            ServiceName = "test-service",
            ExternalIp = "10.0.0.1",
            Ports = new List<int> { 80, 443 }
        };

        // Act
        var result = await service.CreateServiceAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-service", result.ServiceName);
        Assert.Equal("default", result.Namespace);
        Assert.Equal("10.0.0.1", result.ExternalIp);
        Assert.Equal(2, result.Ports.Count);
        Assert.Contains(80, result.Ports);
        Assert.Contains(443, result.Ports);
    }

    [Fact]
    public async Task CreateService_ThrowsNotFoundException_WhenClusterNotFound()
    {
        // Arrange
        var service = new KubernetesService(_context);
        var dto = new ServiceCreateDto
        {
            ClusterName = "non-existent-cluster",
            Namespace = "default",
            ServiceName = "test-service",
            Ports = new List<int> { 80 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateServiceAsync(dto));
    }

    [Fact]
    public async Task CreateService_ThrowsDbUpdateException_WhenDuplicate()
    {
        // Arrange
        var service = new KubernetesService(_context);
        await CreateTestCluster();

        var dto = new ServiceCreateDto
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            ServiceName = "test-service",
            Ports = new List<int> { 80 }
        };

        // First creation
        await service.CreateServiceAsync(dto);

        // Act & Assert - Second creation should fail
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.CreateServiceAsync(dto));
    }

    [Fact]
    public async Task GetServicesByCluster_ReturnsCorrectServices()
    {
        // Arrange
        var service = new KubernetesService(_context);
        var cluster = await CreateTestCluster();

        var svc1 = new Service
        {
            ClusterId = cluster.Id,
            Namespace = "default",
            ServiceName = "service-1",
            Ports = new List<int> { 80 }
        };

        var svc2 = new Service
        {
            ClusterId = cluster.Id,
            Namespace = "default",
            ServiceName = "service-2",
            Ports = new List<int> { 443 }
        };

        _context.Services.AddRange(svc1, svc2);
        await _context.SaveChangesAsync();

        // Act
        var results = await service.GetServicesByClusterAsync("test-cluster");

        // Assert
        Assert.Equal(2, results.Count());
    }
}