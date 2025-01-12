using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace KubernetesTracker.Tests;

public class IngressServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly ApplicationDbContext _context;

    public IngressServiceTests()
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
    public async Task CreateIngress_Success()
    {
        // Arrange
        var service = new IngressService(_context);
        await CreateTestCluster();

        var dto = new IngressCreateDto
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            IngressName = "test-ingress",
            Hosts = new List<string> { "test.example.com" }
        };

        // Act
        var result = await service.CreateIngressAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-ingress", result.IngressName);
        Assert.Equal("default", result.Namespace);
        Assert.Single(result.Hosts);
        Assert.Equal("test.example.com", result.Hosts[0]);
    }

    [Fact]
    public async Task CreateIngress_ThrowsNotFoundException_WhenClusterNotFound()
    {
        // Arrange
        var service = new IngressService(_context);
        var dto = new IngressCreateDto
        {
            ClusterName = "non-existent-cluster",
            Namespace = "default",
            IngressName = "test-ingress",
            Hosts = new List<string> { "test.example.com" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateIngressAsync(dto));
    }

    [Fact]
    public async Task CreateIngress_ThrowsDbUpdateException_WhenDuplicate()
    {
        // Arrange
        var service = new IngressService(_context);
        await CreateTestCluster();

        var dto = new IngressCreateDto
        {
            ClusterName = "test-cluster",
            Namespace = "default",
            IngressName = "test-ingress",
            Hosts = new List<string> { "test.example.com" }
        };

        // First creation
        await service.CreateIngressAsync(dto);

        // Act & Assert - Second creation should fail
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.CreateIngressAsync(dto));
    }

    [Fact]
    public async Task GetIngressesByCluster_ReturnsCorrectIngresses()
    {
        // Arrange
        var service = new IngressService(_context);
        var cluster = await CreateTestCluster();

        var ingress1 = new Ingress
        {
            ClusterId = cluster.Id,
            Namespace = "default",
            IngressName = "ingress-1",
            Hosts = new List<string> { "test1.example.com" }
        };

        var ingress2 = new Ingress
        {
            ClusterId = cluster.Id,
            Namespace = "default",
            IngressName = "ingress-2",
            Hosts = new List<string> { "test2.example.com" }
        };

        _context.Ingresses.AddRange(ingress1, ingress2);
        await _context.SaveChangesAsync();

        // Act
        var results = await service.GetIngressesByClusterAsync("test-cluster");

        // Assert
        Assert.Equal(2, results.Count());
    }
}
