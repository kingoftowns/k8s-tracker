public interface IIngressService
{
    Task<IEnumerable<IngressResponseDto>> GetAllIngressesAsync();
    Task<IEnumerable<IngressResponseDto>> GetIngressesByClusterAsync(string clusterName);
    Task<IngressResponseDto?> GetIngressAsync(int id);
    Task<IngressResponseDto> CreateIngressAsync(IngressCreateDto ingressDto);
    Task<IngressResponseDto> UpdateIngressAsync(int id, IngressCreateDto ingressDto);
    Task DeleteIngressAsync(int id);
}