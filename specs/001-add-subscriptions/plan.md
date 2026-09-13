# Implementation Plan: Add Feed Subscriptions

**Branch**: `main` | **Date**: 2026-09-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-add-subscriptions/spec.md`,
[ProjectGoals.md](../../StakeholderDocuments/ProjectGoals.md), and
[TechStack.md](../../StakeholderDocuments/TechStack.md).

**Feature identifier**: `001-add-subscriptions`. The setup script reports this identifier as
`BRANCH`; Git independently reports `main`. No branch was created or switched.

**Status**: Phase 0 research and Phase 1 design complete. Planning artifacts only; ready for
`/speckit-tasks`. Application implementation and runtime acceptance have not been performed.

## Summary

Deliver one local subscription page that accepts a nonblank string and shows the resulting
list without a page reload. A separate ASP.NET Core Minimal API owns a thread-safe, in-memory
list; a standalone Blazor WebAssembly client serializes add/list requests and displays only
confirmed snapshots as inert text. Duplicate values and surrounding whitespace are preserved.
No feed fetching, parsing, URL validation, persistence, authentication, or background work is
introduced. See [research.md](research.md) for evidence and alternatives.

## Technical Context

**Language/Version**: C# 14, .NET 10 LTS, `net10.0`, current serviced 10.0 SDK.
The implementation must record the exact SDK used; no SDK installation is part of planning.

**Primary Dependencies**: ASP.NET Core shared framework; Microsoft.AspNetCore.Components.WebAssembly
and its development server from the .NET 10 standalone template; System.Net.Http.Json.
Use serviced 10.0 package versions and review restored transitive dependencies. No feed libraries.

**Storage**: Private singleton `List<string>` protected by one private lock for append and
snapshot copying. No disk, browser storage, database, static global list, or distributed cache.

**Testing**: Mandatory clean builds and repeatable manual API/browser checks for the MVP.
No test framework is required for this slice. Existing tests, if added, must run and pass;
xUnit unit/integration coverage becomes mandatory for later Extended-MVP backend behavior.

**Target Platform**: Separate loopback processes and a modern WebAssembly-capable browser on
Windows, macOS, and Linux. Cross-platform acceptance must be recorded individually.

**Project Type**: Two-project local web application, not a hosted Blazor Web App.

**Performance Goals**: First addition identified within 30 seconds; each of 10 sequential
successful additions visible within two seconds; 2,048-character values fully inspectable.

**Constraints**: Backend `http://localhost:5151`; frontend `http://localhost:5213`;
configured `ApiBaseUrl` is `http://localhost:5151/api/`. JSON requests, exact-origin CORS,
no credentials, no feed destinations contacted. Blank input is the only semantic rejection.

**Scale/Scope**: One local user, one page, GET and POST on `/api/subscriptions`, volatile state
for one backend process lifetime. Ten entries are an acceptance workload, not a capacity limit.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Authority: [constitution v1.0.0](../../.specify/memory/constitution.md).
PASS means design compliance, not a claim that runtime checks have already passed.

| Gate | Pre-research assessment | Design evidence / required verification |
|------|-------------------------|------------------------------------------|
| I. MVP scope | PASS | Only add/list; no feed operations, URL checks, persistence, or extra actions. |
| II. Security | PASS | Loopback-only listeners, exact-origin CORS, JSON-only mutation, encoded inert text, no submitted values in logs or secrets in assets. Verify actual listeners and rendering. |
| III. Architecture | PASS | API owns a locked singleton; UI uses snapshots. Two projects, no generic repository or speculative shared layer. |
| IV. Code quality | PASS | Nullable analysis, no new warnings, async I/O, explicit response handling, matching JSON contract, dependency review. |
| V. Verification | PASS | Manual MVP checks permitted; acceptance includes overlapping writes, failure stages, unsafe-looking input, and restart behavior. |
| Technology/runtime | PASS | ASP.NET Core plus standalone WASM; coordinated localhost ports and cross-platform commands. |
| Foundation gate | PASS | Remove template demo pages and links before feature UI; clean build and routing smoke checks before and after root-page addition. |
| Governance | PASS | Scope derives from supplied documents; no exception or amendment needed. Existing constitution draft remains untouched. |

**Post-design re-evaluation (2026-09-13)**: PASS for every gate above, with no exceptions.
[data-model.md](data-model.md) defines exact text preservation, atomic append, and snapshot
ownership. [contracts/subscriptions.md](contracts/subscriptions.md) covers all 12 functional
requirements, JSON-only mutation, CORS, safe rendering, and confirmed UI state.
[quickstart.md](quickstart.md) covers all five success criteria, foundation routing, failure
stages, concurrent requests, dependency review, listener checks, and per-OS evidence.

The model contains no durable state or feed operations; the contract exposes only add/list;
the validation guide adds no production features or mandatory MVP test framework. There are
no unresolved design questions. Runtime build, browser, listener, vulnerability, and OS checks
remain implementation acceptance gates, not outcomes established during this planning run.

## Project Structure

### Documentation (this feature)

```text
specs/001-add-subscriptions/
  spec.md
  plan.md
  research.md
  data-model.md
  quickstart.md
  contracts/
    subscriptions.md
  checklists/
    requirements.md
```

`tasks.md` is a subsequent `/speckit-tasks` output and is not created by this command.

### Source Code (repository root)
```text
backend/
  RSSFeedReader.Api/
    RSSFeedReader.Api.csproj
    Program.cs
    Models/AddSubscriptionRequest.cs
    Services/SubscriptionStore.cs
    Properties/launchSettings.json
    appsettings.json
frontend/
  RSSFeedReader.UI/
    RSSFeedReader.UI.csproj
    Program.cs
    App.razor
    _Imports.razor
    Pages/Subscriptions.razor
    Layout/MainLayout.razor
    Properties/launchSettings.json
    wwwroot/appsettings.json
    wwwroot/css/app.css
README.md
```

**Structure Decision**: The tree is the planned implementation layout, not existing code.
Keep the two small handlers in backend Program.cs and state operations in a concrete store.
The sole page owns its simple async workflow; no extra client-service abstraction or shared
DTO project is required for a single request shape and string-array response. Template layout
assets may remain only when used; demo pages, navigation, sample data, and sample endpoints must
be removed. Use per-project commands rather than requiring a solution or new test project.

### Delivery Sequence

1. Foundation: scaffold the two .NET 10 projects, establish nullable/build settings, configure
   localhost ports and exact CORS origin, remove sample code and demo routes. Verify the shell
   builds and renders without ambiguous routes before subscription UI work.
2. Implement the store and JSON contract, then the sole root page. Guard every initial-load
   and add workflow before its first await; POST then GET, with no optimistic list mutation.
3. Validate API and UI behavior, security boundaries, and per-OS acceptance. Update README
   setup instructions and record the exact SDK, commands, and outcomes. Do not claim unrun
   checks passed. These are future implementation steps, not work performed by this plan.

## Complexity Tracking

No constitution violations or exceptions. Two projects are mandated by the stakeholder stack;
a single concrete locked store is sufficient. No additional architecture is justified.
