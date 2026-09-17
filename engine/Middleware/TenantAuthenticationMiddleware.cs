using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using ComplianceEngine.Data;
using ComplianceEngine.Models;

namespace ComplianceEngine.Middleware;

public class TenantAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public TenantAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.Request.Path.Value?.ToLower();
        
        // Skip auth for tenant creation endpoint
        if (endpoint == "/tenants" && context.Request.Method == "POST")
        {
            await _next(context);
            return;
        }

        // Try to extract tenant from Authorization header
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var apiKey = authHeader.Substring("Bearer ".Length);
            
            // Resolve DbContext from the request scope
            using var scope = context.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ComplianceDbContext>();
            
            var tenant = await AuthenticateTenantAsync(db, apiKey);
            
            if (tenant != null)
            {
                context.Items["CurrentTenant"] = tenant;
                context.Response.Headers["X-Tenant-Id"] = tenant.Id.ToString();
            }
            else
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("{\"error\":\"Invalid or inactive API key\"}");
                return;
            }
        }
        else
        {
            // Allow unauthenticated access to health check and Swagger UI
            var allowedPublicEndpoints = new[] { "/health", "/", "/swagger", "/swagger/index.html", "/swagger/v1/swagger.json", "/swagger/swagger-ui-bundle.js", "/swagger/swagger-ui-standalone-preset.js", "/swagger/swagger-ui.css" };
            
            if (!allowedPublicEndpoints.Any(endpoint.EndsWith))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("{\"error\":\"API key required\"}");
                return;
            }
        }

        await _next(context);
    }

    private async Task<Tenant?> AuthenticateTenantAsync(ComplianceDbContext db, string apiKey)
    {
        try
        {
            // Hash the provided API key
            var apiKeyHash = ComputeSha256Hash(apiKey);
            
            // Find tenant by hash and active status
            var tenant = await db.Tenants
                .FirstOrDefaultAsync(t => t.ApiKeyHash == apiKeyHash && t.IsActive);
                
            return tenant;
        }
        catch
        {
            return null;
        }
    }

    private string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}

public static class TenantAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantAuthenticationMiddleware>();
    }
}