using System.Text.Json.Serialization;

namespace APIHandler.Models.GuestSessions;

public record ParticipantDishResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("price")] decimal Price);

public record ParticipantItemResponse(
    [property: JsonPropertyName("dish")] ParticipantDishResponse Dish,
    [property: JsonPropertyName("quantity")] int Quantity);