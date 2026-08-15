using Microsoft.EntityFrameworkCore;
using ComplianceEngine.Models;

namespace ComplianceEngine.Data;

public class ComplianceDbContext : DbContext
{
    public ComplianceDbContext(DbContextOptions<ComplianceDbContext> options) : base(options) { }

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<Holding> Holdings => Set<Holding>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<RuleEvaluation> RuleEvaluations => Set<RuleEvaluation>();
}