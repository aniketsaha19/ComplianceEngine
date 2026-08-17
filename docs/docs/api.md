# API Reference

Base: `http://localhost:5000` (or whatever port `launchSettings.json`
assigns — check the console output on `dotnet run`).

All three endpoints below are live and verified working. This is the
complete surface of `/engine`'s API — the MCP server will call these three
and nothing else.

## `GET /portfolio/{id}/holdings`

Returns the current holdings for a portfolio.

**Response 200:**
```json
{
  "portfolioId": 1,
  "portfolioName": "Portfolio 01",
  "holdings": [
    { "id": 1, "portfolioId": 1, "ticker": "CCL", "sector": "Consumer Discretionary", "quantity": 1234 }
  ]
}
```

**Response 404** if the portfolio doesn't exist:
```json
{ "error": "Portfolio 1 not found." }
```

## `GET /portfolio/{id}/compliance-summary`

Runs a full rule evaluation and returns the result. This also writes one row
per rule to `RuleEvaluations` (audit trail) as a side effect.

**Response 200:**
```json
{
  "portfolioId": 1,
  "portfolioName": "Portfolio 01",
  "evaluatedAt": "2026-08-15T22:52:04.7655930Z",
  "compliant": false,
  "rules": [
    {
      "ruleId": 1,
      "ruleName": "UCITS Single Issuer Limit",
      "ruleType": "max_position_pct",
      "breached": true,
      "currentValue": 0.3753108301293702,
      "threshold": 0.10,
      "detail": "CCL is 37.5% of the portfolio, exceeding the 10% single-position limit."
    }
  ]
}
```
`compliant` is `true` only if every active rule passes. `detail` is a
plain-language sentence — this is the field the LLM layer should lean on for
explanations rather than re-deriving its own from the raw numbers.

**Response 404** — same shape as above, if portfolio doesn't exist.

## `POST /position-change`

Applies a buy or sell to a portfolio's holdings, then immediately re-runs
the full compliance evaluation and returns the result — same shape as
`compliance-summary`. This is the endpoint the event simulator (Task 02)
will call repeatedly.

**Request body:**
```json
{
  "portfolioId": 1,
  "ticker": "AAPL",
  "sector": "Technology",
  "action": "BUY",
  "quantity": 100
}
```
`action` is `"BUY"` or `"SELL"`. If the ticker isn't already held, `action`
must be `"BUY"` (creates a new holding). Selling more than is held, or
selling a ticker not held, returns 400.

**Response 200:**
```json
{
  "portfolioId": 1,
  "updatedTicker": "AAPL",
  "compliant": true,
  "rules": [ /* same shape as compliance-summary */ ]
}
```

**Response 400** — invalid action (e.g. negative resulting quantity, or
selling a non-existent position):
```json
{ "error": "Resulting quantity cannot be negative." }
```

**Response 404** — portfolio doesn't exist.

## Endpoints planned but not yet built

None — this is the complete planned surface for `/engine`. The remaining
work (event simulator, MCP server) is entirely built as *clients* of these
three endpoints, not as new engine endpoints. If a future task genuinely
needs a new endpoint, it should be called out explicitly and added here.
