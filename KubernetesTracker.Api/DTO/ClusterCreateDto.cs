public class ClusterCreateDto
{
    public string ClusterName { get; set; } = null!;
    public string ApiserverVersion { get; set; } = null!;
    public List<string> KubeletVersions { get; set; } = new();
    public List<string> KernelVersions { get; set; } = new();
}