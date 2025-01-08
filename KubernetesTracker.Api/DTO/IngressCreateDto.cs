public class IngressCreateDto
{
    public string ClusterName { get; set; } = null!;
    public string Namespace { get; set; } = null!;
    public string IngressName { get; set; } = null!;
    public List<string> Hosts { get; set; } = new();
}