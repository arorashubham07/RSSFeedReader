# Quickstart and Validation: Add Feed Subscriptions

**Date**: 2026-09-13 | **Plan**: [plan.md](plan.md)

This guide is for the implemented MVP. Application projects do not exist yet; the commands below
are acceptance instructions, not evidence of completed builds or tests. Run commands from the
repository root. Use [contracts/subscriptions.md](contracts/subscriptions.md) for exact behavior
and [data-model.md](data-model.md) for state lifetime and concurrency.

## Prerequisites and Setup

- Install a supported, serviced .NET 10 SDK and a modern WebAssembly-capable browser on each
  target OS. Initial package restore requires access to package sources; add/list itself does
  not require Internet access. Record `dotnet --version` and `dotnet --list-sdks`.
- The implementation phase creates the planned `backend/RSSFeedReader.Api` and
  `frontend/RSSFeedReader.UI` projects. This guide does not scaffold or implement them.
- Configure each project's `http` launch profile: backend `http://localhost:5151`, frontend
  `http://localhost:5213`. Configure frontend `wwwroot/appsettings.json` with `ApiBaseUrl` equal
  to `http://localhost:5151/api/`. Allow precisely `http://localhost:5213` in backend CORS.
- Remove environment/command-line endpoint overrides that expose non-loopback addresses.
  HTTP-only profiles must not redirect to an unconfigured HTTPS port. If ports are occupied,
  select a free pair and change all three configuration locations together.

These commands work in PowerShell, bash, and zsh once the projects are implemented:

```text
dotnet --version
dotnet --list-sdks
dotnet restore backend/RSSFeedReader.Api/RSSFeedReader.Api.csproj
dotnet restore frontend/RSSFeedReader.UI/RSSFeedReader.UI.csproj
dotnet clean backend/RSSFeedReader.Api/RSSFeedReader.Api.csproj
dotnet clean frontend/RSSFeedReader.UI/RSSFeedReader.UI.csproj
dotnet build backend/RSSFeedReader.Api/RSSFeedReader.Api.csproj --no-restore -warnaserror
dotnet build frontend/RSSFeedReader.UI/RSSFeedReader.UI.csproj --no-restore -warnaserror
dotnet list backend/RSSFeedReader.Api/RSSFeedReader.Api.csproj package --vulnerable --include-transitive
dotnet list frontend/RSSFeedReader.UI/RSSFeedReader.UI.csproj package --vulnerable --include-transitive
```

Expected: restore/build success, no compiler warnings, reviewed direct/transitive package
licenses, and no applicable high/critical vulnerabilities. Audit-source failures mean the
audit is incomplete, not clean. If automated tests have been added, run their documented
`dotnet test` commands; do not treat the absence of a test project as passing automated tests.
Manual verification below remains the MVP gate.

## Foundation Routing Gate

Before implementing subscription UI, remove Home, Counter, and Weather demo pages, their
navigation links, demo sample data, and backend sample endpoints. Inspect pages/routes:

Windows PowerShell:

```powershell
Get-ChildItem frontend/RSSFeedReader.UI/Pages -Filter *.razor | Select-Object Name
Get-ChildItem frontend/RSSFeedReader.UI -Filter *.razor -Recurse | Select-String '^@page '
```

macOS/Linux:

```bash
find frontend/RSSFeedReader.UI/Pages -name '*.razor' -print
grep -R -n '^@page ' frontend/RSSFeedReader.UI --include='*.razor'
```

Clean/build the frontend and open its shell before feature UI work. At that intermediate gate,
an empty/not-found root is acceptable, but an ambiguous-route exception is not. After adding
Subscriptions, repeat the check: exactly one `@page "/"`, and a working subscription page.
Framework not-found handling may remain, but demo links/pages must be absent.

## Run the Application

Backend terminal:

```text
dotnet run --project backend/RSSFeedReader.Api/RSSFeedReader.Api.csproj --no-build --launch-profile http
```

Frontend terminal:

```text
dotnet run --project frontend/RSSFeedReader.UI/RSSFeedReader.UI.csproj --no-build --launch-profile http
```

Open `http://localhost:5213`. Check browser Console and Network for routing, CORS, and connection
errors. Verify actual listener addresses, not just startup URLs:

Windows PowerShell:

