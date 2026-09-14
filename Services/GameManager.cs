using Microsoft.AspNetCore.SignalR;
using MuscleRivalsBackend.Data.Lists;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Hubs;

namespace MuscleRivalsBackend.Services;

public class GameManager(MatchmakingRoomsList matchmakingRoomsManager, GameHubConnectionList hubConnectionList, IHubContext<GameHub> gameHubContext, ILogger<GameManager> logger)
{

    private readonly MatchmakingRoomsList _matchmakingRoomsManager = matchmakingRoomsManager;
    private readonly GameHubConnectionList _hubConnectionList = hubConnectionList;
    private readonly IHubContext<GameHub> _gameHubContext = gameHubContext;

    private readonly ILogger<GameManager> _logger = logger;


    public void StartRoom(List<int> userIds)
    {
        _matchmakingRoomsManager.CreateRoom(userIds);

    }



    /// <summary>
    ///     Client invoked actions
    /// </summary>
    /// <param name="action"></param>
    /// <returns></returns>
    public async void ClientAction(int userId, PlayerAction action)
    {

        switch (action)
        {
            case PlayerAction.countRep:
                break;
            case PlayerAction.exitGame:
                break;
            case PlayerAction.reinitializeWebRTC:
                break;
        }

    }




    // private async void StartMatch(List<int> userIds)
    // {
    //     await _gameHubContext.Clients.Users(userIds.Select(x => x.ToString())).SendAsync("StartMatch");
    // }

    /// <summary>
    ///     Initializes WebRTC between clients in the room, so far only support for two clients
    /// </summary>
    /// <param name="userIds"></param>
    /// <returns></returns>
    private async Task InitializeWebRTC(int roomId, CancellationToken cancellationToken)
    {
        List<int> playerIds = _matchmakingRoomsManager.GetRoomMembers(roomId);
        if (playerIds.Count != 2)
        {
            EndMatch(roomId, "Not enough players");
            return;
        }


        // Get first user offer
        string? firstPlayerConnectionId = _hubConnectionList.GetConnection(playerIds[0]);
        if (firstPlayerConnectionId == null)
        {
            EndMatch(roomId, "First player lost connection");
            return;
        }

        try
        {

            string offer = await _gameHubContext.Clients.Client(firstPlayerConnectionId).InvokeAsync<string>("GetWebRTCOffer", cancellationToken);

            // Get second user answer
            string? secondPlayerConnectionId = _hubConnectionList.GetConnection(playerIds[1]);
            if (secondPlayerConnectionId == null)
            {
                EndMatch(roomId, "Second player lost connection");
                return;
            }
            string answer = await _gameHubContext.Clients.Client(secondPlayerConnectionId).InvokeAsync<string>("GetWebRTCAnswer", offer, cancellationToken);


            // Sending back answer to first player
            await _gameHubContext.Clients.Client(firstPlayerConnectionId).SendAsync("SetWebRTCAnswer", answer, cancellationToken);
        }
        catch (Exception ex)
        {

            EndMatch(roomId, "Failed to initialize WebRTC");
            _logger.LogError("Failed to initialize WebRTC for room {RoomId} : {Exception}", roomId, ex);
            throw;
        }

    }

    private async void EndMatch(int roomId, string reason)
    {
        await _gameHubContext.Clients.Users(_matchmakingRoomsManager.GetRoomMembers(roomId).Select(x => x.ToString())).SendAsync("EndMatch", reason);

    }

}