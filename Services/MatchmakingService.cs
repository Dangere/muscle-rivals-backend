using MuscleRivalsBackend.Utilities;

namespace MuscleRivalsBackend.Services;

public class MatchmakingService
{

    /// <summary>
    /// Adds the the user to the queue to find a match
    /// </summary>
    /// <returns></returns>
    /// Checks first if the user is in the signalR matchmaking hub before adding them to the queue
    public async Task<Result<string>> EnterQueue()
    {
        return Result<string>.Success("test");
    }

    /// <summary>
    /// Allows the user to exit the queue
    /// </summary>
    /// <returns></returns> 
    public async Task<Result<string>> ExitQueue()
    {
        return Result<string>.Success("test");
    }


    /// <summary>
    /// Allows the user to exit the match if its running
    /// </summary>
    /// <returns></returns> 
    public async Task<Result<string>> ExistMatch()
    {
        return Result<string>.Success("test");
    }
}