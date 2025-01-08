public class ClusterCreateDto
{
    public string ClusterName { get; set; } = null!;
    public string ApiserverVersion { get; set; } = null!;
    public string KubeletVersion { get; set; } = null!;
    public string KernelVersion { get; set; } = null!;
}