```powershell
Get-NetTCPConnection -State Listen | Where-Object { $_.LocalPort -in 5151, 5213 } | Select-Object LocalAddress, LocalPort, OwningProcess
```

macOS:

```bash
lsof -nP -iTCP:5151 -iTCP:5213 -sTCP:LISTEN
```

Linux:

```bash
ss -ltnp '( sport = :5151 or sport = :5213 )'
```

Expected: only IPv4/IPv6 loopback (127.0.0.1 / ::1), never 0.0.0.0, ::, or a LAN address.
Other local processes can access these ports; that is not remote authentication.

## HTTP Smoke Checks

Run with a fresh backend and no UI additions. Use synthetic values only, never private feed
URLs. Windows PowerShell 5.1 includes Invoke-RestMethod/Invoke-WebRequest:

```powershell
$endpoint = 'http://localhost:5151/api/subscriptions'
Invoke-RestMethod -Uri $endpoint
$body = @{ url = '  not a URL  ' } | ConvertTo-Json -Compress
(Invoke-WebRequest -UseBasicParsing -Uri $endpoint -Method Post -ContentType 'application/json' -Body $body).StatusCode
Invoke-RestMethod -Uri $endpoint | ConvertTo-Json -Compress
(Invoke-WebRequest -UseBasicParsing -Uri $endpoint -Method Post -ContentType 'application/json' -Body $body).StatusCode
Invoke-RestMethod -Uri $endpoint | ConvertTo-Json -Compress
```

macOS/Linux curl (also usable as curl.exe on Windows):

```bash
curl -i http://localhost:5151/api/subscriptions
curl -i -H 'Content-Type: application/json' --data '{"url":"  not a URL  "}' http://localhost:5151/api/subscriptions
curl -i -H 'Content-Type: application/json' --data '{"url":"  not a URL  "}' http://localhost:5151/api/subscriptions
curl -i http://localhost:5151/api/subscriptions
```

Expected: initial `[]`; each POST 204 with no body; final decoded list has exactly two equal
strings with both leading and trailing spaces preserved. GET includes Cache-Control: no-store.

For each negative case, record count before and after; none may mutate state:

| Request body / type | Expected |
|---------------------|----------|
| `{"url":"   "}`, `{}`, `{"url":null}`, or `null` as JSON | 400 |
| `{"url":42}` or malformed JSON | 400 |
| Valid-looking JSON sent as text/plain or form-urlencoded | 415 |
| DELETE on the subscription route | 405 |

PowerShell 5.1 throws for 4xx; inspect status in a try/catch rather than mistaking that exception
for a failed test. Example for blank input:

```powershell
try {
    Invoke-WebRequest -UseBasicParsing -Uri $endpoint -Method Post -ContentType 'application/json' -Body '{"url":"   "}'
} catch {
    [int]$_.Exception.Response.StatusCode
}
```

For overlapping writes, start from two known entries and dispatch two PowerShell background jobs:

```powershell
$jobs = foreach ($value in @('overlap-one', 'overlap-two')) {
  Start-Job -ArgumentList $value -ScriptBlock {
    param($value)
    $body = @{ url = $value } | ConvertTo-Json -Compress
    (Invoke-WebRequest -UseBasicParsing -Uri 'http://localhost:5151/api/subscriptions' -Method Post -ContentType 'application/json' -Body $body).StatusCode
  }
}
$jobs | Wait-Job | Receive-Job
$jobs | Remove-Job
Invoke-RestMethod -Uri 'http://localhost:5151/api/subscriptions' | ConvertTo-Json -Compress
```

Or from bash/zsh issue concurrent requests directly:

```bash
curl -i -H 'Content-Type: application/json' --data '{"url":"overlap-one"}' http://localhost:5151/api/subscriptions &
curl -i -H 'Content-Type: application/json' --data '{"url":"overlap-two"}' http://localhost:5151/api/subscriptions &
wait
curl -i http://localhost:5151/api/subscriptions
```

Confirm both requests succeed and final count increases by exactly two, containing both new
values. Either relative order is valid. Parallel dispatch does not guarantee that server
execution overlaps; record whether overlap was actually observed. Review the store's shared
append/read lock as well; sequential execution alone does not prove concurrency safety.

## CORS and No-Destination-Contact Checks

PowerShell preflight (change Origin to `http://localhost:9999` for the denial check):

