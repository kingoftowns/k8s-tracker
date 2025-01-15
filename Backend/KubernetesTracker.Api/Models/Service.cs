public class Service : BaseEntity
{
    public int Id { get; set; }
    public int ClusterId { get; set; }
    public string Namespace { get; set; } = null!;
    public string ServiceName { get; set; } = null!;
    public string? ExternalIp { get; set; }
    public List<int> Ports { get; set; } = new();

    public Cluster Cluster { get; set; } = null!;
}