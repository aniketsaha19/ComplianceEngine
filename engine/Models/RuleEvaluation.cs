namespace ComplianceEngine.Models;

public class RuleEvaluation
{
    public int Id { get; set; }
    public int RuleId { get; set; }
    public int PortfolioId { get; set; }
    public bool Breached { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Threshold { get; set; }
    public DateTime EvaluatedAt { get; set; }
}