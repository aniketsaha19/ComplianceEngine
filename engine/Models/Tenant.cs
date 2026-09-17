namespace ComplianceEngine.Models;

public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ApiKeyHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<Rule> Rules { get; set; } = new List<Rule>();
    public ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
}