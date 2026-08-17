# Requirements & Guardrails

This defines what the remaining work must do, and — just as important — what
it must NOT touch. Read this before running any task prompt.

## Hard constraints — do not violate these

1. **Do not change existing table schemas** (`Portfolios`, `Holdings`,
   `Rules`, `RuleEvaluations`, `MarketPriceHistory`) without an EF Core
   migration. Never hand-edit the database schema directly.
2. **Do not rename or change the signature of existing endpoints**
   (`GET /portfolio/{id}/holdings`, `GET /portfolio/{id}/compliance-summary`,
   `POST /position-change`). New functionality gets new endpoints or new
   modes on existing tools, not breaking changes to these three.
3. **Do not touch the 20 seeded rules** in the `Rules` table as part of the
   diversity/simulator/MCP work. If a task genuinely requires a new rule,
   it must be called out explicitly, not silently added.
4. **Do not duplicate rule evaluation logic outside `/engine`.** The MCP
   server must call the engine's HTTP API — it must never reimplement
   compliance math in Python. This is a core design decision (see
   `architecture.md`), not a style preference.
5. **Do not introduce new languages/runtimes without reason.** `/engine` and
   `/data/DataLoader` are C#/.NET 10. `/mcp-server` is Python. Nothing else.
6. **Do not change the connection string pattern or add a new database.**
   Everything reads/writes the one SQL Server instance already running in
   Docker, via the same `ConnectionStrings:Default` pattern already in use.
7. **Do not remove or weaken the "Skipped 0 rows" / row-count verification
   habits** already established in the DataLoader — any new data operation
   should print a verifiable count, not run silently.

## Functional requirements for the remaining work

### Portfolio diversity (Task 01)
- Must produce a genuine spread: some portfolios that pass most/all rules,
  some that fail concentration rules specifically, some that fail
  diversification rules specifically.
- Must still use real tickers/prices from `MarketPriceHistory` — no
  invented data.
- Must not delete or renumber the existing 10 portfolios without explicit
  confirmation — prefer adding new portfolios (11+) over overwriting.

### Event simulator (Task 02)
- Must replay real historical prices from `MarketPriceHistory`, in
  chronological order, at a compressed/simulated pace.
- Must call `POST /position-change` (the existing endpoint) — must not
  write directly to the `Holdings` table.
- Must include at least one scripted, reproducible breach scenario, driven
  by genuine historical price movement (not a random/fake spike).

### MCP server (Tasks 03–05)
- Must be a standard MCP server using the official Python SDK.
- Must expose exactly three tools initially: `check_compliance`,
  `get_holdings`, `explain_breach` — matching the naming already agreed in
  `architecture.md`.
- Every tool must return structured JSON with enough detail (current value,
  threshold, timestamp) that the calling LLM never has to estimate or guess
  a number.
- Must run inside its own virtual environment in `/mcp-server/venv`, with a
  `requirements.txt` committed once dependencies are settled.
- Must be testable by connecting it to Claude Desktop as a local MCP server
  before being considered complete.

## Non-functional requirements
- Every task prompt must be run and verified independently before starting
  the next — no combining steps to "save time."
- Every new script/tool must print enough output to verify it worked
  (row counts, HTTP status codes, tool call results) — no silent success.
- Prefer small, reviewable changes over large ones. If a task feels like it
  needs to touch more than 3–4 files, stop and flag it rather than proceeding.

## What "done" looks like for the whole remaining scope
A live demo where: the event simulator is running, replaying real trades; a
portfolio crosses a compliance threshold from genuine price movement; asking
Claude (connected via the MCP server) "is Portfolio X compliant?" returns a
correct, explained answer sourced from the real rule engine — not a guess.
