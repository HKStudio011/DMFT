using System.ComponentModel.DataAnnotations;

namespace DMFT.Entities;

public class AppSetting
{
    [Key]
    public string Id { get; set; } = string.Empty;
    [MaxLength(4000)]
    public string Value { get; set; } = string.Empty;
}
