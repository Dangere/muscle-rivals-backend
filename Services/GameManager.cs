using Microsoft.AspNetCore.SignalR;
using MuscleRivalsBackend.Data.Lists;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Hubs;
using MuscleRivalsBackend.Models.DTOs.Game;
using MuscleRivalsBackend.Models.DTOs.Game.Modes;
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
    public async Task StartMatch(List<int> userIds, ExerciseType exercise, GameModeType mode)
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



    private async Task<int> CreateRoom(List<int> userIds, ExerciseType exercise, GameModeType mode)
    {
        BaseGameMode gameMode = mode switch
        {
            GameModeType.TimeLimited => new TimeLimitedGameMode(),
            GameModeType.MaxReps => new MaxRepsGameMode(exercise),
            _ => throw new Exception("Invalid game mode"),
        };

        int roomId = _roomsManager.CreateRoom(userIds, exercise, gameMode);
        List<UserDTO> users;

        using (var scope = _scopeFactory.CreateScope())
        {

            var userService = scope.ServiceProvider.GetRequiredService<UserService>();

            users = await userService.GetUsersDTOs(userIds);

        }

        MatchDTO match = new(roomId, users, DateTime.UtcNow, exercise, gameMode);

        _ = _gameHubContext.Clients.Users(userIds.Select(x => x.ToString())).SendAsync("StartMatch", match).ContinueWith(t => _logger.LogError(t.Exception, "Failed to start match {RoomId}", roomId), TaskContinuationOptions.OnlyOnFaulted);

        _logger.LogInformation("Starting new match!");


        return roomId;
    }



    /// <summary>
    ///     Gets called to quit a match for any reason beside time over
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="reason"></param>
    private void EndMatch(int roomId, string reason)
    {

        _ = _gameHubContext.Clients.Users(_roomsManager.GetRoomMembers(roomId).Select(x => x.ToString())).SendAsync("EndMatch", reason).ContinueWith(t => _logger.LogError(t.Exception, "Failed to end match {RoomId}", roomId),
        TaskContinuationOptions.OnlyOnFaulted); ;
        _roomsManager.RemoveRoom(roomId);
        _logger.LogInformation("Match ended! Reason: {Reason}", reason);

    }



    private async void PauseGame(int roomId, bool pause)
    {

        _roomsManager.PauseGame(roomId, pause);
        _logger.LogInformation("Game paused: {Pause}", pause);

        _ = _gameHubContext.Clients.Users(_roomsManager.GetRoomMembers(roomId).Select(x => x.ToString())).SendAsync("PauseGame", pause).ContinueWith(t => _logger.LogError(t.Exception, "Failed to pause game {RoomId}", roomId),
        TaskContinuationOptions.OnlyOnFaulted);

    }



    /// <summary>
    ///    Client invoked actions
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="action"></param>
    public async void ClientAction(int userId, ClientAction action)
    {

        MatchRoom? room = _roomsManager.UserInRoom(userId);

        if (room == null) return;

        switch (action)
        {
            case Enums.ClientAction.CountRep:
                ClientCountRep(room, userId);
                break;
            case Enums.ClientAction.QuitMatch:
                ClientDisconnect(room);
                break;
            case Enums.ClientAction.ReinitializeWebRTC:
                break;
            case Enums.ClientAction.ConcludeMatch:
                ClientConcludeMatch(room);
                break;
        }

    }

    /// <summary>
    ///     Counts a rep and returns total reps for that user
    /// </summary>
    /// <param name="userId"></param>
    private async void ClientCountRep(MatchRoom room, int userId)
    {
        // If the room is not valid, we return
        if (ValidateMatch(room) == false) return;

        if (room.IsFinished)
        {
            return;
        }

        if (room.IsPaused)
        {
            return;
        }


        List<int> userIds = room.UserIds.ToList();
        int newScore = _roomsManager.CountRep(room.RoomId, userId);


        // Sends the userId and their new score to the clients
        _ = _gameHubContext.Clients.Users(userIds.Select(x => x.ToString())).SendAsync("SetRep", userId, newScore).ContinueWith(t => _logger.LogError(t.Exception, "Failed to set rep for user {UserId}", userId),
        TaskContinuationOptions.OnlyOnFaulted);


        // Sees if the match met the win condition which marks it as finished
        ClientConcludeMatch(room);

    }
    /// <summary>
    ///     Concludes the match after checking a condition based on the game mode, and stores the scores for the players
    ///     For example, gets called when the time is over by the client when the timer runs out
    ///     it checks the if the match duration is actually over (with accounting to paused duration) then ends the match
    ///     it also gets called on every user action to check if the match is over
    /// </summary>
    private bool ClientConcludeMatch(MatchRoom room)
    {

        int winnerId;
        // Making sure one thread can conclude the match
        lock (room)
        {
            // Getting the winner and validating the win condition
            winnerId = room.GetMatchWinner();
            // If we cant validate the win condition yet, we return is not finished
            if (winnerId == 0)
                return false;

            // If the room was already marked as finished, we return match is concluded
            if (room.IsFinished)
                return true;

            room.MarkFinished();

        }
        // TODO: Store score for individual players

        _logger.LogInformation("Match concluded! Winner: {WinnerId}", winnerId);


        // Sends the winnerId to the clients
        List<int> userIds = room.UserIds.ToList();
        _ = _gameHubContext.Clients.Users(userIds.Select(x => x.ToString())).SendAsync("MatchConcluded", winnerId).ContinueWith(t => _logger.LogError(t.Exception, "Failed to notify match conclusion for room {RoomId}", room.RoomId),
        TaskContinuationOptions.OnlyOnFaulted); ;

        _roomsManager.RemoveRoom(room.RoomId);
        return true;
    }


    /// <summary>
    ///     Client disconnected so remove it from the room and end it if they're in any
    /// </summary>
    /// <param name="room">In memory room instance</param>
    public void ClientDisconnect(MatchRoom room)
    {
        EndMatch(room.RoomId, "One of the players disconnected");

    }


    /// <summary>
    ///     Client disconnected so remove it from the room and end it if they're in any
    /// </summary>
    /// <param name="userId"></param>
    public void ClientDisconnect(int userId)
    {

        MatchRoom? room = _roomsManager.UserInRoom(userId);
        if (room == null) return;
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