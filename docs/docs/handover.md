# Handover — Compliance Rules Engine Project

Read this file first, before touching any code. It exists so a new agent session
(or a new person) has full context without re-deriving decisions already made.

## What this project is

A real-time portfolio compliance rule engine for asset management, built as a
personal/resume project. It demonstrates a pattern used in production by real
buy-side platforms (e.g. BlackRock Aladdin): a deterministic rule engine does
the compliance math, and an LLM (via MCP) sits on top to answer natural-language
questions about the results. The LLM never computes compliance numbers itself —
it only calls the engine's API and explains what comes back.

## Why this project exists (for context, not for the agent to act on)

Built by a senior .NET engineer (fintech/lease-finance background) exploring
open problems in asset management technology, as a portfolio piece. Not a
production system, not for a real firm. Optimize for: working end-to-end,
technically credible, demoable.

## Current status: what is already built and working

### `/data`

- `sp500-prices.csv` — 26 years of daily OHLCV, 472 S&P 500 tickers (Kaggle:
  jacksaleeby/s-and-p500-historical-data)
- `ticker-sectors.csv` — hand-built ticker→sector mapping, verified 1:1 against
  every unique ticker in the price file
- `DataLoader/` — .NET 10 console app, two working modes:
  - `dotnet run -- load-prices` — loaded all 2,703,531 price rows into
    `MarketPriceHistory`
  - `dotnet run -- seed-portfolios` — generated 10 portfolios with holdings,
    using real tickers/prices from `MarketPriceHistory`. Portfolio 01 has a
    deliberately oversized position (~37.5% in one ticker) as a scripted
    breach scenario.

### `/engine`

- .NET 10 Web API (`ComplianceEngine`), SDK pinned via `global.json`
- SQL Server via Docker, connected through the mssql VS Code extension
- EF Core schema, migrated and live: `Portfolios`, `Holdings`, `Rules`,
  `RuleEvaluations` (plus `MarketPriceHistory`, loaded by the DataLoader, not
  by EF Core)
- `RuleEvaluationService` — evaluates a portfolio against all active rules,
  writes every evaluation (pass or fail) to `RuleEvaluations`
- 20 rules seeded, covering 5 rule types, based on real fund-industry
  conventions (UCITS 5/10/40, SEC diversified/non-diversified fund limits,
  typical mandate concentration/diversification caps) — see `database.md`
- 3 working endpoints — see `api.md`:
  - `GET /portfolio/{id}/holdings`
  - `GET /portfolio/{id}/compliance-summary`
  - `POST /position-change`
- Verified end-to-end against real portfolios: confirmed the engine correctly
  discriminates (some rules pass, some fail, depending on actual portfolio
  composition — not a blanket fail).

Full schema and endpoint details: see `database.md` and `api.md`.
Full architecture reasoning (why MCP, why C# not Python for the loader, why
these 5 rule types): see `architecture.md`.

## What is NOT built yet (the remaining work)

1. **Wider portfolio diversity** — all 10 seeded portfolios currently have
   only 6 holdings across 3 sectors, so every portfolio fails the
   diversification-related rules identically. Need some portfolios that
   genuinely pass, for a believable demo.
2. **Event simulator** — a script that replays `MarketPriceHistory` data
   chronologically as a live trade feed, posting to `POST /position-change`
   over time, including a scripted breach moment.
3. **MCP server** (`/mcp-server`) — Python, wraps the engine's HTTP API as
   MCP tools (`check_compliance`, `get_holdings`, `explain_breach`) so an LLM
   (Claude or another MCP-compatible client) can answer natural-language
   questions about portfolio compliance.

Task-by-task prompts for all three are in `/docs/prompts/`, meant to be run
in order (`01-...` through `05-...`). Read `requirements.md` before starting
any of them — it defines what must not change.

## Ground rules for whoever picks this up

- Do not modify the existing schema, endpoints, or DataLoader modes without
  reading `requirements.md` first — most of what looks like it could be
  "improved" was a deliberate choice, explained in `architecture.md`.
- If something in these docs looks wrong or stale, flag it — don't silently
  work around it.
