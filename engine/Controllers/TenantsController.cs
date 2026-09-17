using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ComplianceEngine.Data;
using ComplianceEngine.Models;

namespace ComplianceEngine.Controllers;

[ApiController]
[Route("tenants")]
public class TenantsController : ControllerBase
{
    private readonly ComplianceDbContext _db;

    public TenantsController(ComplianceDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        try
        {
            // Generate random API key
            var rawApiKey = GenerateApiKey();
            var apiKeyHash = ComputeSha256Hash(rawApiKey);

            // Create tenant
            var tenant = new Tenant
            {
                Name = request.Name,
                ApiKeyHash = apiKeyHash,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                tenantId = tenant.Id,
                apiKey = rawApiKey  // Only shown once!
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to create tenant", details = ex.Message });
        }
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        try
        {
            var tenantCount = await _db.Tenants.CountAsync();
            return Ok(new 
            { 
                status = "healthy",
                tenantCount = tenantCount,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Service unhealthy", details = ex.Message });
        }
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile([FromHeader] string tenantId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return BadRequest(new { error = "Tenant ID header required" });
            }

            var tenant = await _db.Tenants.FindAsync(int.Parse(tenantId));
            if (tenant == null)
            {
                return NotFound(new { error = "Tenant not found" });
            }

            return Ok(new
            {
                id = tenant.Id,
                name = tenant.Name,
                isActive = tenant.IsActive,
                createdAt = tenant.CreatedAt,
                tenantId = tenant.Id.ToString()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to get profile", details = ex.Message });
        }
    }

    private string GenerateApiKey()
    {
        // Generate a secure random API key (32 characters)
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random(BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4)));
        return new string(Enumerable.Repeat(chars, 32)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string ComputeSha256Hash(string rawData)
    {
        using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}

public class CreateTenantRequest
{
    public string Name { get; set; } = "";
}