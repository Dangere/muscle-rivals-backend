using MuscleRivalsBackend.Data.Lists;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Models.Matchmaking;
using MuscleRivalsBackend.Utilities;

namespace MuscleRivalsBackend.Services;

public class MatchmakingService(GameHubConnectionList hubConnectionList, RoomsList matchmakingRoomsList, MatchmakingQueueList matchmakingQueueList)
{

    private readonly GameHubConnectionList _hubConnectionList = hubConnectionList;
    private readonly RoomsList _matchmakingRoomsList = matchmakingRoomsList;
    private readonly MatchmakingQueueList _matchmakingQueueList = matchmakingQueueList;


    /// <summary>
    ///     Adds the the user to the queue to find a match
    /// </summary>
    /// <returns></returns>
    /// Checks first if the user is in the signalR matchmaking hub before adding them to the queue
    public async Task<Result<string>> EnterQueue(int userId)
    {
        // Making sure player isn't already in queue
        if (_matchmakingQueueList.UserInQueue(userId))
        {
            return Result<string>.Error("User is already in queue", ErrorCodes.USER_ALREADY_IN_QUEUE);
        }

        // Making sure the user is in the hub before adding them to the queue, this should not happen from the clients side but just in case
        if (!_hubConnectionList.UserInHub(userId))
        {
            return Result<string>.Error("User is not in the hub", ErrorCodes.USER_NOT_IN_HUB);
        }

        // Making sure the user isn't in a room already in case of any disconnects during room creating
        if (_matchmakingRoomsList.UserInRoom(userId) != null)
        {
            Console.WriteLine($"User is already in a room, {_matchmakingRoomsList.UserInRoom(userId)}");
            return Result<string>.Error("User is already in a room", ErrorCodes.USER_ALREADY_IN_ROOM);
        }

        _matchmakingQueueList.Enqueue(new(userId, ExerciseType.Pushups, GameModeType.MaxReps, 0.45));



        return Result<string>.Success("success");
    }

    /// <summary>
    ///     Allows the user to exit the queue
    /// </summary>
    /// <returns></returns> 
    public Result<string> LeaveQueue(int userId)
    {

        _matchmakingQueueList.LeaveQueue(userId);

        return Result<string>.Success("test");
    }
}