using System.Collections.ObjectModel;

namespace MuscleRivalsBackend.Models.Matchmaking;

public class MatchRoom(List<int> userIds)
{
    public ReadOnlyCollection<int> UserIds { get; private set; } = userIds.AsReadOnly();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

}