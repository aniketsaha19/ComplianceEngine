using System.Text.Json.Serialization;

namespace ComplianceEngine.Models;

public class MarketPriceHistory
{
    public int Id { get; set; }
    public string Ticker { get; set; } = "";
    public DateTime TradeDate { get; set; }
    public string Sector { get; set; } = "";
    public decimal ClosePrice { get; set; }
}