# Research: Add Feed Subscriptions

**Date**: 2026-09-13 | **Feature**: [spec.md](spec.md)

Phase 0 is complete. Research was delegated read-only and consolidated against the stakeholder
documents and constitution. Official documentation supports the choices below; no application
code, SDK installation, or runtime verification was performed. All design unknowns are resolved.

## 1. Runtime and Project Baseline

**Decision**: Use .NET 10 LTS, C# 14, and `net10.0` for both projects. Select a current serviced
10.0 SDK at implementation time and record its full version. Use `webapi` with Minimal APIs
and the standalone `blazorwasm` template, no authentication, no HTTPS requirement for this
loopback-only POC, and no runtime OpenAPI package.

**Rationale**: This is the required ASP.NET Core/Blazor separation with a supported LTS baseline.
.NET 10 support runs through November 14, 2028. Runtime patch numbers are not SDK version pins.

**Alternatives considered**: .NET 8/9 have less remaining support. The `blazor` template is a
different hosting architecture; the old hosted WASM template is not the .NET 10 path.

**Sources**:
- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates
- https://learn.microsoft.com/en-us/aspnet/core/blazor/tooling?view=aspnetcore-10.0

## 2. State Ownership and Concurrency

**Decision**: Register a concrete singleton SubscriptionStore with a private `List<string>`.
Use one private `System.Threading.Lock` for both append and copying the list to a new array.
Never return the live list, await inside the lock, or serialize while holding it. Backend
restart defines the end of the application run; browser/frontend restart does not.

**Rationale**: Singleton lifetime provides reload retention. Explicit locking protects
overlapping requests; a copied array provides a consistent snapshot and preserves duplicates.
DI container thread safety does not make a registered instance thread-safe.

**Alternatives considered**: A scoped store loses state across requests; a static mutable list
complicates ownership; a concurrent bag lacks required order. Persistence and repository layers
add work outside the MVP. A read-only wrapper over a live mutable list is not a snapshot.

**Sources**:
- https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines
- https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1?view=net-10.0
- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock

## 3. Presence-Only Input and HTTP Contract

**Decision**: GET `/api/subscriptions` returns a JSON string array. POST accepts an object
with nullable string `url`; reject absent, null, empty, or whitespace-only values with 400,
otherwise append the original string and return 204. Malformed JSON/type errors are transport
rejections, not URL validation. The browser skips blank submissions. Require application/json
for POST; do not accept form submissions, query-string mutation, or GET-based mutation.

**Rationale**: Explicit `string.IsNullOrWhiteSpace` is sufficient. A DTO makes body binding
unambiguous; no `[Url]`, URI parser, normalization, trimming, uniqueness check, or feed probe is
allowed. Minimal API annotation validation must not silently broaden acceptance rules.

**Alternatives considered**: A POST returning a snapshot saves a request but makes add and
list less independently exercised; POST then GET is simple for this scale. Resource IDs and
Location headers are unnecessary because no individual-resource operations exist.

**Sources**:
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/dotnet/api/system.string.isnullorwhitespace?view=net-10.0
- https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0

## 4. UI Ordering, Confirmation, and Literal Text

**Decision**: One busy guard serializes initial GET and each POST-then-GET workflow. Capture
input before awaiting. Replace the list only with a successful, nonnull, well-formed GET
snapshot. Preserve the last confirmed snapshot on failure, and never automatically retry an
uncertain POST. Reopening/reloading the view reconciles state when communication recovers.

Use a plain text control, not `type="url"` or a validation form that constrains URL syntax.
If using Blazor input binding, use an input-event string binding that does not trim values.
Render through ordinary Razor text expressions, never MarkupString or anchors. Use
`white-space: break-spaces`, `overflow-wrap: anywhere`, and a shrinkable list region so long
values and surrounding whitespace remain inspectable without covering controls.

**Rationale**: Async components are reentrant at awaits. Serializing entire workflows avoids
stale initial GETs replacing later state. A POST timeout is not proof of server rejection.
Encoded text satisfies no-validation requirements without executing user-provided content.

**Alternatives considered**: Optimistic append can claim success falsely; independent overlapping
GETs can regress the display; automatic POST retries can create unintended duplicates. A shared
state framework or JS renderer is not warranted for this single page.

**Sources**:
- https://learn.microsoft.com/en-us/aspnet/core/blazor/call-web-api?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/blazor/components/synchronization-context?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/blazor/components/?view=aspnetcore-10.0

## 5. Local Networking and Data Exposure

**Decision**: HTTP-only named `http` launch profiles bind API to localhost:5151 and UI to
localhost:5213. The configured client base URL is `http://localhost:5151/api/`. A global named
CORS policy allows exactly `http://localhost:5213`, GET/POST and Content-Type, without
credentials. Middleware handles OPTIONS preflight; there is no business OPTIONS endpoint.
Do not enable HTTPS redirection for this HTTP-only local profile. No HTTP body logging,
submitted-value logging, secrets in public client assets, or outbound backend HTTP client.

**Rationale**: application/json cross-origin POST needs preflight; localhost and 127.0.0.1 are
different origins. CORS is not authentication or isolation from local processes. JSON-only
mutation prevents ordinary cross-origin form writes; listener verification is still required.
Launch profiles do not prevent environment/command-line overrides, so verify actual bindings.

**Alternatives considered**: Wildcard CORS, wildcard listeners, HTTPS redirection to unconfigured
ports, and embedded hardcoded API URLs are incompatible with the local configuration contract.
Public hosting requires a separate security design and is not approved here.

**Sources**:
- https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0

## 6. Quality Gates Without Expanding the MVP

**Decision**: Use clean per-project builds, dependency review, manual HTTP/browser acceptance,
and per-OS records. Remove demo pages, links, sample data/endpoints, and conflicting routes at
the foundation gate. Check shell rendering before the feature and exactly one root route after
adding Subscriptions. No xUnit or browser-test package is installed solely by this plan.

**Rationale**: The constitution permits manual MVP verification. Shared-state and security risks
still need explicit checks for concurrent writes, literal text, request failure, CORS, and
listener binding. In-process HTTP tests alone would not prove real browser CORS or listeners.

**Alternatives considered**: Skipping browser checks misses runtime routing failures; requiring
a broad new test framework delays the stakeholder's minimal POC. Automated tests can be added
when needed, and later Extended-MVP backend work must use xUnit as required by the constitution.

**Sources**:
- https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/aspnet/core/blazor/test?view=aspnetcore-10.0

## Resolved Boundaries

- The stakeholder's "no HTTP client" means no feed-fetching client; the UI still needs the
  application API client explicitly required by the selected architecture.
- No feed error handling does not authorize optimistic success on failed local API calls.
- Cross-platform commands are part of the design; Windows/macOS/Linux runtime results remain
  unverified until implementation. No production readiness or completed acceptance is claimed.