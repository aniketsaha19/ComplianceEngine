using Microsoft.EntityFrameworkCore;
using ComplianceEngine.Data;
using ComplianceEngine.Services;
using ComplianceEngine.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<ComplianceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<RuleEvaluationService>();

var app = builder.Build();

// Use custom tenant authentication middleware (must be before authorization)
app.UseTenantAuthentication();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();