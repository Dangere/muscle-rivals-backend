using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MuscleRivalsBackend.Attributes;
using MuscleRivalsBackend.Data.Managers;
using MuscleRivalsBackend.Enums;
namespace MuscleRivalsBackend.Hubs;

/// <summary>
///     Hub used to add users to signalR
/// </summary>
/// <param name="logger"></param>
/// <param name="inMemoryConnectionManager"></param>
[AuthorizeRoles(UserRoles.User, UserRoles.Admin)]
public class MatchmakingHub(ILogger<MatchmakingHub> logger, MatchmakingHubConnectionManager inMemoryConnectionManager) : Hub
{
    private readonly ILogger<MatchmakingHub> _logger = logger;
    private readonly MatchmakingHubConnectionManager _inMemoryConnectionManager = inMemoryConnectionManager;

    public override async Task OnConnectedAsync()
    {
        var deviceId = Context.GetHttpContext()?.Request.Query["deviceId"].ToString();
        int userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _logger.LogInformation("A client has connected, UserId: {UserId} DeviceId: {DeviceId}", userId, deviceId);
        _inMemoryConnectionManager.AddConnection(userId, Context.ConnectionId, deviceId ?? userId.ToString());
        await base.OnConnectedAsync();
    }

}