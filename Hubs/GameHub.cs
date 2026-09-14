using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using MuscleRivalsBackend.Attributes;
using MuscleRivalsBackend.Data.Lists;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Services;
namespace MuscleRivalsBackend.Hubs;

/// <summary>
///     Hub used to add users to signalR
/// </summary>
/// <param name="logger"></param>
/// <param name="inMemoryConnectionManager"></param>
[AuthorizeRoles(UserRoles.User, UserRoles.Admin)]
public class GameHub(ILogger<GameHub> logger, GameHubConnectionList inMemoryConnectionManager, GameManager gameManager) : Hub
{
    private readonly ILogger<GameHub> _logger = logger;
    private readonly GameHubConnectionList _inMemoryConnectionManager = inMemoryConnectionManager;

    private readonly Action<int, PlayerAction> _playerActionDelegate = gameManager.ClientAction;


    public override async Task OnConnectedAsync()
    {
        var deviceId = Context.GetHttpContext()?.Request.Query["deviceId"].ToString();
        int userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _logger.LogInformation("A client has connected, UserId: {UserId} DeviceId: {DeviceId}", userId, deviceId);

        // If user is already in hub we don't add another connection ID, as only one account can be in matchmaking hub at a time
        if (_inMemoryConnectionManager.UserInHub(userId))
        {
            _logger.LogInformation("A client has tried to connect while already in hub, UserId: {UserId} DeviceId: {DeviceId}", userId, deviceId);
            return;
        }
        _inMemoryConnectionManager.AddConnection(userId, Context.ConnectionId, deviceId ?? userId.ToString());
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        int userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _inMemoryConnectionManager.RemoveConnection(userId, Context.ConnectionId);

        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    ///     Server function called by the client to count a rep in the room they're in
    /// </summary>
    /// <returns></returns>
    public async Task CountRep()
    {
        int userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _playerActionDelegate(userId, PlayerAction.countRep);
    }

    /// <summary>
    ///     Server function called by the client to exit the room
    /// </summary>
    /// <returns></returns>
    public async Task ExitRoom()
    {
        int userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _playerActionDelegate(userId, PlayerAction.exitGame);
    }

    /// <summary>
    ///     Server function to request to reinitialize WebRTC in case it fails during the match for any reason
    ///     It shares ICE candidate and SDP information between the clients in two way trips
    /// </summary>
    /// <returns></returns>
    public async Task ReinitializeWebRTC()
    {
        int userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _playerActionDelegate(userId, PlayerAction.reinitializeWebRTC);

    }

}