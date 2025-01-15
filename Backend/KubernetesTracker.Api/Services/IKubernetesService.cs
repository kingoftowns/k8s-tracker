public interface IKubernetesService
{
    Task<IEnumerable<ServiceResponseDto>> GetAllServicesAsync();
    Task<IEnumerable<ServiceResponseDto>> GetServicesByClusterAsync(string clusterName);
    Task<ServiceResponseDto?> GetServiceAsync(int id);
    Task<ServiceResponseDto> CreateServiceAsync(ServiceCreateDto serviceDto);
    Task<ServiceResponseDto> UpdateServiceAsync(int id, ServiceCreateDto serviceDto);
    Task DeleteServiceAsync(int id);
}