# RSSFeedReader

## Prerequisites

The planned MVP requires a supported, serviced .NET 10 SDK (C# 14, `net10.0`) and a
WebAssembly-capable browser. See the [implementation guide](specs/001-add-subscriptions/quickstart.md).

## Implementation Status

The SDK prerequisite was cleared on 2026-09-13: `dotnet --version` selects `10.0.401`.
Both `9.0.318` and `10.0.401` are installed on the Windows development host. The application
uses the .NET 10 SDK, not just the runtime. Implementation is in progress.

No application projects have been scaffolded, and no build or application acceptance checks
have run. The implementation target remains .NET 10.
