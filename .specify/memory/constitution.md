# RSSFeedReader Constitution

## Core Principles

### I. MVP-First Scope

- The MVP MUST serve one user locally and implement only adding a subscription URL and
	displaying the subscription list. The list MUST update after a successful add without a
	manual page reload.
- Subscriptions MUST remain in backend memory only. Restarting the backend MAY discard them.
- The MVP MUST accept supplied URLs without feed URL validation and MUST NOT fetch, parse,
	or probe them. Feed-specific error handling is outside the MVP.
- Fetching, item display, persistence, removal, polling, and organization MUST NOT enter the
	MVP through incidental refactoring or dependency installation. Each later-phase capability
	MUST have an approved specification before implementation.

This boundary keeps the proof of concept measurable: a user can add a URL and see it listed.

### II. Security at Trust Boundaries

- The local POC MUST bind its application listeners to loopback interfaces. Backend CORS MUST
	allow only explicitly configured frontend origins; wildcard origins MUST NOT be used.
	CORS MUST NOT be treated as authentication. Non-local or multi-user exposure MUST receive
	a separate security design covering authentication, authorization, and transport protection.
- Subscription strings MUST be rendered as encoded text, never raw HTML or executable markup.
	Accepting URLs without validation MUST NOT cause navigation, network fetching, or script
	execution. The MVP MUST display them as inert text.
- Secrets MUST NOT be committed, logged, or stored in Blazor WebAssembly assets or client
	configuration. Logs MUST NOT include full subscription URLs, which can contain credentials
	or private query parameters.
- Before Extended-MVP feed fetching is enabled, the backend MUST restrict outbound requests to
	HTTP(S), reject embedded credentials and loopback/private/link-local destinations, and enforce
	those restrictions across DNS resolution, connection establishment, and redirects. Requests
	MUST have bounded timeouts and response sizes. XML parsing MUST disable external entity and
	DTD resolution. These controls MUST have automated negative tests.
- Extended-MVP titles MUST remain encoded text; item links MUST allow only HTTP(S) navigation.
	Rich HTML rendering MUST remain deferred until an approved sanitization design exists.

Security controls follow the capabilities actually enabled; accepting text is not permission
to trust or execute it.

### III. Minimal, Maintainable Architecture

- ASP.NET Core Web API MUST own subscription state and expose add/list operations. Blazor
	WebAssembly MUST own user interaction and call that API; it MUST NOT become an independent
	authoritative subscription store.
- API handlers, subscription state management, and presentation MUST have separate
	responsibilities. Shared in-memory state MUST handle overlapping requests without corruption
	and MUST NOT expose a mutable collection directly to callers.
- Implementations MUST use the smallest concrete design that satisfies the current phase.
	New layers, generic repositories, shared projects, or dependencies MUST include a rationale
	tied to a current requirement or demonstrated duplication, not a hypothetical future feature.
- Feed fetching and parsing, when approved, MUST run in the backend using HttpClient and
	System.ServiceModel.Syndication. The MVP MUST NOT add a feed-fetching HTTP client or parser;
	its frontend HTTP client is solely for the application API.

These boundaries permit later persistence and feed operations without coupling them to UI code.

### IV. Reviewable Code Quality

- C# changes MUST use descriptive names, consistent repository formatting, and nullable
	reference analysis. Changes MUST introduce no compiler warnings; nullable suppressions MUST
	include a documented reason in the review.
- API and UI contracts MUST agree on payloads, status codes, and configuration keys. Contract
	changes MUST update affected callers and verification steps in the same change.
- I/O MUST use asynchronous APIs without blocking waits. Exceptions MUST NOT be silently
	swallowed, and failed API calls MUST NOT be presented as successfully added subscriptions.
	This does not require a detailed error taxonomy or feed-error UI in the MVP.
- Changes MUST remain scoped to an approved requirement or defect. New packages MUST have
	a documented purpose, compatible license, and vulnerability review. Known applicable high
	or critical vulnerabilities MUST block adoption or release until remediated.

### V. Repeatable Verification

- Every behavior change MUST include reproducible acceptance steps and recorded results.
	MVP verification MAY be manual; introducing a test framework is not an MVP prerequisite.
- MVP acceptance MUST exercise add/list through the browser and backend API, add multiple
	supplied URLs, confirm the list updates without reloading, and confirm no outbound feed
	requests occur. A markup-like input MUST display as text without execution.
