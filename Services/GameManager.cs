using Microsoft.AspNetCore.SignalR;
using MuscleRivalsBackend.Data.Lists;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Hubs;
using MuscleRivalsBackend.Models.DTOs.Game;
using MuscleRivalsBackend.Models.DTOs.Users;
using MuscleRivalsBackend.Models.Matchmaking;

namespace MuscleRivalsBackend.Services;

public class GameManager(RoomsList roomsManager, GameHubConnectionList hubConnectionList, IHubContext<GameHub> gameHubContext, IServiceScopeFactory scopeFactory, ILogger<GameManager> logger)
{

    private readonly RoomsList _roomsManager = roomsManager;
    private readonly GameHubConnectionList _hubConnectionList = hubConnectionList;
    private readonly IHubContext<GameHub> _gameHubContext = gameHubContext;

    private readonly ILogger<GameManager> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <summary>
    ///     Creates a match room and starts it
    /// </summary>
    public async Task StartMatch(List<int> userIds, ExerciseType exercise, GameMode mode)
    {
        if (userIds.Count != 2) throw new Exception("Room must have 2 players!");
        if (!_hubConnectionList.UsersInHub(userIds)) return;

        // Match starts paused, we have to unpause it after initializing
        int roomId = await CreateRoom(userIds, exercise, mode);

        await Task.Delay(10000);

        // Starting the game after 10 seconds
        PauseGame(roomId, false);
        await Task.Delay(5000);


        // Pausing it again after another 5 seconds 
        PauseGame(roomId, true);
        await Task.Delay(10000);


        // Starting the game after 10 seconds
        PauseGame(roomId, false);


    }



    private async Task<int> CreateRoom(List<int> userIds, ExerciseType exercise, GameMode mode)
    {
        int roomId = _roomsManager.CreateRoom(userIds, exercise, mode);
        List<UserDTO> users;

        using (var scope = _scopeFactory.CreateScope())
        {

            var userService = scope.ServiceProvider.GetRequiredService<UserService>();

            users = await userService.GetUsersDTOs(userIds);

        }

        MatchDTO match = new(roomId, users, DateTime.UtcNow, exercise, mode);

        await _gameHubContext.Clients.Users(userIds.Select(x => x.ToString())).SendAsync("StartMatch", match);

        _logger.LogInformation("Starting new match!");


        return roomId;
    }



    /// <summary>
    ///     Gets called to quit a match for any reason beside time over
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="reason"></param>
    private async void EndMatch(int roomId, string reason)
    {

        await _gameHubContext.Clients.Users(_roomsManager.GetRoomMembers(roomId).Select(x => x.ToString())).SendAsync("EndMatch", reason);
        _roomsManager.RemoveRoom(roomId);
        _logger.LogInformation("Match ended! Reason: {Reason}", reason);

    }



    private async void PauseGame(int roomId, bool pause)
    {

        _roomsManager.PauseGame(roomId, pause);
        _logger.LogInformation("Game paused: {Pause}", pause);

        await _gameHubContext.Clients.Users(_roomsManager.GetRoomMembers(roomId).Select(x => x.ToString())).SendAsync("PauseGame", pause);

    }



    /// <summary>
    ///    Client invoked actions
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="action"></param>
    public async void ClientAction(int userId, ClientAction action)
    {
        switch (action)
        {
            case Enums.ClientAction.CountRep:
                ClientCountRep(userId);
                break;
            case Enums.ClientAction.QuitMatch:
                ClientDisconnect(userId);
                break;
            case Enums.ClientAction.ReinitializeWebRTC:
                break;
            case Enums.ClientAction.ConcludeMatch:
                ClientConcludeMatch(userId);
                break;
        }

    }

    /// <summary>
    ///     Counts a rep and returns total reps for that user
    /// </summary>
    /// <param name="userId"></param>
    private async void ClientCountRep(int userId)
    {
        MatchRoom? room = _roomsManager.UserInRoom(userId);


        if (room == null)
        {
            return;
        }

        if (ValidateMatch(room) == false) return;

        if (room.IsPaused)
        {
            return;
        }


        List<int> userIds = room.UserIds.ToList();
        int newScore = _roomsManager.CountRep(room.RoomId, userId);


        // Sends the userId and their new score to the clients
        await _gameHubContext.Clients.Users(userIds.Select(x => x.ToString())).SendAsync("SetRep", userId, newScore);
    }
    /// <summary>
    ///     Concludes the match after checking a condition based on the game mode, and stores the scores for the players
    ///     For example, gets called when the time is over by the client when the timer runs out
    ///     it checks the if the match duration is actually over (with accounting to paused duration, + 2 seconds of leeway room) then ends the match
    /// </summary>
    private async void ClientConcludeMatch(int userId)
    {
        MatchRoom? room = _roomsManager.UserInRoom(userId);


        // If the room doesn't exist or the duration isn't over, return
        if (room == null)
        {
            return;
        }

        // Making sure one thread can conclude the match
        lock (room)
        {
            // If we still within active timer, we return
            // If the room was already marked as finished, we return
            if (room.ValidateMatchConcludeCondition() == true || room.IsFinished)
                return;

            room.MarkFinished();

        }
        // TODO: Store score for individual players

        // The id of the winning user
        int winnerId = room.WinningUserid();

        // Sends the winnerId to the clients
        List<int> userIds = room.UserIds.ToList();
        await _gameHubContext.Clients.Users(userIds.Select(x => x.ToString())).SendAsync("MatchConcluded", winnerId);

        _roomsManager.RemoveRoom(room.RoomId);
    }


    /// <summary>
    ///     Client disconnected so remove it from the room and end it if they're in any
    /// </summary>
    /// <param name="userId"></param>
    public async void ClientDisconnect(int userId)
    {

        MatchRoom? room = _roomsManager.UserInRoom(userId);
        if (room == null)
        {
            return;
        }

        EndMatch(room.RoomId, "One of the players disconnected");

    }

    /// <summary>
    ///     Initializes WebRTC between clients in the room, so far only support for two clients
    /// </summary>
    /// <param name="userIds"></param>
    /// <returns></returns>
    private async Task ClientInitializeWebRTC(int roomId, CancellationToken cancellationToken)
    {
        List<int> playerIds = _roomsManager.GetRoomMembers(roomId);
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


    /// <summary>
    ///     Validates match's maximum lifetime and current players and ends the match if needed 
    /// </summary>
    /// <param name="match"></param>
    /// <returns>false if the match is no longer valid</returns>
    private bool ValidateMatch(MatchRoom match)
    {
        if (!match.ValidateTotalLifespan())
        {
            EndMatch(match.RoomId, "Match duration exceeded");
            return false;
        }

        List<int> userIds = match.UserIds.ToList();

        if (!_hubConnectionList.UsersInHub(userIds))
        {
            EndMatch(match.RoomId, "One of the players disconnected");
            return false;
        }

        return true;


    }
}