namespace ComplianceEngine.Models;

public class Rule
{
    public int Id { get; set; }
    public string RuleType { get; set; } = ""; // "max_position_pct" | "max_sector_pct"
    public decimal Threshold { get; set; }     // e.g. 0.10 = 10%
    public bool IsActive { get; set; } = true;
}