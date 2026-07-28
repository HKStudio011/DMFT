using System.ComponentModel.DataAnnotations;

namespace DMFT.Entities;

public class DownloadSetting
{
    [Key]
    public string Id { get; set; } = "default";
    [MaxLength(1000)]
    public string DefaultPath { get; set; } = string.Empty;
}
