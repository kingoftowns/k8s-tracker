public class ClusterCreateDto
{
    public string ClusterName { get; set; } = null!;
    public string ApiserverVersion { get; set; } = null!;
    public List<string> KubeletVersions { get; set; } = new();
    public List<string> KernelVersions { get; set; } = new();
}

public class ClusterResponseDto
{
    public int Id { get; set; }
    public string ClusterName { get; set; } = null!;
    public string ApiserverVersion { get; set; } = null!;
    public List<string> KubeletVersions { get; set; } = new();
    public List<string> KernelVersions { get; set; } = new();
    public List<IngressResponseDto> Ingresses { get; set; } = new();
    public List<ServiceResponseDto> Services { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}