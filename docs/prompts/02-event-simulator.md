# Task 02 — Event Simulator

## Context
Read `/docs/handover.md`, `/docs/requirements.md`, and `/docs/api.md` first.
Everything built so far is static — portfolios were seeded once and haven't
changed since. This task makes the system feel live: a script that replays
real historical price/trade activity as a stream of events hitting the real
API, over time.

## Goal
Build a console app (or a new mode in `data/DataLoader`) that:
1. Picks a portfolio and a date range from `MarketPriceHistory`
2. Walks that date range chronologically
3. On each date, decides whether a trade happens (your own scripted logic —
   doesn't need to be every single day) and, if so, calls
   `POST /position-change` on the running engine with a real quantity and
   the real closing price context for that date
4. Sleeps briefly (e.g. 200–500ms) between events to simulate a live feed
   rather than dumping everything instantly
5. Includes **one deliberate, reproducible scripted scenario**: pick a
   ticker with genuine historical price appreciation over the chosen date
   range, and script increasing buys into that ticker so that partway
   through the replay, a real compliance rule breaches — driven by actual
   price movement, not a fake number.

## Where this goes
New mode in `data/DataLoader/Program.cs`, e.g.:
```bash
dotnet run -- simulate-feed --portfolio 11 --from 2023-01-01 --to 2023-06-30
```
Or a separate console project under `data/EventSimulator/` if that's
cleaner — agent's call, but keep it inside `/data`, not `/engine`.

## Requirements
- Must call the real running API (`POST /position-change`) over HTTP — must
  NOT write to the `Holdings` table directly. The whole point is exercising
  the real endpoint, same as a real trade feed would.
- The engine (`dotnet run` in `/engine`) must already be running for this to
  work — the simulator should give a clear error if it can't reach the API,
  not fail silently.
- Print each event as it's posted: date, ticker, action, quantity, and
  whether the response came back compliant or not — so the breach moment is
  visible in real time in the console.
- Use `System.Net.Http.HttpClient` for the API calls.

## Acceptance criteria
1. `dotnet build` succeeds.
2. Running the simulator against a real portfolio and date range produces a
   visible stream of console output, one line per event, with realistic
   pacing (not instant).
3. At some point in the run, console output shows `"compliant": false`
   appearing where it wasn't before — confirm this by checking that
   specific rule's `detail` field references a ticker/sector that matches
   your scripted scenario.
4. After the run, query `RuleEvaluations` for that portfolio and confirm
   there's a growing audit trail with timestamps spread across the
   simulated run, not all at one instant.
5. Report back: which portfolio, which ticker, which date range, and the
   exact moment (date + rule name) the breach occurred — this becomes the
   demo script for later.
