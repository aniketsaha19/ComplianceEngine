# Task 03 — MCP Server Scaffold

## Context
Read `/docs/handover.md`, `/docs/requirements.md`, and `/docs/architecture.md`
first, specifically the "Why MCP" and "Data flow for the MCP layer" sections.
This task only sets up the project — no tool logic yet, that's Task 04.

## Goal
Create a working, empty MCP server skeleton in `/mcp-server` that can start
up and be recognized by an MCP client, before any real tool logic is added.

## Steps
```bash
cd mcp-server
python3 -m venv venv
source venv/bin/activate
pip install mcp httpx
```

Create `mcp-server/server.py` with a minimal MCP server that registers no
tools yet (or one trivial placeholder tool, e.g. `ping`, that just returns
`"pong"`) — the goal here is confirming the server process starts and speaks
MCP correctly, before wiring it to the engine.

Create `mcp-server/requirements.txt`:
```bash
pip freeze > requirements.txt
```

## Requirements
- Must use the official `mcp` Python SDK — do not hand-roll the protocol.
- Must run entirely inside the `venv` — do not install anything with
  `--break-system-packages` or globally.
- Keep this file minimal. Do not add `check_compliance`, `get_holdings`, or
  `explain_breach` yet — that's Task 04, deliberately separated so scaffold
  problems and tool-logic problems don't get debugged at the same time.

## Acceptance criteria
1. `python3 server.py` (or whatever the SDK's run command is) starts
   without errors.
2. The server can be pointed to from an MCP client config (e.g. Claude
   Desktop's config file) and shows up as connected, even with just the
   placeholder tool.
3. `requirements.txt` exists and lists `mcp` and `httpx` at minimum.
4. Report back the exact command used to start the server, and confirm
   whether the placeholder tool was callable from a connected client — this
   confirms the plumbing works before Task 04 adds real logic on top.
