# Task 01 — Widen Portfolio Diversity

## Context
Read `/docs/handover.md` and `/docs/requirements.md` first if you haven't.
All 10 existing portfolios have exactly 6 holdings across 3 sectors, so they
all fail the diversification-related rules (`min_holdings_count`,
`max_top_n_concentration`) identically. This makes the demo look like the
engine just fails everything, rather than genuinely discriminating between
compliant and non-compliant portfolios.

## Goal
Add new portfolios (do not overwrite Portfolios 01–10) with a genuine spread
of outcomes:
- 2–3 portfolios that are **broadly compliant** — 15–20 holdings, spread
  across 5+ sectors, no single position or sector oversized. Should pass
  most or all 20 rules.
- 2–3 portfolios that fail **only concentration/sector rules** — well
  diversified in holding count (10+ holdings) but with one oversized
  position or sector-heavy tilt.
- 2–3 portfolios that fail **only diversification rules** — genuinely few
  holdings (5–7) but each position reasonably sized, so concentration rules
  pass while `min_holdings_count`/`max_top_n_concentration` fail.

## Where this goes
Extend `SeedPortfolios` in `data/DataLoader/Program.cs`, or add a new method
(`SeedDiversePortfolios`) called via a new argument, e.g.:
```bash
dotnet run -- seed-diverse-portfolios
```
Do not modify the existing `seed-portfolios` mode's behavior — it should
still work exactly as before, producing Portfolios 01–10 unchanged if run
again against an empty database.

## Requirements
- Use real tickers and real latest prices from `MarketPriceHistory` — same
  pattern as the existing `SeedPortfolios` method (query latest close price
  per ticker, don't invent numbers).
- Continue the naming pattern: `Portfolio 11`, `Portfolio 12`, etc.
- Print a summary line per portfolio as it's created (same style as the
  existing method), stating which category it belongs to (compliant /
  concentration-breach / diversification-breach) so it's visible in console
  output.

## Acceptance criteria
1. `dotnet build` succeeds with no new errors.
2. Running the new seed mode creates the expected new portfolios (verify
   count via a query against `Portfolios`).
3. For each new portfolio, call
   `GET /portfolio/{id}/compliance-summary` and confirm the result matches
   its intended category — at least one portfolio should come back
   `"compliant": true` or close to it.
4. Do not touch `Rules`, `RuleEvaluations`, or any existing endpoint.
5. Report back the full list of new portfolio IDs and their
   `compliant` status, so it's easy to pick a "clean" one and a "breaching"
   one for later demo/simulator use.
