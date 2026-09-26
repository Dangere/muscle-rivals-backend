using System.Collections.ObjectModel;
using MuscleRivalsBackend.Enums;

namespace MuscleRivalsBackend.Models.Matchmaking;

public class MatchRoom(int roomId, List<int> userIds, GameMode mode, ExerciseType exercise)
{
    public int RoomId = roomId;
    public ReadOnlyCollection<int> UserIds { get; private set; } = userIds.AsReadOnly();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public List<int> PlayerScores { get; private set; } = userIds.Select(x => 0).ToList();

    public GameMode Mode { get; private set; } = mode;
    public ExerciseType Exercise { get; private set; } = exercise;




    public readonly int MatchLengthMinutes = 1;

    // Keeps track of how many seconds were paused during the match to calculate the total time if the game mode is timed
    private int _pausedSeconds = 0;

    private readonly int _matchMaxLifeTimInMinutes = 5;

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
    ///     Validates if the match can be concluded or not by a condition that depends on the game mode
    /// </summary>
    /// <returns></returns>
    public bool ValidateMatchConcludeCondition()
    {
        return Mode switch
        {
            GameMode.Timed => (DateTime.UtcNow - CreatedAt).Seconds - _pausedSeconds > (MatchLengthMinutes * 60) - 2,
            GameMode.MaxReps => false,
            _ => false,
        };
    }

    public bool ValidateTotalLifespan()
    {
        Console.WriteLine($"Seconds has passed {(DateTime.UtcNow - CreatedAt).Seconds} out of {_matchMaxLifeTimInMinutes * 60}");
        Console.WriteLine((DateTime.UtcNow - CreatedAt).Seconds > _matchMaxLifeTimInMinutes * 60);



        return (DateTime.UtcNow - CreatedAt).Seconds < _matchMaxLifeTimInMinutes * 60;
    }

    public void TickPausedSeconds()
    {
        if (_state == MatchState.Paused)
        {
            _pausedSeconds++;
        }

        // Console.WriteLine($"Paused seconds: {_pausedSeconds}");

        // Console.WriteLine($"Current active match seconds: {(DateTime.UtcNow - CreatedAt).Seconds - _pausedSeconds}");

    }

    public bool IsPaused => _state == MatchState.Paused;

    public int WinningUserid()
    {
        int index = PlayerScores.IndexOf(PlayerScores.Max());
        return UserIds[index];
    }

    public void MarkFinished() => _state = MatchState.Finished;

    public bool IsFinished => _state == MatchState.Finished;



}