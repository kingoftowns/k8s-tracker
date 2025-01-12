public interface IKubernetesService
{
    Task<IEnumerable<Service>> GetAllServicesAsync();
    Task<IEnumerable<Service>> GetServicesByClusterAsync(string clusterName);
    Task<Service?> GetServiceAsync(int id);
    Task<Service> CreateServiceAsync(ServiceCreateDto serviceDto);
    Task<Service> UpdateServiceAsync(int id, ServiceCreateDto serviceDto);
    Task DeleteServiceAsync(int id);
}