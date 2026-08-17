# Task 05 — Connect and Test With Claude Desktop

## Context
Read `/docs/handover.md` first. This is the final task — everything else
has been building toward this moment: a real chat client asking real
natural-language questions, answered by the real rule engine underneath.

## Goal
Connect the working MCP server (from Task 04) to Claude Desktop, and run a
live end-to-end demo, then write it down as a repeatable script.

## Steps
1. Locate Claude Desktop's MCP config file (path differs by OS — check
   Claude Desktop's own settings/docs for the current location, don't guess
   a stale path).
2. Add an entry pointing at `mcp-server/server.py`, using the project's
   `venv` Python interpreter explicitly (not the system Python) — e.g.
   `mcp-server/venv/bin/python3 mcp-server/server.py`.
3. Restart Claude Desktop, confirm the server shows as connected and its
   three tools are visible/available.
4. With `/engine` running and the event simulator (Task 02) either having
   already run or running live, ask Claude Desktop natural-language
   questions such as:
   - "Is Portfolio 11 compliant right now?"
   - "Which of my portfolios have compliance issues today?"
   - "Explain why Portfolio 1 is breaching its concentration limit."
5. Confirm Claude's answers are grounded in real numbers from the engine
   (cross-check against a direct `curl` call to the same endpoint) — not
   paraphrased guesses.

## Requirements
- Do not modify `/engine` or `/mcp-server`'s tool logic as part of this
  task — if something doesn't work, the fix belongs back in Task 04, not
  patched ad hoc here.
- Write down the exact config file contents used (with any machine-specific
  paths noted as such) so this is reproducible on a fresh machine.

## Acceptance criteria
1. Claude Desktop shows the MCP server connected with all three tools
   listed.
2. All three example questions above return correct, grounded answers.
3. Asking about a portfolio that doesn't exist returns a sensible answer,
   not a crash or a hallucinated result.
4. Write a short `mcp-server/DEMO.md` documenting: the exact config used,
   the three example questions and their real answers (screenshot or
   transcript), and the exact steps to reproduce the whole demo from a cold
   start (start SQL Server → start engine → start simulator → open Claude
   Desktop → ask questions).
5. Report back confirming all of the above, plus anything that didn't work
   as expected — this is the last task, so anything left broken here should
   be flagged clearly rather than silently left incomplete.
