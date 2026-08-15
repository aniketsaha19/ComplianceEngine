using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.Data.SqlClient;

string connectionString = "Server=localhost,1433;Database=ComplianceEngine;User Id=sa;Password=SecurePass123!;TrustServerCertificate=True;";
string priceCsvPath = "../sp500-prices.csv";
string sectorCsvPath = "../ticker-sectors.csv";

if (args.Length > 0 && args[0] == "seed-portfolios")
{
    SeedPortfolios(connectionString);
    return;
}

if (args.Length > 0 && args[0] == "load-prices")
{
    LoadPrices(connectionString, priceCsvPath, sectorCsvPath);
    return;
}

Console.WriteLine("Usage: dotnet run -- load-prices | seed-portfolios");

// ---------- Load price history (what you already ran) ----------

void LoadPrices(string connStr, string pricePath, string sectorPath)
{
    var tickerSectors = new Dictionary<string, string>();
    using (var reader = new StreamReader(sectorPath))
    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    {
        foreach (var record in csv.GetRecords<TickerSector>())
            tickerSectors[record.Ticker] = record.Sector;
    }
    Console.WriteLine($"Loaded {tickerSectors.Count} ticker-sector mappings.");

    var table = new System.Data.DataTable();
    table.Columns.Add("Ticker", typeof(string));
    table.Columns.Add("TradeDate", typeof(DateTime));
    table.Columns.Add("Sector", typeof(string));
    table.Columns.Add("ClosePrice", typeof(decimal));

    int skipped = 0;
    using (var reader = new StreamReader(pricePath))
    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    {
        foreach (var record in csv.GetRecords<PriceRecord>())
        {
            if (!tickerSectors.TryGetValue(record.Ticker, out var sector))
            {
                skipped++;
                continue;
            }
            var row = table.NewRow();
            row["Ticker"] = record.Ticker;
            row["TradeDate"] = record.Date;
            row["Sector"] = sector;
            row["ClosePrice"] = record.AdjClose;
            table.Rows.Add(row);
        }
    }
    Console.WriteLine($"Prepared {table.Rows.Count} rows. Skipped {skipped} rows with no sector match.");

    using var connection = new SqlConnection(connStr);
    connection.Open();
    using var bulkCopy = new SqlBulkCopy(connection)
    {
        DestinationTableName = "MarketPriceHistory",
        BatchSize = 5000
    };
    bulkCopy.ColumnMappings.Add("Ticker", "Ticker");
    bulkCopy.ColumnMappings.Add("TradeDate", "TradeDate");
    bulkCopy.ColumnMappings.Add("Sector", "Sector");
    bulkCopy.ColumnMappings.Add("ClosePrice", "ClosePrice");
    bulkCopy.WriteToServer(table);

    Console.WriteLine("Price load complete.");
}

// ---------- Seed 10 portfolios from real tickers/prices ----------

void SeedPortfolios(string connStr)
{
    using var connection = new SqlConnection(connStr);
    connection.Open();

    var tickers = new List<(string Ticker, string Sector, decimal LatestClose)>();
    string query = @"
        SELECT h.Ticker, h.Sector, h.ClosePrice
        FROM MarketPriceHistory h
        INNER JOIN (
            SELECT Ticker, MAX(TradeDate) AS MaxDate
            FROM MarketPriceHistory
            GROUP BY Ticker
        ) latest ON h.Ticker = latest.Ticker AND h.TradeDate = latest.MaxDate";

    using (var cmd = new SqlCommand(query, connection))
    using (var reader = cmd.ExecuteReader())
    {
        while (reader.Read())
            tickers.Add((reader.GetString(0), reader.GetString(1), reader.GetDecimal(2)));
    }
    Console.WriteLine($"Loaded {tickers.Count} tickers with latest prices.");

    if (tickers.Count == 0)
    {
        Console.WriteLine("No tickers found — run 'dotnet run -- load-prices' first.");
        return;
    }

    var bySector = tickers.GroupBy(t => t.Sector).ToDictionary(g => g.Key, g => g.ToList());
    var sectors = bySector.Keys.ToList();
    var rng = new Random(42);

    for (int p = 1; p <= 10; p++)
    {
        int portfolioId;
        using (var cmd = new SqlCommand(
            "INSERT INTO Portfolios (Name) OUTPUT INSERTED.Id VALUES (@name)", connection))
        {
            cmd.Parameters.AddWithValue("@name", $"Portfolio {p:D2}");
            portfolioId = (int)cmd.ExecuteScalar()!;
        }

        var chosenSectors = sectors.OrderBy(_ => rng.Next()).Take(Math.Min(3, sectors.Count)).ToList();
        decimal targetPerHolding = 50000m;
        bool isBreachPortfolio = (p == 1);

        foreach (var sector in chosenSectors)
        {
            var candidates = bySector[sector].OrderBy(_ => rng.Next()).Take(2).ToList();
            foreach (var c in candidates)
            {
                decimal targetValue = (isBreachPortfolio && sector == chosenSectors[0] && c == candidates[0])
                    ? targetPerHolding * 3
                    : targetPerHolding;
                decimal quantity = Math.Round(targetValue / c.LatestClose, 0);

                using var insert = new SqlCommand(
                    "INSERT INTO Holdings (PortfolioId, Ticker, Sector, Quantity) VALUES (@pid, @t, @s, @q)", connection);
                insert.Parameters.AddWithValue("@pid", portfolioId);
                insert.Parameters.AddWithValue("@t", c.Ticker);
                insert.Parameters.AddWithValue("@s", c.Sector);
                insert.Parameters.AddWithValue("@q", quantity);
                insert.ExecuteNonQuery();
            }
        }
        Console.WriteLine($"Seeded Portfolio {p:D2} (Id={portfolioId}) — sectors: {string.Join(", ", chosenSectors)}");
    }
}