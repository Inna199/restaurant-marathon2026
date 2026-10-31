using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Handlers.GuestSessions;
using APIHandler.Models.GuestSessions;
using APIHandler.Models.Orders;
using APIHandler.Services.GuestSessions;
using Moq;
using System.Text.Json;

namespace APIHandler.Tests.Handlers;

public class GetGuestSessionsHandlerTests
{
    private const string GroupReservationId = "group-1";
    private const string UserId = "owner-sub";

    private readonly Mock<IGuestSessionService> _guestSessionService = new();
    private readonly Mock<ILambdaLogger> _logger = new();
    private readonly ILambdaContext _context;
    private readonly GetGuestSessionsHandler _handler;

    public GetGuestSessionsHandlerTests()
    {
        var context = new Mock<ILambdaContext>();
        context.Setup(lambdaContext => lambdaContext.Logger).Returns(_logger.Object);
        _context = context.Object;
        _handler = new GetGuestSessionsHandler(_guestSessionService.Object);
    }

    [Fact]
    public async Task HandleRequest_WhenGroupReservationIdIsMissing_ReturnsBadRequest()
    {
        var request = new APIGatewayProxyRequest();

        var response = await _handler.HandleRequest(request, _context);

        Assert.Equal(400, response.StatusCode);
        _guestSessionService.Verify(
            service => service.GetGuestSessionsForOwnerAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleRequest_WhenSubClaimIsMissing_ReturnsUnauthorized()
    {
        var request = CreateRequest(claims: new Dictionary<string, string>());

        var response = await _handler.HandleRequest(request, _context);

        Assert.Equal(401, response.StatusCode);
        _guestSessionService.Verify(
            service => service.GetGuestSessionsForOwnerAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleRequest_WhenSubClaimIsBlank_ReturnsUnauthorized()
    {
        var request = CreateRequest(new Dictionary<string, string> { ["sub"] = " " });

        var response = await _handler.HandleRequest(request, _context);

        Assert.Equal(401, response.StatusCode);
        _guestSessionService.Verify(
            service => service.GetGuestSessionsForOwnerAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleRequest_WhenServiceForbidsCaller_ReturnsForbidden()
    {
        _guestSessionService
            .Setup(service => service.GetGuestSessionsForOwnerAsync(UserId, GroupReservationId))
            .ThrowsAsync(new ActionForbiddenException("Only the reservation owner can perform this action."));

        var response = await _handler.HandleRequest(CreateAuthorizedRequest(), _context);

        Assert.Equal(403, response.StatusCode);
        using var body = JsonDocument.Parse(response.Body);
        Assert.Equal(
            "Only the reservation owner can perform this action.",
            body.RootElement.GetProperty("error").GetString());
        _guestSessionService.Verify(
            service => service.GetGuestSessionsForOwnerAsync(UserId, GroupReservationId),
            Times.Once);
    }

    [Fact]
    public async Task HandleRequest_WhenGroupReservationIsMissing_ReturnsNotFound()
    {
        _guestSessionService
            .Setup(service => service.GetGuestSessionsForOwnerAsync(UserId, GroupReservationId))
            .ThrowsAsync(new GroupReservationNotFoundException());

        var response = await _handler.HandleRequest(CreateAuthorizedRequest(), _context);

        Assert.Equal(404, response.StatusCode);
        _guestSessionService.Verify(
            service => service.GetGuestSessionsForOwnerAsync(UserId, GroupReservationId),
            Times.Once);
    }

    [Fact]
    public async Task HandleRequest_WhenParentReservationIsMissing_ReturnsNotFound()
    {
        _guestSessionService
            .Setup(service => service.GetGuestSessionsForOwnerAsync(UserId, GroupReservationId))
            .ThrowsAsync(new ReservationNotFoundException("Parent reservation not found."));

        var response = await _handler.HandleRequest(CreateAuthorizedRequest(), _context);

        Assert.Equal(404, response.StatusCode);
        _guestSessionService.Verify(
            service => service.GetGuestSessionsForOwnerAsync(UserId, GroupReservationId),
            Times.Once);
    }

    [Fact]
    public async Task HandleRequest_WhenOwnerRequestSucceeds_ReturnsGuestSessionsResponse()
    {
        var result = new GetAllSessionsResponse(
            [
                new Participant(
                    "guest-session-1",
                    ParticipantType.Guest,
                    "Guest One",
                    [
                        new ParticipantItemResponse(
                            new ParticipantDishResponse("dish-1", "Soup", 4.25m),
                            2)
                    ],
                    8.50m,
                    true)
            ],
            8.50m);
        _guestSessionService
            .Setup(service => service.GetGuestSessionsForOwnerAsync(UserId, GroupReservationId))
            .ReturnsAsync(result);

        var response = await _handler.HandleRequest(CreateAuthorizedRequest(), _context);

        Assert.Equal(200, response.StatusCode);
        using var body = JsonDocument.Parse(response.Body);
        var root = body.RootElement;
        Assert.Equal(8.50m, root.GetProperty("total").GetDecimal());
        var participant = Assert.Single(root.GetProperty("participants").EnumerateArray());
        Assert.Equal("guest-session-1", participant.GetProperty("id").GetString());
        Assert.Equal("Guest", participant.GetProperty("type").GetString());
        Assert.Equal("Guest One", participant.GetProperty("name").GetString());
        Assert.True(participant.GetProperty("isSubmitted").GetBoolean());
        Assert.Equal(8.50m, participant.GetProperty("total").GetDecimal());
        var item = Assert.Single(participant.GetProperty("items").EnumerateArray());
        Assert.Equal("dish-1", item.GetProperty("dish").GetProperty("id").GetString());
        Assert.Equal(4.25m, item.GetProperty("dish").GetProperty("price").GetDecimal());
        Assert.Equal(2, item.GetProperty("quantity").GetInt32());
        _guestSessionService.Verify(
            service => service.GetGuestSessionsForOwnerAsync(UserId, GroupReservationId),
            Times.Once);
    }

    private static APIGatewayProxyRequest CreateAuthorizedRequest()
        => CreateRequest(new Dictionary<string, string> { ["sub"] = UserId });

    private static APIGatewayProxyRequest CreateRequest(Dictionary<string, string> claims)
        => new()
        {
            PathParameters = new Dictionary<string, string> { ["groupReservationId"] = GroupReservationId },
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                Authorizer = new APIGatewayCustomAuthorizerContext { Claims = claims }
            }
        };
}
