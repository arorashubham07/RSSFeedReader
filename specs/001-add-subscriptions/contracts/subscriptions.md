# Contract: Local Subscription API and UI

**Date**: 2026-09-13 | **Feature**: [spec.md](../spec.md)

This is the implementation contract for the MVP, not an implemented service. The only business
operations are list and add. See [data-model.md](../data-model.md) for state ownership and
[quickstart.md](../quickstart.md) for executable acceptance requests.

## Addressing and Transport

- API origin: `http://localhost:5151`; route: `/api/subscriptions`.
- UI origin: `http://localhost:5213`; root route `/`.
- UI configuration key: `ApiBaseUrl`, value `http://localhost:5151/api/`. Resolve relative
  `subscriptions` against that base, not `/subscriptions`, which would discard `/api/`.
- Responses containing lists use `application/json` and `Cache-Control: no-store`. JSON string
  escaping may differ on the wire; decoded characters and whitespace must remain identical.
- No credentials, session cookies, user IDs, external destinations, or URL values in paths or
  query strings. No authentication system is required for the loopback-only POC.
- Both listeners must bind only loopback. A JSON contract and CORS do not authenticate local
  processes; remote hosting is excluded.

## GET /api/subscriptions

**Request**: No body, no parameters.

**200 OK**: A nonnull JSON string array containing a consistent snapshot in append order.

```json
["https://example.com/feed", "  not a URL  ", "https://example.com/feed"]
```

Fresh backend processes return `[]`. GET must never fetch feeds or mutate state. The client
must treat non-success responses, null bodies, malformed JSON, or null/non-string elements
as a failed read, not as an empty list. No pagination or filtering is exposed.

## POST /api/subscriptions

**Request**: Content-Type `application/json`; object with `url` string property.

```json
{"url":"  https://example.com/feed  "}
```

`url` is semantically required and must contain a non-whitespace character. Case-insensitive
property matching and ignored unknown properties follow ASP.NET Core web JSON defaults; client
requests use the exact lower-case `url` name. No accepted string is trimmed or interpreted as
a URI. Duplicate values are accepted.

| Result | Meaning | Mutation |
|--------|---------|----------|
| 204 No Content | One original string appended; no response body or Location header | Exactly one append for this request |
| 400 Bad Request | Missing/null/blank `url`, null/absent body, malformed JSON, or wrong JSON shape/type | None |
| 415 Unsupported Media Type | Body sent with a media type other than application/json (optional charset permitted) | None |
| 5xx or interrupted response | Unexpected failure; caller cannot infer whether append occurred | Outcome may be unknown |

Invalid bodies must be rejected before accessing mutation logic. GET/POST are the only business
methods; PUT/PATCH/DELETE return 405. Form-urlencoded, multipart, and text/plain POSTs must not
write state. No automatic retry or idempotency key is provided. Two intentional identical
requests create two entries.

Error bodies have no required schema; clients use status codes and never display server detail
verbatim. Do not include submitted values or exception internals in application error responses.

## CORS

Use a named global policy allowing only `http://localhost:5213`, methods GET/POST, and header
Content-Type, without credentials. Apply it in the correct middleware order before endpoints
(after explicit routing if used). Middleware answers OPTIONS preflights.

For an allowed JSON POST preflight, expect a successful empty response (normally 204) with
Access-Control-Allow-Origin equal to the exact UI origin, POST among allowed methods, and
Content-Type among allowed headers. Disallowed origins must receive no allow-origin grant;
the browser must block the cross-origin JSON POST. A CORS denial is not necessarily an HTTP
403. Simple cross-origin form POSTs must independently be rejected by media-type enforcement.

Do not use wildcard origins, allow credentials, or redirect these HTTP endpoints to HTTPS.
Changing a port requires coordinated launch settings, ApiBaseUrl, and CORS changes. Using
127.0.0.1 in the browser instead of localhost changes the origin and is not implicitly allowed.

## UI Behavior

- Render a subscriptions heading, an explicitly labelled plain text entry control, Add, and
  one unframed list. No demo navigation or additional feature actions. Use a text-preserving
  control; a textarea can preserve pasted line breaks as well as surrounding spaces without
  applying URL validation. No input pattern, maxlength, or URI-specific browser constraint.
- Begin with a GET. Serialize initialization and Add workflows using one busy guard. Disable
  input/Add while busy and make blank input a no-op. Each Add captures the original text,
  awaits POST, checks 204, then awaits GET and replaces the list with the confirmed snapshot.
- On rejected POST, preserve list and input and show a short generic failure state. On an
  interrupted POST, state that the outcome is unconfirmed; do not report a definite failure or
  retry automatically. On successful POST followed by failed GET, preserve the old snapshot
  and indicate that the list could not be updated. These are local operation states, not a
  detailed feed-error feature. Clear input only after the complete successful workflow.
- On initial GET failure, distinguish load failure from a genuinely empty successful snapshot.
  Reloading the page after recovery obtains authoritative state; there is no added Refresh
  business action, background polling, or cross-tab push synchronization.
- Render list entries as ordinary Razor text, never HTML, anchors, image sources, or script.
  Preserve visible whitespace and wrap long tokens (`white-space: break-spaces` and
  `overflow-wrap: anywhere`); allow list items to shrink within their container. Keep controls
  readable and unobscured at desktop and narrow mobile widths. Do not key entries by URL.

## Requirement Coverage

| Spec requirements | Contract coverage |
|-------------------|-------------------|
| FR-001, FR-002 | Loopback addresses, root subscription view, empty startup list, no sign-in |
| FR-003, FR-006 | Confirmed POST/GET workflow, original strings, order and duplicates |
| FR-004, FR-005 | Presence-only acceptance; no destination contacts; inert rendering |
| FR-007 | Blank no-op in UI and 400 without mutation at API boundary |
| FR-008 | Backend process lifetime and no-store snapshots on reload |
| FR-009 | Distinct failed/uncertain operation states; preserve confirmed snapshot |
| FR-010 | Atomic append and copied snapshots under one lock |
| FR-011 | No truncation; whitespace and long-token display constraints |
| FR-012 | Only add/list; other business methods/actions absent |