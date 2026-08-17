using System.Globalization;
using System.Threading.Tasks;
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

if (args.Length > 0 && args[0] == "seed-diverse-portfolios")
{
    SeedDiversePortfolios(connectionString);
    return;
}

if (args.Length > 0 && args[0] == "verify-diverse-portfolios")
{
    VerifyDiversePortfolios(connectionString);
    return;
}

if (args.Length > 0 && args[0] == "test-portfolio-api")
{
    VerifyDiversePortfolios(connectionString);
    Console.WriteLine("\n📋 API Testing Note:");
    Console.WriteLine("To test API endpoints, ensure engine is running on port 5070:");
    Console.WriteLine("cd engine && dotnet run");
    Console.WriteLine("Then test: curl http://localhost:5070/portfolio/11/compliance-summary");
    return;
}

if (args.Length > 0 && args[0] == "query-holdings")
{
    QueryHoldings(connectionString);
    return;
}

if (args.Length > 0 && args[0] == "clear-diverse-portfolios")
{
    ClearDiversePortfolios(connectionString);
    return;
}

if (args.Length > 0 && args[0] == "load-prices")
{
    LoadPrices(connectionString, priceCsvPath, sectorCsvPath);
    return;
}

Console.WriteLine("Usage: dotnet run -- load-prices | seed-portfolios | seed-diverse-portfolios | verify-diverse-portfolios | query-holdings | clear-diverse-portfolios");

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

// ---------- Seed diverse portfolios with varied compliance outcomes ----------

void SeedDiversePortfolios(string connStr)
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

    // Broadly compliant portfolios (2-3) - 25-30 holdings to clear aggregate rules
    CreateCompliantPortfolio(connection, 11, bySector, sectors, rng);
    CreateCompliantPortfolio(connection, 12, bySector, sectors, rng);
    CreateCompliantPortfolio(connection, 13, bySector, sectors, rng);

    // Concentration rule breach portfolios (2-3) - 20+ holdings with oversized position
    CreateConcentrationBreachPortfolio(connection, 14, bySector, sectors, rng);
    CreateConcentrationBreachPortfolio(connection, 15, bySector, sectors, rng);
    CreateConcentrationBreachPortfolio(connection, 16, bySector, sectors, rng);

    // Diversification rule breach portfolios (2-3) - 10-12 holdings, evenly weighted
    CreateDiversificationBreachPortfolio(connection, 17, bySector, sectors, rng);
    CreateDiversificationBreachPortfolio(connection, 18, bySector, sectors, rng);
    CreateDiversificationBreachPortfolio(connection, 19, bySector, sectors, rng);
}

void CreateCompliantPortfolio(SqlConnection connection, int portfolioNum, 
    Dictionary<string, List<(string Ticker, string Sector, decimal LatestClose)>> bySector, 
    List<string> sectors, Random rng)
{
    int portfolioId;
    using (var cmd = new SqlCommand(
        "INSERT INTO Portfolios (Name) OUTPUT INSERTED.Id VALUES (@name)", connection))
    {
        cmd.Parameters.AddWithValue("@name", $"Portfolio {portfolioNum:D2}");
        portfolioId = (int)cmd.ExecuteScalar()!;
    }

    // 25-30 holdings across 5+ sectors to clear aggregate concentration rules
    var chosenSectors = sectors.OrderBy(_ => rng.Next()).Take(Math.Min(6, sectors.Count)).ToList();
    int holdingsPerSector = 5; // 25-30 total holdings (5 per sector × 6 sectors)
    decimal targetPerHolding = 3000m; // modest position sizes for more holdings

    foreach (var sector in chosenSectors)
    {
        var candidates = bySector[sector].OrderBy(_ => rng.Next()).Take(holdingsPerSector).ToList();
        foreach (var c in candidates)
        {
            decimal quantity = Math.Round(targetPerHolding / c.LatestClose, 0);
            if (quantity <= 0) quantity = 1;

            using var insert = new SqlCommand(
                "INSERT INTO Holdings (PortfolioId, Ticker, Sector, Quantity) VALUES (@pid, @t, @s, @q)", connection);
            insert.Parameters.AddWithValue("@pid", portfolioId);
            insert.Parameters.AddWithValue("@t", c.Ticker);
            insert.Parameters.AddWithValue("@s", c.Sector);
            insert.Parameters.AddWithValue("@q", quantity);
            insert.ExecuteNonQuery();
        }
    }

    Console.WriteLine($"Seeded Portfolio {portfolioNum:D2} (Id={portfolioId}) — COMPLIANT: {chosenSectors.Count} sectors, {chosenSectors.Count * holdingsPerSector} holdings (clear aggregate rules)");
}

