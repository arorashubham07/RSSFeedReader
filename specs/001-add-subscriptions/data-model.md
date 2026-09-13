# Data Model: Add Feed Subscriptions

**Date**: 2026-09-13 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Subscription

One occurrence of a user-supplied string intended to identify a feed. The internal element
is a `string`; it needs no persistent ID, name, timestamp, feed metadata, or content model.

| Attribute | Type | Rules |
|-----------|------|-------|
| Value | Nonnull string | Must contain a non-whitespace character; preserve the entire original value without trimming or normalization. |
| Position | Implicit list index | Reflects successful append order; not a stable public identifier. |

Identical strings are separate occurrences, not the same entity. No removal, update, or
individual lookup is exposed. A 2,048-character value is a required acceptance case, not a
maximum length or a reason to add a length validator.

## Subscription List

One ordered collection per backend process, initially empty. The concrete SubscriptionStore
owns a private `List<string>` and private lock. Register it as a singleton in dependency
injection. All mutations and snapshot copies use the same lock. Return a new `string[]` on
every read and serialize outside the lock. Never return a live mutable list or wrapper.

**Relationship**: One list contains zero or more subscription occurrences. No user record,
database, filesystem, browser storage, or durable cache participates.

**Concurrency**: Each append is atomic. Two successful overlapping appends each contribute one
occurrence. Their relative lock-acquisition order is acceptable; earlier completed sequential
appends retain their order. A snapshot contains all appends completed before its read lock is
acquired and none of any partially performed append.

## Request and Response Shapes

| Shape | Fields | Purpose |
|-------|--------|---------|
| AddSubscriptionRequest | `url`: nullable string at the binding boundary | Explicit JSON-body request; nullability permits controlled presence rejection. |
| Subscription snapshot | JSON array of strings | Current list, retaining order and duplicate values; never null. |
| Error | HTTP status; no body schema consumed by the UI | Failure must not echo submitted values or imply that an unconfirmed add succeeded. |

GET and POST details are in [contracts/subscriptions.md](contracts/subscriptions.md).
Only the backend needs a named request DTO. The UI may serialize an object with the same
`url` property; a shared contract assembly is unnecessary.

## Validation Rules

- Apply `string.IsNullOrWhiteSpace` only for presence; store the original nonblank value.
- No URL attribute, browser URL input, regex, URI conversion, feed inspection, de-duplication,
  or whitespace trimming is permitted.
- Missing/null/blank `url` is a 400 response with no mutation. Malformed JSON and non-string
  `url` are invalid transport shapes and also have no mutation. Wrong media types yield 415.
- UI blank input never sends POST. Arbitrary nonblank values, including markup-like text,
  remain acceptable and are rendered only as text.
- No logging of full input strings, request bodies, or snapshot bodies is permitted.

## State Transitions

| Event | Backend state | Visible state |
|-------|---------------|---------------|
| Backend starts | New empty list | Initial GET obtains empty snapshot. |
| Initial GET succeeds | Unchanged | Replace displayed list with decoded snapshot. |
| Blank input Add | No request from UI | Existing list unchanged. |
| Valid POST succeeds | Append original value once, return 204 | Await subsequent GET before replacing list; no optimistic append. |
| POST rejected before mutation | Unchanged | Retain confirmed list and input; show a generic failure state. |
| POST interrupted | May or may not have appended | Retain confirmed list; indicate unconfirmed outcome; no retry. |
| GET fails after successful POST | New entry remains in store | Retain previous snapshot; indicate list could not be updated. |
| Browser reload or frontend restart | Unchanged | Obtain a fresh snapshot; recover from stale/uncertain display. |
| Backend stops and restarts | Previous list discarded | Next successful GET replaces display with empty list. |

The UI owns only `input`, `confirmedSubscriptions`, `isBusy`, and a small operation-status
value. Set `isBusy` before the first await of initial load or Add, disable input/Add while
busy, and reset the guard in a finally path. This serializes page workflows and prevents stale
GET responses from replacing later state. Do not use submitted text as a unique Razor key,
because duplicates are valid. The list is a snapshot, not a live multi-client synchronization
feature; another tab's additions appear on the next normal GET or page reload.