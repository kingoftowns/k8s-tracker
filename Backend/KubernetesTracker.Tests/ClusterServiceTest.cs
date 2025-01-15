using KubernetesTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace KubernetesTracker.Tests;

public class ClusterServiceTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly ApplicationDbContext _context;

    public ClusterServiceTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique database for each test
            .Options;

        _context = new ApplicationDbContext(_options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateCluster_RemovesDuplicateVersions()
    {
        // Arrange
        var service = new ClusterService(_context);
        var dto = new ClusterCreateDto
        {
            ClusterName = "test-cluster",
            ApiserverVersion = "1.0.0",
            KubeletVersions = new List<string> { "1.0.0", "1.0.0", "1.1.0" },
            KernelVersions = new List<string> { "5.0.0", "5.0.0", "5.1.0" }
        };

        // Act
        var result = await service.CreateClusterAsync(dto);

        // Assert
        Assert.Equal(2, result.KubeletVersions.Count);
        Assert.Equal(2, result.KernelVersions.Count);
        Assert.Contains("1.0.0", result.KubeletVersions);
        Assert.Contains("1.1.0", result.KubeletVersions);
    }

    [Fact]
    public async Task GetClusterByName_ReturnsCorrectCluster()
    {
        // Arrange
        var service = new ClusterService(_context);
        var cluster = new Cluster
        {
            ClusterName = "test-cluster",
            ApiserverVersion = "1.0.0",
            KubeletVersions = new List<string> { "1.0.0" },
            KernelVersions = new List<string> { "5.0.0" }
        };

        _context.Clusters.Add(cluster);
        await _context.SaveChangesAsync();

        // Act
        var result = await service.GetClusterByNameAsync("test-cluster");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-cluster", result.ClusterName);
    }

    [Fact]
    public async Task CreateCluster_EnforceUniqueClusterName()
    {
        // Arrange
        var service = new ClusterService(_context);
        
        // First cluster
        await service.CreateClusterAsync(new ClusterCreateDto
        {
            ClusterName = "test-cluster",
            ApiserverVersion = "1.0.0",
            KubeletVersions = new List<string> { "1.0.0" },
            KernelVersions = new List<string> { "5.0.0" }
        });

        // Second cluster with same name
        var duplicateDto = new ClusterCreateDto
        {
            ClusterName = "test-cluster", // Same name
            ApiserverVersion = "2.0.0",   // Different version
            KubeletVersions = new List<string> { "2.0.0" },
            KernelVersions = new List<string> { "6.0.0" }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => 
            service.CreateClusterAsync(duplicateDto));
            
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task UpdateCluster_ThrowsNotFoundException_WhenClusterDoesNotExist()
    {
        // Arrange
        var service = new ClusterService(_context);
        var dto = new ClusterCreateDto
        {
            ClusterName = "test-cluster",
            ApiserverVersion = "1.0.0",
            KubeletVersions = new List<string> { "1.0.0" },
            KernelVersions = new List<string> { "5.0.0" }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await service.UpdateClusterAsync(999, dto)
        );
        
        Assert.Equal("Cluster with ID 999 not found", exception.Message);
    }
}