using ComplianceEngine.Data;
using ComplianceEngine.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ComplianceEngine.Services;

public class HoldingWeight
{
    public string Ticker { get; set; } = "";
    public string Sector { get; set; } = "";
    public decimal Value { get; set; }
    public decimal Weight { get; set; }
}

public class RuleEvaluationOutcome
{
    public int RuleId { get; set; }
    public string RuleName { get; set; } = "";
    public string RuleType { get; set; } = "";
    public bool Breached { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Threshold { get; set; }
    public string Detail { get; set; } = "";
}

public class RuleEvaluationService
{
    private readonly ComplianceDbContext _db;
    private readonly string _connectionString;
    private const decimal LargePositionTrigger = 0.05m; // UCITS "5%" trigger
    private const int TopNHoldings = 10;

    public RuleEvaluationService(ComplianceDbContext db, IConfiguration config)
    {
        _db = db;
        _connectionString = config.GetConnectionString("Default")!;
    }

    public async Task<List<RuleEvaluationOutcome>> EvaluatePortfolioAsync(int portfolioId)
    {
        var holdings = await _db.Holdings.Where(h => h.PortfolioId == portfolioId).ToListAsync();
        if (holdings.Count == 0) return new List<RuleEvaluationOutcome>();

        var prices = GetLatestPrices(holdings.Select(h => h.Ticker).Distinct().ToList());

        var weighted = new List<HoldingWeight>();
        decimal totalValue = 0m;
        foreach (var h in holdings)
        {
            decimal price = prices.TryGetValue(h.Ticker, out var p) ? p : 0m;
            decimal value = h.Quantity * price;
            totalValue += value;
            weighted.Add(new HoldingWeight { Ticker = h.Ticker, Sector = h.Sector, Value = value });
        }
        foreach (var w in weighted)
            w.Weight = totalValue == 0 ? 0 : w.Value / totalValue;

        var rules = await _db.Rules.Where(r => r.IsActive).ToListAsync();
        var results = new List<RuleEvaluationOutcome>();

        foreach (var rule in rules)
        {
            var outcome = rule.RuleType switch
            {
                "max_position_pct" => EvaluateMaxPosition(rule, weighted),
                "max_sector_pct" => EvaluateMaxSector(rule, weighted),
                "min_holdings_count" => EvaluateMinHoldings(rule, weighted),
                "aggregate_large_position_pct" => EvaluateAggregateLargePosition(rule, weighted),
                "max_top_n_concentration" => EvaluateTopN(rule, weighted),
                _ => new RuleEvaluationOutcome { RuleId = rule.Id, RuleName = rule.Name, RuleType = rule.RuleType, Detail = "Unknown rule type" }
            };
            results.Add(outcome);

            _db.RuleEvaluations.Add(new RuleEvaluation
            {
                RuleId = rule.Id,
                PortfolioId = portfolioId,
                Breached = outcome.Breached,
                CurrentValue = outcome.CurrentValue,
                Threshold = outcome.Threshold,
                EvaluatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return results;
    }

    private Dictionary<string, decimal> GetLatestPrices(List<string> tickers)
    {
        var result = new Dictionary<string, decimal>();
        if (tickers.Count == 0) return result;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var paramNames = tickers.Select((t, i) => $"@t{i}").ToList();
        string sql = $@"
            SELECT m.Ticker, m.ClosePrice
            FROM MarketPriceHistory m
            INNER JOIN (
                SELECT Ticker, MAX(TradeDate) AS MaxDate
                FROM MarketPriceHistory
                WHERE Ticker IN ({string.Join(",", paramNames)})
                GROUP BY Ticker
            ) latest ON m.Ticker = latest.Ticker AND m.TradeDate = latest.MaxDate";

        using var cmd = new SqlCommand(sql, connection);
        for (int i = 0; i < tickers.Count; i++)
            cmd.Parameters.AddWithValue(paramNames[i], tickers[i]);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result[reader.GetString(0)] = reader.GetDecimal(1);

        return result;
    }

    private RuleEvaluationOutcome EvaluateMaxPosition(Rule rule, List<HoldingWeight> w)
    {
        var worst = w.OrderByDescending(x => x.Weight).First();
        bool breached = worst.Weight > rule.Threshold;
        return new RuleEvaluationOutcome
        {
            RuleId = rule.Id, RuleName = rule.Name, RuleType = rule.RuleType,
            Breached = breached, CurrentValue = worst.Weight, Threshold = rule.Threshold,
            Detail = breached
                ? $"{worst.Ticker} is {worst.Weight:P1} of the portfolio, exceeding the {rule.Threshold:P0} single-position limit."
                : $"Largest position is {worst.Ticker} at {worst.Weight:P1}, within the {rule.Threshold:P0} limit."
        };
    }

    private RuleEvaluationOutcome EvaluateMaxSector(Rule rule, List<HoldingWeight> w)
    {
        var worst = w.GroupBy(x => x.Sector)
            .Select(g => new { Sector = g.Key, Weight = g.Sum(x => x.Weight) })
            .OrderByDescending(x => x.Weight).First();
        bool breached = worst.Weight > rule.Threshold;
        return new RuleEvaluationOutcome
        {
            RuleId = rule.Id, RuleName = rule.Name, RuleType = rule.RuleType,
            Breached = breached, CurrentValue = worst.Weight, Threshold = rule.Threshold,
            Detail = breached
                ? $"{worst.Sector} sector is {worst.Weight:P1} of the portfolio, exceeding the {rule.Threshold:P0} sector limit."
                : $"Largest sector exposure is {worst.Sector} at {worst.Weight:P1}, within the {rule.Threshold:P0} limit."
        };
    }

    private RuleEvaluationOutcome EvaluateMinHoldings(Rule rule, List<HoldingWeight> w)
    {
        int count = w.Select(x => x.Ticker).Distinct().Count();
        bool breached = count < rule.Threshold;
        return new RuleEvaluationOutcome
        {
            RuleId = rule.Id, RuleName = rule.Name, RuleType = rule.RuleType,
            Breached = breached, CurrentValue = count, Threshold = rule.Threshold,
            Detail = breached
                ? $"Portfolio holds only {count} distinct securities, below the minimum of {rule.Threshold} required."
                : $"Portfolio holds {count} distinct securities, meeting the minimum diversification requirement."
        };
    }

    private RuleEvaluationOutcome EvaluateAggregateLargePosition(Rule rule, List<HoldingWeight> w)
    {
        decimal aggregate = w.Where(x => x.Weight >= LargePositionTrigger).Sum(x => x.Weight);
        bool breached = aggregate > rule.Threshold;
        return new RuleEvaluationOutcome
        {
            RuleId = rule.Id, RuleName = rule.Name, RuleType = rule.RuleType,
            Breached = breached, CurrentValue = aggregate, Threshold = rule.Threshold,
            Detail = breached
                ? $"Positions of {LargePositionTrigger:P0}+ total {aggregate:P1}, exceeding the {rule.Threshold:P0} aggregate limit."
                : $"Positions of {LargePositionTrigger:P0}+ total {aggregate:P1}, within the {rule.Threshold:P0} aggregate limit."
        };
    }

    private RuleEvaluationOutcome EvaluateTopN(Rule rule, List<HoldingWeight> w)
    {
        decimal topSum = w.OrderByDescending(x => x.Weight).Take(TopNHoldings).Sum(x => x.Weight);
        bool breached = topSum > rule.Threshold;
        return new RuleEvaluationOutcome
        {
            RuleId = rule.Id, RuleName = rule.Name, RuleType = rule.RuleType,
            Breached = breached, CurrentValue = topSum, Threshold = rule.Threshold,
            Detail = breached
                ? $"Top {TopNHoldings} holdings represent {topSum:P1} of the portfolio, exceeding the {rule.Threshold:P0} limit."
                : $"Top {TopNHoldings} holdings represent {topSum:P1} of the portfolio, within the {rule.Threshold:P0} limit."
        };
    }
}