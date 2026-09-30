using MuscleRivalsBackend.Enums;

namespace MuscleRivalsBackend.Models.Matchmaking;

public class MatchEnqueue(int userId, ExerciseType exerciseType, GameModeType gameMode, double performance)
{

    public int UserId { get; private set; } = userId;
    public DateTime EnqueuedAt { get; private set; } = DateTime.UtcNow;
    public double Performance { get; private set; } = performance;

    public ExerciseType ExerciseType { get; private set; } = exerciseType;
    public GameModeType GameMode { get; private set; } = gameMode;

}