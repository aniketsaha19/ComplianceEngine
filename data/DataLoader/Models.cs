using CsvHelper.Configuration.Attributes;

public class PriceRecord
{
    public string Ticker { get; set; }
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    [Name("Adj Close")]
    public decimal AdjClose { get; set; }
    public long Volume { get; set; }
}

public class TickerSector
{
    public string Ticker { get; set; }
    public string Sector { get; set; }
}