public class Ingress : BaseEntity
{
    public int Id { get; set; }
    public int ClusterId { get; set; }
    public string Namespace { get; set; } = null!;
    public string IngressName { get; set; } = null!;
    public List<string> Hosts { get; set; } = new();

    public Cluster Cluster { get; set; } = null!;
}