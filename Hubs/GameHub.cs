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
public class GameHub(ILogger<GameHub> logger, GameHubConnectionList inMemoryConnectionManager, GameManager gameManager, MatchmakingService matchmakingService) : Hub
{
    private readonly ILogger<GameHub> _logger = logger;
    private readonly GameHubConnectionList _inMemoryConnectionManager = inMemoryConnectionManager;

    private readonly Action<int, ClientAction> _playerActionDelegate = gameManager.ClientAction;
    internal void OnUserDisconnect(int userId)
    {
        gameManager.ClientDisconnect(userId);
        matchmakingService.LeaveQueue(userId);

    }

    public override async Task OnConnectedAsync()
    {
        var deviceId = Context.GetHttpContext()?.Request.Query["deviceId"].ToString();
        if (!int.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "fail", out int userId))
        {
            throw new HubException("User ID not found in token");
        }
        _logger.LogInformation("A client has connected, UserId: {UserId} DeviceId: {DeviceId}", userId, deviceId);

        // If user is already in hub we remove his initial connection id then add the new one after kicking him from any room
        if (_inMemoryConnectionManager.UserInHub(userId))
            OnUserDisconnect(userId);

        _inMemoryConnectionManager.SetConnection(userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        int userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _inMemoryConnectionManager.RemoveConnections(userId);

        OnUserDisconnect(userId);

        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    ///     Server function called by the client to count a rep in the room they're in
    /// </summary>
    /// <returns></returns>
    public async Task CountRep()
    {
        int userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _playerActionDelegate(userId, ClientAction.CountRep);
    }

    /// <summary>
    ///     Server function called by the client to exit the room
    /// </summary>
    /// <returns></returns>
    public async Task QuitMatch()
    {
        int userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _playerActionDelegate(userId, ClientAction.QuitMatch);
    }

    /// <summary>
    ///     Server function to request to reinitialize WebRTC in case it fails during the match for any reason
    ///     It shares ICE candidate and SDP information between the clients in two way trips
    /// </summary>
    /// <returns></returns>
    public async Task ReinitializeWebRTC()
    {
        int userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        _playerActionDelegate(userId, ClientAction.ReinitializeWebRTC);

    }

}