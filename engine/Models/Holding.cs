namespace ComplianceEngine.Models;

public class Holding
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public Portfolio? Portfolio { get; set; }
    public string Ticker { get; set; } = "";
    public string Sector { get; set; } = "";
    public decimal Quantity { get; set; }
}