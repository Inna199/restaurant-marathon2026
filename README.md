## Homework: Owner view of group pre-order sessions

Implemented `GET /guest/groups/{groupReservationId}/sessions`.

- Only the owner of the reservation can call it (`401` without a token, `403` for another user, `404` if the group does not exist).
- Returns all guest sessions of the group: participant `id`, `type`, `name`, `items` (`dish` with `id`, `name`, `price`, and `quantity`), per-participant `total`, `isSubmitted`, and the overall `total`.

### Changed files
- `backend/restaurant-backend-app/deployment_resources.json` (route path `/guest/groups/{groupReservationId}/sessions`)
- `backend/restaurant-backend-app/dnapp/lambdas/api-handler/Services/GuestSessions/GuestSessionService.cs`
- `backend/restaurant-backend-app/dnapp/lambdas/api-handler/Models/GuestSessions/GetAllResponse.cs`
- `backend/restaurant-backend-app/dnapp/lambdas/api-handler/Models/GuestSessions/ParticipantItemResponse.cs` (new)
- `backend/restaurant-backend-app/Tests/Handlers/GetGuestSessionsHandlerTests.cs`

### Actual response
```json
{
    "participants": [
        {
            "id": "b4da8242-dd0a-4f46-a7a8-dbde2db8c489",
            "type": "Guest",
            "name": "Olena",
            "items": [
                {
                    "dish": {
                        "id": "79528671-b65f-4754-9c37-b056cef6c883",
                        "name": "Tom Yum",
                        "price": 220
                    },
                    "quantity": 1
                }
            ],
            "total": 220,
            "isSubmitted": false
        },
        {
            "id": "086bd34b-e848-4236-92b3-d896a1974bc8",
            "type": "Guest",
            "name": "Andrii",
            "items": [
                {
                    "dish": {
                        "id": "d31862e1-87ad-42af-923a-2230098c9f86",
                        "name": "Caesar Salad",
                        "price": 180
                    },
                    "quantity": 2
                }
            ],
            "total": 360,
            "isSubmitted": true
        }
    ],
    "total": 580
}
```

### Tests
`dotnet test backend/restaurant-backend-app/Tests/APIHandler.Tests.csproj` (153 tests pass).