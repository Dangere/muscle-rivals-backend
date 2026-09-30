using System.Collections.ObjectModel;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Models.DTOs.Game.Modes;

namespace MuscleRivalsBackend.Models.Matchmaking;

public class MatchRoom(int roomId, List<int> userIds, BaseGameMode gameMode, ExerciseType exercise)
{
    public int RoomId = roomId;
    public ReadOnlyCollection<int> UserIds { get; private set; } = userIds.AsReadOnly();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public List<int> PlayerScores { get; private set; } = userIds.Select(x => 0).ToList();

    public ExerciseType Exercise { get; private set; } = exercise;

    public BaseGameMode GameMode { get; private set; } = gameMode;


    // Keeps track of how many seconds were paused during the match to calculate the total time if the game mode is timed
    private int _pausedSeconds = 0;


    // Matches start paused for initialization
    private MatchState _state = MatchState.Paused;

    public void Pause()
    {
        _state = MatchState.Paused;
    }

    public void Resume()
    {
        _state = MatchState.InProgress;
    }


    /// <summary>
    ///     Increases the score of a player
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>the new score</returns>
    public int IncreasePlayerScore(int userId)
    {
        int index = UserIds.IndexOf(userId);
        PlayerScores[index]++;
        return PlayerScores[index];

    }

    /// <summary>
    ///     Checks a win condition based on the game mode and returns a userid or 0
    /// </summary>
    /// <returns></returns>
    public int GetMatchWinner()
    {

        List<int> PlayerScoresSnapShot = [.. PlayerScores];

        switch (GameMode)
        {
            // If its the timed mode, we check if timer ran out then return the player id with highest score
            case TimeLimitedGameMode timeLimitedMode:
                bool timeRanOut = (DateTime.UtcNow - CreatedAt).TotalSeconds - _pausedSeconds >= timeLimitedMode.TimeLimitSeconds;
                if (timeRanOut)
                {
                    int winnerIndex = PlayerScoresSnapShot.IndexOf(PlayerScoresSnapShot.Max());
                    return UserIds[winnerIndex];
                }
                break;
            // If its the max reps we check if the player with the highest score has reached 30 
            case MaxRepsGameMode maxRepsMode:
                bool repsReached = PlayerScoresSnapShot.Max() >= maxRepsMode.MaxReps;

                if (repsReached)
                {
                    int winnerIndex = PlayerScoresSnapShot.IndexOf(PlayerScoresSnapShot.Max());
                    return UserIds[winnerIndex];
                }
                break;
            default:
                return 0;
        }
        return 0;


    }

    public bool ValidateTotalLifespan()
    {
        Console.WriteLine($" {(DateTime.UtcNow - CreatedAt).TotalSeconds} Seconds has passed out of {GameMode.MaxLifeTimeMinutes * 60}");



        return (DateTime.UtcNow - CreatedAt).TotalSeconds < GameMode.MaxLifeTimeMinutes * 60;
    }

    public void TickPausedSeconds()
    {
        if (_state == MatchState.Paused)
        {
            _pausedSeconds++;
        }

        // Console.WriteLine($"Paused seconds: {_pausedSeconds}");

        // Console.WriteLine($"Current active match seconds: {(DateTime.UtcNow - CreatedAt).TotalSeconds - _pausedSeconds}");

    }

    public bool IsPaused => _state == MatchState.Paused;

    // public int WinningUserid()
    // {
    //     int index = PlayerScores.IndexOf(PlayerScores.Max());
    //     return UserIds[index];
    // }

    public void MarkFinished() => _state = MatchState.Finished;

    public bool IsFinished => _state == MatchState.Finished;



}