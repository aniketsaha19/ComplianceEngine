# Task 04 — MCP Server Tools

## Context
Read `/docs/handover.md`, `/docs/requirements.md`, `/docs/architecture.md`,
and `/docs/api.md` first. This task assumes Task 03's scaffold is working —
the server starts and is reachable from a client. Now add the real tools.

## Goal
Implement three MCP tools in `mcp-server/server.py` (or split into a
`tools.py` if cleaner), each calling the engine's existing HTTP API via
`httpx` — no direct database access, no reimplemented compliance logic (see
`requirements.md`, hard constraint 4).

### `check_compliance(portfolio_id: int)`
Calls `GET /portfolio/{portfolio_id}/compliance-summary`. Returns the JSON
response as-is (or lightly reshaped for clarity, but don't drop the
`detail` field on any rule — that's what the LLM will lean on for
explanations).

### `get_holdings(portfolio_id: int)`
Calls `GET /portfolio/{portfolio_id}/holdings`. Returns the JSON response.

### `explain_breach(portfolio_id: int, rule_id: int)`
Calls `GET /portfolio/{portfolio_id}/compliance-summary`, then extracts and
returns just the entry for the given `rule_id` from the `rules` array — not
the whole response. If that rule isn't breached, say so explicitly rather
than returning an empty result.

## Requirements
- Base URL for the engine API should be configurable (env var or constant
  at the top of the file), not hardcoded inline in multiple places —
  Localhost port must match whatever `/engine`'s `launchSettings.json`
  actually uses.
- Every tool must handle the engine being unreachable (connection refused,
  timeout) and return a clear structured error — not an unhandled exception
  that crashes the MCP server.
- Every tool must handle a 404 from the engine (portfolio doesn't exist) and
  return a clear structured error, matching the engine's own error message
  where possible.
- Tool descriptions (the MCP schema metadata) should be written so an LLM
  calling them understands exactly what each returns — this is what lets
  Claude decide which tool to call for a given question.
- Support the engine being asked about **multiple portfolios in one turn**
  — this doesn't require special code, just make sure nothing in your tool
  design assumes only one call happens per conversation turn.

## Acceptance criteria
1. `dotnet run` in `/engine` is running, with at least one compliant and
   one non-compliant portfolio available (from Task 01).
2. Call each of the three tools directly (via whatever test harness the MCP
   SDK provides, or a simple Python script that invokes them) against a real
   portfolio ID and confirm the response matches what `curl` against the
   same endpoint returns.
3. Call `check_compliance` against a portfolio ID that doesn't exist and
   confirm a clean structured error comes back, not a crash.
4. Stop the engine (`Ctrl+C` in its terminal) and call any tool — confirm a
   clean "engine unreachable" error, not a hang or crash. Restart the engine
   afterward.
5. Report back the three tools' exact function signatures and a sample
   response from each, run against a real portfolio.