```powershell
$headers = @{ Origin = 'http://localhost:5213'; 'Access-Control-Request-Method' = 'POST'; 'Access-Control-Request-Headers' = 'content-type' }
(Invoke-WebRequest -UseBasicParsing -Uri $endpoint -Method Options -Headers $headers).Headers
```

macOS/Linux:

```bash
curl -i -X OPTIONS -H 'Origin: http://localhost:5213' -H 'Access-Control-Request-Method: POST' -H 'Access-Control-Request-Headers: content-type' http://localhost:5151/api/subscriptions
curl -i -X OPTIONS -H 'Origin: http://localhost:9999' -H 'Access-Control-Request-Method: POST' -H 'Access-Control-Request-Headers: content-type' http://localhost:5151/api/subscriptions
```

Allowed origin must receive its exact allow-origin value with POST/Content-Type permitted;
disallowed origin must receive no allow-origin grant. Verify the real UI can POST through a
browser; command-line tools do not enforce browser CORS. Verify form/text POSTs cannot mutate.

Add `https://example.invalid/feed` and markup-like values while recording browser requests.
Only local application requests may occur. Also inspect the backend dependency graph and
handlers/store to confirm no feed HTTP client, URI probe, parser, or outbound request path
exists; browser Network alone cannot prove absence of backend network activity. Review logs
and logging configuration to confirm submitted strings and bodies are not emitted.

## End-to-End Acceptance

Restart the backend before this walkthrough; leave it running between additions and page reloads.

| Check | Action and expected result | Spec coverage |
|-------|----------------------------|---------------|
| First use | Open root with no sign-in; paste a prepared URL, Add, and identify its entry within 30 seconds. | FR-001/002; SC-001 |
| Sequential additions | Add 10 entries, timing each from Add to visible entry; each at most two seconds, all retained in order without page reload. | FR-003/006; SC-002 |
| Duplicates and arbitrary input | Add the same value twice, a non-URL, surrounding spaces, and an unreachable URL; every nonblank value is preserved. | FR-004/006; SC-003 |
| Inert text | Add `<b>feed</b>` and `javascript:alert(1)`; literal text only, no bold markup, script, link, or navigation. | FR-005; SC-003 |
| Blank | Attempt empty and whitespace-only Add; no request and no list change. | FR-007; SC-003 |
| Long input | Paste a 2,048-character value; confirm full text via selection/textContent and inspect at 375px and 1280px widths, no control overlap or truncation. | FR-011; SC-003 |
| View lifetime | Reload page or restart frontend only; all confirmed entries remain. Stop/restart backend; next reload shows empty list. | FR-008; SC-004 |
| Known failure | Block POST in browser request-blocking tools without stopping backend; attempt Add, retain confirmed entries/input, show no successful new entry. | FR-009 |
| Uncertain outcome | Interrupt POST response delivery; indicate unconfirmed state, do not retry. Reload after recovery to reconcile actual backend state. | FR-009 |
| Failed follow-up GET | Allow POST but block subsequent GET; retain old snapshot and report list update failure, not definite add failure. Unblock and reload to see committed entry. | FR-009 |
| Slow initialization | Throttle initial GET and attempt Add; guard prevents overlapping workflows or stale snapshot replacement. | FR-003/009 |
| Overlapping requests | Complete the HTTP overlap check above; both accepted additions survive. | FR-010 |
| Scope | Inspect available actions and routes: no feed refresh, items, removal, editing, search, or demo navigation. | FR-012 |

Blocking requests is different from stopping the backend: stopping intentionally destroys
in-memory state and cannot test preservation of previously confirmed backend entries.

## Record Results

Run on each target OS and fill evidence during implementation, not during planning:

| Platform | SDK / browser | Build and audit | HTTP / browser / security checks | Outcome |
|----------|---------------|-----------------|---------------------------------|---------|
| Windows | Not yet recorded | Not run | Not run | Pending |
| macOS | Not yet recorded | Not run | Not run | Pending |
| Linux | Not yet recorded | Not run | Not run | Pending |

For every run record date, exact commands, timing/count results, relevant screenshots or console
evidence, listener bindings, and any failures. Use synthetic inputs in evidence. SC-005 remains
unmet until all three platforms pass; a Windows-only pass must not be reported as cross-platform
success. Maintainer review, dependency/license review, and all required checks gate MVP sign-off.