void CreateConcentrationBreachPortfolio(SqlConnection connection, int portfolioNum,
    Dictionary<string, List<(string Ticker, string Sector, decimal LatestClose)>> bySector,
    List<string> sectors, Random rng)
{
    int portfolioId;
    using (var cmd = new SqlCommand(
        "INSERT INTO Portfolios (Name) OUTPUT INSERTED.Id VALUES (@name)", connection))
    {
        cmd.Parameters.AddWithValue("@name", $"Portfolio {portfolioNum:D2}");
        portfolioId = (int)cmd.ExecuteScalar()!;
    }

    // Well diversified (20+ holdings) but with one oversized position
    var chosenSectors = sectors.OrderBy(_ => rng.Next()).Take(5).ToList();
    int holdingsPerSector = 4; // 20 total holdings (4 per sector × 5 sectors)
    decimal targetPerNormalHolding = 2500m;
    decimal oversizedTarget = 25000m; // Will likely breach concentration rules

    foreach (var sector in chosenSectors)
    {
        var candidates = bySector[sector].OrderBy(_ => rng.Next()).Take(holdingsPerSector).ToList();
        foreach (var c in candidates)
        {
            decimal target = (sector == chosenSectors[0] && c == candidates[0]) ? oversizedTarget : targetPerNormalHolding;
            decimal quantity = Math.Round(target / c.LatestClose, 0);
            if (quantity <= 0) quantity = 1;

            using var insert = new SqlCommand(
                "INSERT INTO Holdings (PortfolioId, Ticker, Sector, Quantity) VALUES (@pid, @t, @s, @q)", connection);
            insert.Parameters.AddWithValue("@pid", portfolioId);
            insert.Parameters.AddWithValue("@t", c.Ticker);
            insert.Parameters.AddWithValue("@s", c.Sector);
            insert.Parameters.AddWithValue("@q", quantity);
            insert.ExecuteNonQuery();
        }
    }

    Console.WriteLine($"Seeded Portfolio {portfolioNum:D2} (Id={portfolioId}) — CONCENTRATION BREACH: {chosenSectors.Count} sectors, {chosenSectors.Count * holdingsPerSector} holdings (1 oversized)");
}

void CreateDiversificationBreachPortfolio(SqlConnection connection, int portfolioNum,
    Dictionary<string, List<(string Ticker, string Sector, decimal LatestClose)>> bySector,
    List<string> sectors, Random rng)
{
    int portfolioId;
    using (var cmd = new SqlCommand(
        "INSERT INTO Portfolios (Name) OUTPUT INSERTED.Id VALUES (@name)", connection))
    {
        cmd.Parameters.AddWithValue("@name", $"Portfolio {portfolioNum:D2}");
        portfolioId = (int)cmd.ExecuteScalar()!;
    }

    // 10-12 holdings, evenly weighted, no deliberately oversized positions
    var chosenSectors = sectors.OrderBy(_ => rng.Next()).Take(4).ToList();
    int holdingsPerSector = 3; // 12 total holdings (3 per sector × 4 sectors)
    decimal targetPerHolding = 12000m; // Evenly distributed to avoid concentration breaches

    foreach (var sector in chosenSectors)
    {
        var candidates = bySector[sector].OrderBy(_ => rng.Next()).Take(holdingsPerSector).ToList();
        foreach (var c in candidates)
        {
            decimal targetValue = targetPerHolding;
            decimal quantity = Math.Round(targetValue / c.LatestClose, 0);
            if (quantity <= 0) quantity = 1;

            using var insert = new SqlCommand(
                "INSERT INTO Holdings (PortfolioId, Ticker, Sector, Quantity) VALUES (@pid, @t, @s, @q)", connection);
            insert.Parameters.AddWithValue("@pid", portfolioId);
            insert.Parameters.AddWithValue("@t", c.Ticker);
            insert.Parameters.AddWithValue("@s", c.Sector);
            insert.Parameters.AddWithValue("@q", quantity);
            insert.ExecuteNonQuery();
        }
    }

    Console.WriteLine($"Seeded Portfolio {portfolioNum:D2} (Id={portfolioId}) — DIVERSIFICATION BREACH: {chosenSectors.Count} sectors, {chosenSectors.Count * holdingsPerSector} holdings (evenly weighted)");
}

// ---------- Verify portfolio diversity ----------

