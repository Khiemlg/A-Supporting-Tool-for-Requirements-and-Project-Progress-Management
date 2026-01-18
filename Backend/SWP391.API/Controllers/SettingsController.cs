using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWP391.Domain.Entities;
using SWP391.Infrastructure.Data;

namespace SWP391.API.Controllers;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    public record IntegrationSettingsDto(
        string? GitHubToken,
        string? JiraBaseUrl,
        string? JiraEmail,
        string? JiraApiToken
    );

    public record SaveIntegrationSettingsRequest(
        string? GitHubToken,
        string? JiraBaseUrl,
        string? JiraEmail,
        string? JiraApiToken
    );

    /// <summary>
    /// Get current integration settings (tokens are masked)
    /// </summary>
    [HttpGet("integration")]
    public async Task<IActionResult> GetIntegrationSettings()
    {
        var settings = await _context.IntegrationSettings.ToListAsync();
        
        string? GetValue(string key) => settings.FirstOrDefault(s => s.Key == key)?.Value;
        string? MaskToken(string? value) => string.IsNullOrEmpty(value) ? null 
            : value.Length > 8 ? $"{value[..4]}...{value[^4..]}" : "****";

        return Ok(new IntegrationSettingsDto(
            GitHubToken: MaskToken(GetValue("GitHub:PAT")),
            JiraBaseUrl: GetValue("Jira:BaseUrl"),
            JiraEmail: GetValue("Jira:Email"),
            JiraApiToken: MaskToken(GetValue("Jira:ApiToken"))
        ));
    }

    /// <summary>
    /// Save integration settings
    /// </summary>
    [HttpPost("integration")]
    public async Task<IActionResult> SaveIntegrationSettings([FromBody] SaveIntegrationSettingsRequest request)
    {
        await SaveSetting("GitHub:PAT", request.GitHubToken, "GitHub Personal Access Token");
        await SaveSetting("Jira:BaseUrl", request.JiraBaseUrl, "Jira Base URL");
        await SaveSetting("Jira:Email", request.JiraEmail, "Jira Account Email");
        await SaveSetting("Jira:ApiToken", request.JiraApiToken, "Jira API Token");

        await _context.SaveChangesAsync();

        return Ok(new { message = "Settings saved successfully" });
    }

    private async Task SaveSetting(string key, string? value, string description)
    {
        if (string.IsNullOrEmpty(value)) return;

        var setting = await _context.IntegrationSettings.FirstOrDefaultAsync(s => s.Key == key);
        
        if (setting == null)
        {
            setting = new IntegrationSetting
            {
                Key = key,
                Value = value,
                Description = description,
                UpdatedAt = DateTime.UtcNow
            };
            _context.IntegrationSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Check if integration is configured
    /// </summary>
    [HttpGet("integration/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetIntegrationStatus()
    {
        var settings = await _context.IntegrationSettings.ToListAsync();
        
        bool HasValue(string key) => settings.Any(s => s.Key == key && !string.IsNullOrEmpty(s.Value));

        return Ok(new
        {
            gitHubConfigured = HasValue("GitHub:PAT"),
            jiraConfigured = HasValue("Jira:BaseUrl") && HasValue("Jira:Email") && HasValue("Jira:ApiToken")
        });
    }
}
