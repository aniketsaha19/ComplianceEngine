# Database Reference

SQL Server, running in Docker, connected via the mssql VS Code extension.
Connection string lives in `engine/appsettings.json` under
`ConnectionStrings:Default`. Schema owned by EF Core migrations in
`/engine`, except `MarketPriceHistory` which is created and loaded directly
by `/data/DataLoader`.

## Tables

### `MarketPriceHistory`
Bulk-loaded reference data. Not managed by EF Core.

| Column | Type | Notes |
|---|---|---|
| `Ticker` | VARCHAR(10) | Part of composite PK |
| `TradeDate` | DATE | Part of composite PK |
| `Sector` | VARCHAR(50) | From `ticker-sectors.csv` |
| `ClosePrice` | DECIMAL(18,4) | Adjusted close, not raw close |

Row count: 2,703,531. 472 unique tickers, all sector-mapped.

### `Portfolios`
| Column | Type | Notes |
|---|---|---|
| `Id` | INT | PK, identity |
| `Name` | NVARCHAR | e.g. "Portfolio 01" |

### `Holdings`
| Column | Type | Notes |
|---|---|---|
| `Id` | INT | PK, identity |
| `PortfolioId` | INT | FK → `Portfolios.Id` |
| `Ticker` | NVARCHAR | Joins to `MarketPriceHistory.Ticker` (logical, no FK) |
| `Sector` | NVARCHAR | Denormalized copy, set at insert time |
| `Quantity` | DECIMAL | Can be updated by `POST /position-change` |

### `Rules`
| Column | Type | Notes |
|---|---|---|
| `Id` | INT | PK, identity |
| `Name` | NVARCHAR | Human-readable, e.g. "UCITS Single Issuer Limit" |
| `Description` | NVARCHAR | Plain-language explanation, used in demo/explanations |
| `RuleType` | NVARCHAR | One of the 5 types below |
| `Threshold` | DECIMAL | Interpreted differently per rule type — see below |
| `IsActive` | BIT | Inactive rules are skipped by the evaluator |

Currently 20 rows seeded — see "Seeded rules" below.

### `RuleEvaluations`
Audit log. Every evaluation, pass or fail, is written here — this is what
makes the engine auditable rather than a black box.

| Column | Type | Notes |
|---|---|---|
| `Id` | INT | PK, identity |
| `RuleId` | INT | FK → `Rules.Id` |
| `PortfolioId` | INT | FK → `Portfolios.Id` |
| `Breached` | BIT | |
| `CurrentValue` | DECIMAL | The computed value at evaluation time |
| `Threshold` | DECIMAL | Copy of the rule's threshold at evaluation time |
| `EvaluatedAt` | DATETIME2 | UTC |

## Rule types — formulas

All weights are computed as `holding.Quantity * latestClosePrice / totalPortfolioValue`.

| `RuleType` | Formula | Breach condition |
|---|---|---|
| `max_position_pct` | weight of the single largest holding | `weight > Threshold` |
| `max_sector_pct` | sum of weights within the largest sector | `sectorWeight > Threshold` |
| `min_holdings_count` | count of distinct tickers held | `count < Threshold` |
| `aggregate_large_position_pct` | sum of weights of all holdings individually ≥ 5% (hardcoded trigger, per UCITS convention) | `aggregate > Threshold` |
| `max_top_n_concentration` | sum of the top 10 holdings' weights (N=10, hardcoded) | `topSum > Threshold` |

## Seeded rules (20 total)

Grouped by type for reference — full names/descriptions/thresholds are in
the seed SQL, not repeated here to avoid drift between two copies. See
`engine`'s seed script (run once via the mssql extension) for the
authoritative list. Summary by category:

- 10× `max_position_pct` — thresholds from 5% (conservative) to 15%
  (aggressive growth), including the UCITS 10% single-issuer limit
- 5× `max_sector_pct` — thresholds from 15% (conservative) to 35%
  (sector-flexible)
- 3× `min_holdings_count` — minimums of 8, 15, and 20 distinct holdings
- 3× `aggregate_large_position_pct` — includes UCITS 5/10/40 (40%),
  SEC-style diversified (25%) and non-diversified (50%) fund limits
- 2× `max_top_n_concentration` — top-10 holdings caps at 45% and 60%

## Portfolios currently seeded

10 portfolios (`Portfolio 01`–`Portfolio 10`), each with 6 holdings across 3
sectors, using real tickers and real latest prices from
`MarketPriceHistory`. Portfolio 01 has one position deliberately inflated to
~37.5% of the portfolio (scripted breach). All 10 currently fail the
diversification-related rules identically, since all have exactly 6
holdings — this is the known gap Task 01 addresses.