void VerifyDiversePortfolios(string connStr)
{
    using var connection = new SqlConnection(connStr);
    connection.Open();

    Console.WriteLine("Portfolio Diversity Verification");
    Console.WriteLine("================================");

    // Check total portfolio count
    var cmd = new SqlCommand("SELECT COUNT(*) FROM Portfolios", connection);
    int totalCount = (int)cmd.ExecuteScalar();
    Console.WriteLine($"Total portfolios: {totalCount}");

    cmd = new SqlCommand("SELECT COUNT(*) FROM Portfolios WHERE Id >= 11", connection);
    int newCount = (int)cmd.ExecuteScalar();
    Console.WriteLine($"New portfolios (Id >= 11): {newCount}");

    Console.WriteLine();

    // Check each new portfolio
    string query = @"
        SELECT p.Id, p.Name, COUNT(h.Id) as HoldingsCount, 
               COUNT(DISTINCT h.Sector) as SectorCount
        FROM Portfolios p 
        LEFT JOIN Holdings h ON p.Id = h.PortfolioId 
        WHERE p.Id >= 11 
        GROUP BY p.Id, p.Name 
        ORDER BY p.Id";

    using (var cmd2 = new SqlCommand(query, connection))
    using (var reader = cmd2.ExecuteReader())
    {
        Console.WriteLine("New Portfolio Details:");
        Console.WriteLine("Id\tName\t\t\tHoldings\tSectors\tCategory");
        Console.WriteLine("--\t----\t\t\t--------\t--------\t--------");

        while (reader.Read())
        {
            int id = reader.GetInt32(0);
            string name = reader.GetString(1);
            int holdings = reader.GetInt32(2);
            int sectors = reader.GetInt32(3);
            
            string category = id switch
            {
                >= 11 and <= 13 => "Compliant",
                >= 14 and <= 16 => "Conc Breach", 
                >= 17 and <= 19 => "Div Breach",
                _ => "Unknown"
            };
            
            Console.WriteLine($"{id}\t{name}\t\t{holdings}\t\t{sectors}\t{category}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("✅ Portfolio diversity task completed!");
    Console.WriteLine($"✅ Created {newCount} new portfolios (11-19)");
    Console.WriteLine("   📊 Categories:");
    Console.WriteLine("   - Compliant (11-13): 15-20 holdings, 5+ sectors");
    Console.WriteLine("   - Concentration Breach (14-16): 12+ holdings, 1 oversized position");
    Console.WriteLine("   - Diversification Breach (17-19): 5-6 holdings, well-sized positions");
}

void QueryHoldings(string connStr)
{
    using var connection = new SqlConnection(connStr);
    connection.Open();

    // Query holdings for new diverse portfolios (IDs 29-37)
    string query = @"
        SELECT PortfolioId, COUNT(*) AS HoldingCount 
        FROM Holdings 
        WHERE PortfolioId BETWEEN 29 AND 37 
        GROUP BY PortfolioId 
        ORDER BY PortfolioId";

    using (var cmd = new SqlCommand(query, connection))
    using (var reader = cmd.ExecuteReader())
    {
        Console.WriteLine("PortfolioId | HoldingCount");
        Console.WriteLine("----------- | ------------");
        
        while (reader.Read())
        {
            int portfolioId = reader.GetInt32(0);
            int holdingCount = reader.GetInt32(1);
            Console.WriteLine($"{portfolioId,-11} | {holdingCount,12}");
        }
    }

    // Also check total portfolios
    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Portfolios", connection))
    {
        int totalPortfolios = (int)cmd.ExecuteScalar();
        Console.WriteLine($"\nTotal portfolios in database: {totalPortfolios}");
    }
}

void ClearDiversePortfolios(string connStr)
{
    using var connection = new SqlConnection(connStr);
    connection.Open();
    
    // Delete holdings for portfolios 11-19
    using (var cmd = new SqlCommand("DELETE FROM Holdings WHERE PortfolioId BETWEEN 11 AND 19", connection))
    {
        int deletedHoldings = cmd.ExecuteNonQuery();
        Console.WriteLine($"Deleted {deletedHoldings} holdings from portfolios 11-19");
    }
    
    // Delete portfolios 11-19
    using (var cmd = new SqlCommand("DELETE FROM Portfolios WHERE Id BETWEEN 11 AND 19", connection))
    {
        int deletedPortfolios = cmd.ExecuteNonQuery();
        Console.WriteLine($"Deleted {deletedPortfolios} portfolios (11-19)");
    }
    
    Console.WriteLine("✅ Old diverse portfolios cleared");
}