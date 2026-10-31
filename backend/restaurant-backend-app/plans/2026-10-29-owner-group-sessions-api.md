# Owner Group Pre-order Sessions API

**Status:** Awaiting approval
**Created:** 2026-10-29

## Requirements

Implement `GET /guest/groups/{groupReservationId}/sessions` so the reservation owner can see the guest members who joined the group pre-order, each guest's dishes and quantities, each guest's total, submission state, and the total across all guest members. Match the response shape supplied in the request.

## Scope and exclusions

- Add the endpoint to the Lambda route registry and implement an authenticated request handler.
- Enforce reservation ownership using the authenticated Cognito `sub` claim and the existing owner-validation pattern.
- Reuse the `GetAllSessionsResponse` and participant models, and fetch dish details for guest session items.
- Return guest sessions only; exclude the reservation owner's separate session from `participants` and the aggregate.
- Correct the API route documentation to describe the required response shape.
- Do not change other group pre-order behavior, add deployment resources, deploy, or commit.

## Findings and design

- `Routing/RouteRegistry.cs` registers Lambda routes; the requested GET route is currently absent. The related DELETE route is registered there.
- `DeleteGuestSessionHandler` demonstrates path-parameter and Cognito `sub` handling. `GuestSessionService.ValidateReservationOwnerAsync` already checks that the authenticated user owns the parent reservation and throws the established not-found/forbidden exceptions.
- `GuestSessionService.DeleteGuestSessionAsync` already loads sessions and dish details and calculates participant and aggregate totals, but its current projection includes all sessions as `Guest`. The new read path should return guest sessions only and must not expose or count the owner session.
- `GetAllSessionsResponse`, `Participant`, and `OrderItemResponse` already represent the requested JSON response. `docs/ApiRoutes.md` has an entry for this GET path, but its documented response is a stale array that does not match the requirement.
- `dnapp/lambdas/api-handler/deployment_resources.json` contains the Lambda IAM role only; it does not define HTTP routes. No separate route-specific deployment change is planned.

## Implementation steps

1. Add a guest-session service operation that validates reservation ownership, loads sessions by group reservation ID, excludes owner sessions, retrieves dish records, and maps each guest to items, computed total, and submission state; compute the aggregate from those guest totals.
2. Add a GET handler following the existing authenticated owner handler pattern, including explicit handling for missing path/auth data and established not-found/forbidden exceptions.
3. Register `GET /guest/groups/{groupReservationId}/sessions` in `RouteRegistry.cs`.
4. Update the endpoint documentation with the exact `{ "participants": [...], "total": ... }` response shape.
5. Add service and handler tests for owner access, non-owner and missing-resource errors, guest-only results, dish mapping, per-guest and aggregate totals, empty sessions, and submission state.

## Test expectations

- Run the focused `GuestSessionServiceTests` and handler/API test coverage using the repository's .NET test project.
- Build the API handler project if the focused test run does not compile all production sources.

## Acceptance criteria

- The route is reachable at `GET /guest/groups/{groupReservationId}/sessions`.
- Requests without an authenticated `sub` are rejected; only the parent reservation owner can retrieve the response.
- A missing group reservation (or missing parent reservation) returns the established 404 response; a non-owner receives 403.
- The response uses the supplied shape and contains only guest participants, their dish details, quantities, totals, and `isSubmitted` values. The top-level `total` equals the sum of guest totals and excludes the owner session.
- The documentation and targeted tests reflect these behaviors.

## Assumptions

- “Group members” in the requested participant list means joined guest sessions, not the owner's separate session; this is consistent with the supplied `Guest` participant examples and the owner's purpose as the viewer.
- Existing API handler middleware/API Gateway configuration provides Cognito claims to authenticated routes; the handler will enforce the claim and ownership using existing patterns.
- Missing path parameter returns 400, unauthenticated callers return 401, non-owners return 403, missing resources return 404, and unexpected errors continue through the existing exception handling.