- Once automated tests exist, relevant tests MUST pass for each change. From Extended-MVP
	onward, new backend behavior MUST have xUnit unit or integration coverage, including manual
	refresh success, fetch failure, RSS/Atom parsing, and the security rejection cases above.
	Parser tests MUST use deterministic local fixtures; live-feed checks MUST NOT be the only
	evidence of correctness.
- Clean builds and browser smoke checks MUST pass before declaring a phase complete.
	Unrun or failed checks MUST be reported explicitly and MUST NOT be described as passing.

## Technology and Runtime Constraints

- The project MUST use ASP.NET Core Web API and Blazor WebAssembly on a supported .NET SDK.
	Setup documentation MUST identify the required SDK and restore, build, and run commands.
- Runtime code and required setup steps MUST support Windows, macOS, and Linux. OS-specific
	tooling MUST have documented equivalents; machine-specific absolute paths MUST NOT be
	required. Verification records MUST identify which OS was actually tested.
- Backend and frontend MUST run on separate configured localhost ports. The initial defaults
	are backend `http://localhost:5151` and frontend `http://localhost:5213`.
- Frontend `wwwroot/appsettings.json` MUST supply `ApiBaseUrl`, initially
	`http://localhost:5151/api/`. API URLs MUST NOT be hardcoded in UI components. Port changes
	MUST keep launch settings, API base URL, and the backend CORS allowlist consistent.
- Extended-MVP MUST use manual refresh, title/link display, and a basic failed-to-load message.
	Automatic polling, EF Core/SQLite persistence, full-content rendering, and other post-MVP
	features MUST remain deferred until separately approved.

## Development Workflow and Quality Gates

1. Before planning or implementation, identify the delivery phase and acceptance criteria in
	 the feature specification. Check scope against `StakeholderDocuments/ProjectGoals.md`,
	 `StakeholderDocuments/AppFeatures.md`, and `StakeholderDocuments/TechStack.md`. Plans MUST
	 record constitution compliance and resolve conflicts before feature work starts.
2. During Phase 2 (Foundational), remove Blazor template Home, Counter, and Weather pages and
	 their navigation links before feature UI work. Verify no duplicate routes remain; there
	 MUST be exactly one root route when the subscription page is added. Run a clean frontend
	 build and a browser routing check at the foundation gate and after adding the root page.
3. Keep each change tied to its specification or defect. Update affected setup documentation
	 and configuration instructions with the change; do not perform unrelated cleanup.
4. Before merge, run dependency restore, build affected projects, execute existing relevant
	 tests, and record the acceptance results. New compiler warnings and failing required checks
	 MUST be resolved. Review dependency additions and trust-boundary changes explicitly.
5. Before MVP sign-off, run both applications and verify listener ports, frontend API base URL,
	 CORS, add/list behavior, and absence of browser connection or routing errors. Before
	 Extended-MVP sign-off, additionally verify manual refresh with a known-good feed such as
	 `https://devblogs.microsoft.com/dotnet/feed/` and verify the basic failure message.
6. A maintainer MUST review compliance evidence before merge or phase sign-off. For this
	 single-user POC, a recorded self-review is sufficient; additional approval roles are not
	 required.

## Governance

This constitution governs project engineering decisions. Feature documents supply requirements
but MUST NOT silently override these principles. Conflicts MUST be documented and resolved by
the project maintainer through an explicit requirement correction or constitution amendment
before conflicting implementation proceeds.

Amendments MUST state the reason, affected principles, compatibility impact, and any migration
or follow-up work. The maintainer MUST approve the amendment and record the decision in its
change review. Plans and reviews MUST recheck affected requirements after approval. Temporary
exceptions MUST name the rule, rationale, compensating controls, owner, and expiry or removal
condition and receive the same explicit approval; they MUST NOT be implicit waivers.

Constitution versions MUST use semantic versioning: MAJOR for incompatible principle removal
or redefinition, MINOR for new principles or materially expanded guidance, and PATCH for
non-semantic clarifications. This first project-specific adoption is version 1.0.0. The
ratification date MUST remain the original adoption date; the last-amended date MUST reflect
each substantive update in ISO YYYY-MM-DD format.

Each plan and change review MUST record compliance, approved exceptions, and required follow-up
work. The temporary Sync Impact Report MUST be reviewed and removed before the constitution
amendment is committed; it is not governance content. Dependent templates and commands consume
this constitution at runtime and MUST NOT be edited as part of this constitution-only workflow.

**Version**: 1.0.0 | **Ratified**: 2026-09-13 | **Last Amended**: 2026-09-13
