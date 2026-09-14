using MuscleRivalsBackend.Data.Lists;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Models.Matchmaking;
using MuscleRivalsBackend.Utilities;

namespace MuscleRivalsBackend.Services;

public class MatchmakingService(GameHubConnectionList hubConnectionList, MatchmakingRoomsList matchmakingRoomsList, MatchmakingQueueList matchmakingQueueList)
{

    private readonly GameHubConnectionList _hubConnectionList = hubConnectionList;
    private readonly MatchmakingRoomsList _matchmakingRoomsList = matchmakingRoomsList;
    private readonly MatchmakingQueueList _matchmakingQueueList = matchmakingQueueList;


    /// <summary>
    ///     Adds the the user to the queue to find a match
    /// </summary>
    /// <returns></returns>
    /// Checks first if the user is in the signalR matchmaking hub before adding them to the queue
    public async Task<Result<string>> EnterQueue(MatchEnqueue enqueue)
    {
        // Making sure the user is in the hub before adding them to the queue, this should not happen from the clients side but just in case
        if (!_hubConnectionList.UserInHub(enqueue.UserId))
        {
            return Result<string>.Error("User is not in the hub", ErrorCodes.USER_NOT_IN_HUB);
        }

        // Making sure the user isn't in a room already in case of any disconnects during room creating
        if (_matchmakingRoomsList.UserInRoom(enqueue.UserId) != null)
        {
            return Result<string>.Error("User is already in a room", ErrorCodes.USER_ALREADY_IN_ROOM);
        }

        _matchmakingQueueList.Enqueue(enqueue);



        return Result<string>.Success("test");
    }

    /// <summary>
    ///     Allows the user to exit the queue
    /// </summary>
    /// <returns></returns> 
    public async Task<Result<string>> ExitQueue()
    {
        return Result<string>.Success("test");
    }
}