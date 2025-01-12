public interface IIngressService
{
    Task<IEnumerable<Ingress>> GetAllIngressesAsync();
    Task<IEnumerable<Ingress>> GetIngressesByClusterAsync(string clusterName);
    Task<Ingress?> GetIngressAsync(int id);
    Task<Ingress> CreateIngressAsync(IngressCreateDto ingressDto);
    Task<Ingress> UpdateIngressAsync(int id, IngressCreateDto ingressDto);
    Task DeleteIngressAsync(int id);
}