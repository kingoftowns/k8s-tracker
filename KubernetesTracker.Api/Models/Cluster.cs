public class Cluster : BaseEntity
{
    public int Id { get; set; }
    public string ClusterName { get; set; } = null!;
    public string ApiserverVersion { get; set; } = null!;
    public string KubeletVersion { get; set; } = null!;
    public string KernelVersion { get; set; } = null!;

    public ICollection<Ingress> Ingresses { get; set; } = new List<Ingress>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
}