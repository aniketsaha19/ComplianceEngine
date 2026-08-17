# Task Prompts — Run In Order

Each file below is a self-contained prompt. Give your agent one file at a
time — don't chain them in a single run. Confirm each task's "Acceptance
criteria" section before moving to the next file.

| Order | File                              | What it does                                                              |
| ----- | --------------------------------- | ------------------------------------------------------------------------- |
| 1     | `01-widen-portfolio-diversity.md` | Adds new, more varied portfolios so the demo has genuine pass/fail spread |
| 2     | `02-event-simulator.md`           | Builds a live trade-feed simulator using real historical prices           |
| 3     | `03-mcp-server-scaffold.md`       | Sets up the Python MCP server project (no logic yet)                      |
| 4     | `04-mcp-server-tools.md`          | Implements the three MCP tools, wired to the engine's API                 |
| 5     | `05-mcp-server-testing.md`        | Connects the MCP server to Claude Desktop and runs a live demo test       |

Before running any of these, the agent should have already read
`/docs/docs/handover.md` and `/docs/docs/requirements.md` in full.

Understand `/docs/docs/architecture.md` , `/docs/docs/database.md` and `/docs/docs/api.md` to have full context and perform what is told.
