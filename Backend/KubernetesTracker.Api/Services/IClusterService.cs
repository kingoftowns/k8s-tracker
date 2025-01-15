public interface IClusterService
{
    Task<IEnumerable<ClusterResponseDto>> GetAllClustersAsync();
    Task<ClusterResponseDto?> GetClusterByIdAsync(int id);
    Task<ClusterResponseDto?> GetClusterByNameAsync(string name);
    Task<ClusterResponseDto> CreateClusterAsync(ClusterCreateDto clusterDto);
    Task<ClusterResponseDto> UpdateClusterAsync(int id, ClusterCreateDto clusterDto);
    Task DeleteClusterAsync(int id);
}