public class ServiceCreateDto
{
    public string ClusterName { get; set; } = null!;
    public string Namespace { get; set; } = null!;
    public string ServiceName { get; set; } = null!;
    public string? ExternalIp { get; set; }
    public List<int> Ports { get; set; } = new();
}

public class ServiceResponseDto : BaseEntity
{
    public int Id { get; set; }
    public string Namespace { get; set; } = null!;
    public string ServiceName { get; set; } = null!;
    public string? ExternalIp { get; set; }
    public List<int> Ports { get; set; } = new();
    public string ClusterName { get; set; } = null!;
}