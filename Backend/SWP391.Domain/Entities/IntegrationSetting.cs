namespace SWP391.Domain.Entities;

/// <summary>
/// Integration settings entity - stores global config like API tokens
/// </summary>
public class IntegrationSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;    // "GitHub:PAT", "Jira:Email", etc.
    public string Value { get; set; } = string.Empty;  // Stored value
    public string? Description { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
