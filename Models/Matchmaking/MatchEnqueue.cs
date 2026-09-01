namespace MuscleRivalsBackend.Models.Matchmaking;

public class MatchEnqueue(int userId, double performance)
{

    public int UserId { get; private set; } = userId;
    public DateTime EnqueuedAt { get; private set; } = DateTime.UtcNow;
    public double Performance { get; private set; } = performance;

}