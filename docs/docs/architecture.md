# Architecture

## System shape

Three tiers, each a separate concern:

```
/data          → source data + one-time/repeatable loading & seeding scripts (C#)
/engine        → the actual compliance rule engine, exposed as an HTTP API (C#/.NET 10)
/mcp-server    → thin translation layer exposing the engine's API as MCP tools (Python)
```

Chat clients (Claude, or any other MCP-compatible LLM client) talk only to
`/mcp-server`. `/mcp-server` talks only to `/engine`'s HTTP API. `/engine` is
the only thing that talks to the database. This is deliberate layering —
each tier can be understood, tested, and rebuilt independently.

## Key decisions and why

### Why a rule engine, not just LLM reasoning
LLMs are unreliable at enforcing hard numeric thresholds. A regulator (real
or simulated) would never accept "the AI decided this doesn't breach 10%."
The rule engine is deterministic, testable, and auditable — every evaluation
is logged in `RuleEvaluations`, pass or fail. The LLM's job is orchestration
and explanation, never calculation. This mirrors how BlackRock's Aladdin
Copilot is architected: the compliance rule engine predates Copilot by
decades; Copilot is a natural-language layer bolted on top of it, not a
replacement for it.

### Why MCP, not a Claude "skill" or a platform-specific plugin
- A Claude skill is static instructions — it can't hold live state or call
  a real API with live numbers. This project needs a live compliance check
  against a live database, which a skill can't do.
- A proprietary plugin (tied to one chat platform) only works in that one
  platform. MCP is the interface that works across Claude, Claude Desktop,
  and other MCP-compatible tool-calling clients without rewriting the engine.

### Why C# for the DataLoader, not Python
A Python loader talking to SQL Server needs the Microsoft ODBC driver
installed separately (`msodbcsql18` via Homebrew on Mac) — an extra
dependency chain for something `Microsoft.Data.SqlClient` already handles
natively, since the engine is already in .NET. Keeping the loader in C#
avoids that friction and lets it share patterns with the engine project.

### Why real historical data (Kaggle) instead of fully synthetic data
Synthetic/random price data can't reliably produce a believable breach
scenario on demand, and a real-looking demo needs prices that actually moved
the way markets move. Using real S&P 500 historical prices (Kaggle:
jacksaleeby/s-and-p500-historical-data) means the scripted breach in
Portfolio 01 happened because of genuine price appreciation, not an
implausible synthetic spike.

### Why sector data is a separate hand-built CSV
The chosen Kaggle price dataset has no sector column. Rather than search for
a "perfect" dataset with sector data built in (diminishing returns for ~470
tickers), sector was hand-mapped once (`ticker-sectors.csv`) and verified
1:1 against every unique ticker in the price data.

### Why these 5 rule types
Chosen because they (a) are computable from data already in the schema
(`Ticker`, `Sector`, `Quantity`, `ClosePrice`) with no new fields needed, and
(b) map to real, named fund-industry conventions rather than invented rules:

| Rule type | Real-world basis |
|---|---|
| `max_position_pct` | Single-issuer concentration limit (UCITS 10% cap and similar mandate limits) |
| `max_sector_pct` | Sector concentration limit — standard across most fund mandates |
| `aggregate_large_position_pct` | UCITS "5/10/40" rule — positions ≥5% can't total more than X% in aggregate |
| `min_holdings_count` | Diversification minimum — common in fund prospectuses |
| `max_top_n_concentration` | "Top 10 holdings" concentration limit — standard in fund fact sheets |

Full formulas for each are in `database.md`.

### Why Holdings↔MarketPriceHistory is a logical join, not a foreign key
`MarketPriceHistory` is bulk-loaded independently of the transactional
schema (2.7M+ rows, loaded once via `SqlBulkCopy`). Making it a formally
related EF Core entity would couple a large, mostly-static reference table
to the live transactional schema for no real benefit. The
`RuleEvaluationService` joins on `Ticker` via a direct SQL query at
evaluation time instead.

## Data flow for a single compliance check

1. Client calls `GET /portfolio/{id}/compliance-summary`
2. `PortfolioController` calls `RuleEvaluationService.EvaluatePortfolioAsync`
3. Service loads the portfolio's `Holdings`, looks up each ticker's latest
   price from `MarketPriceHistory`, computes portfolio weights
4. Service loads all active `Rules`, evaluates each against the weighted
   holdings
5. Every result (pass or fail) is written to `RuleEvaluations`
6. Structured JSON is returned to the caller

## Data flow for the MCP layer (once built)

```
Chat client (Claude, etc.)
   → MCP server (check_compliance / get_holdings / explain_breach)
      → HTTP call to /engine's existing endpoints
         → structured JSON back to MCP server
      → structured JSON back to the LLM
   → LLM explains the result in natural language
```

The MCP server does not touch the database directly and does not
reimplement any evaluation logic — see `requirements.md`, hard constraint 4.

## Entity relationships

See `database.md` for full column-level detail. Summary:

- `Portfolios` 1—* `Holdings` (via `Holdings.PortfolioId`)
- `Portfolios` 1—* `RuleEvaluations` (via `RuleEvaluations.PortfolioId`)
- `Rules` 1—* `RuleEvaluations` (via `RuleEvaluations.RuleId`)
- `Holdings` ⇢ `MarketPriceHistory` — logical join on `Ticker`, no FK
  constraint (see decision above)
