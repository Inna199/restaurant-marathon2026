# Owner Group Pre-order Sessions API

**Status:** Blocked
**Created:** 2026-10-28

## Requirements
Implement `GET /guest/groups/{groupReservationId}/sessions` so the reservation owner can see the joined guest sessions, each guest's dishes, item total, submission state, and the total across all members. Return the `participants` object and aggregate `total` in the response shape supplied in the request.

## Assumptions and open questions
- The endpoint requires Cognito authentication and must verify the authenticated user owns the parent reservation.
- Return guest participants only; the owner's own session is not a joined member and is excluded from both the participant list and aggregate.
- A group reservation ID that does not exist returns 404; an authenticated non-owner receives 403.

## Design
- Endpoint: `GET /guest/groups/{groupReservationId}/sessions`; Cognito authorizer; additionally validate `sub` against the parent reservation's `CustomerId`.
- Data: use the existing `GuestSessions` DynamoDB GSI on `groupReservationId`, fetch dish details by dish ID, and calculate each participant's total and the aggregate from dish prices and quantities. Reuse `GetAllSessionsResponse` and `Participant`.
- Response: `200` with `{ "participants": [{ "id", "type": "Guest", "name", "items": [{ "dish": { "id", "name", "price" }, "quantity" }], "total", "isSubmitted" }], "total" }`.
- Errors: `400` for missing path parameter, `401` without authenticated identity, `403` for non-owner, `404` for missing group/parent reservation, `500` for unexpected failures.

## Tasks
| # | Agent | Task | Status | Result |
|---|-------|------|--------|--------|
| 1 | lambda-implementer | Implement the owner-only endpoint and route/deployment wiring; update the API route documentation. | Blocked | Agent unavailable: both invocation attempts failed because the configured `sonnet` model is not available in this environment. |
| 2 | unit-test-writer | Add service and handler coverage for ownership, guest-only results, dish totals, aggregate total, and error cases; run the targeted test suite. | Pending | |
| 3 | code-reviewer | Review the full diff against this plan and acceptance criteria. | Pending | |
| 4 | lambda-implementer | Fix any review findings, then report changed files and verification. | Pending | |

## Acceptance criteria
- [ ] `GET /guest/groups/{groupReservationId}/sessions` is registered in the API router and deployment resources with Cognito authorization.
- [ ] Only the authenticated reservation owner can retrieve the sessions.
- [ ] The response matches the requested shape and includes guests' dish details, per-guest totals, submission state, and aggregate total; the owner session is excluded.
- [ ] Missing resources, unauthenticated callers, and non-owners receive the specified error responses.
- [ ] Targeted tests pass and API route documentation matches the implementation.

## Risks
- The owner's session is stored alongside guests in the same group-reservation index; it must be filtered out to avoid exposing it as a guest or affecting the requested aggregate.
- The deployment resource file currently has a singular `/guest/group/{groupReservationId}/sessions` entry, while the requested path and existing delete route use `/guest/groups/...`; add/wire the exact requested route without changing unrelated routes.
