public interface IClusterService
{
    Task<IEnumerable<Cluster>> GetAllClustersAsync();
    Task<Cluster?> GetClusterByIdAsync(int id);
    Task<Cluster?> GetClusterByNameAsync(string name);
    Task<Cluster> CreateClusterAsync(ClusterCreateDto clusterDto);
    Task<Cluster> UpdateClusterAsync(int id, ClusterCreateDto clusterDto);
    Task DeleteClusterAsync(int id);
}