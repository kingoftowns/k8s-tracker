public class Cluster : BaseEntity
{
    public int Id { get; set; }
    public string ClusterName { get; set; } = null!;
    public string ApiserverVersion { get; set; } = null!;
    public List<string> KubeletVersions { get; set; } = new();
    public List<string> KernelVersions { get; set; } = new();
    public ICollection<Ingress> Ingresses { get; set; } = new List<Ingress>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
}