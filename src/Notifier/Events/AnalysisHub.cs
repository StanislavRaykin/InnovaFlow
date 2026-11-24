using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
namespace Notifier.Events;

// Reject unauthenticated connections. Without this a bad token still connects,
// but with no user id, so Clients.User(...) never reaches it.
[Authorize]
public sealed class AnalysisHub : Hub{}

/// <summary>
/// Clients.User(id) matches on this value. The default provider reads ClaimTypes.NameIdentifier,
/// which doesn't exist here because MapInboundClaims = false keeps the JWT's raw "sub" claim.
/// </summary>
public sealed class SubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
