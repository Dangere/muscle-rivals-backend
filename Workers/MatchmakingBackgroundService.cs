using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MuscleRivalsBackend.Data.Lists;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Models.Matchmaking;
using MuscleRivalsBackend.Services;

namespace MuscleRivalsBackend.Workers;

/// <summary>
///     Background service used to match players every 5 seconds
/// </summary>
/// <param name="logger"></param>
/// <param name="gameManager"></param>
/// <param name="matchmakingQueue"></param>
public class MatchmakingBackgroundService(ILogger<MatchmakingBackgroundService> logger, GameManager gameManager, MatchmakingQueueList matchmakingQueue) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private readonly GameManager _gameManager = gameManager;
    private readonly MatchmakingQueueList _matchmakingQueue = matchmakingQueue;
    private readonly ILogger<MatchmakingBackgroundService> _logger = logger;


    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                MatchPlayers(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Matchmaking tick failed");
            }
        }
    }


    internal void MatchPlayers(CancellationToken stoppingToken)
    {
        // The queue is sorted so the highest rating players are on the end, we will match player with the one below it
        List<MatchEnqueue> snapshot = [.. _matchmakingQueue.GetSnapshot()];
        if (snapshot.Count < 2)
        {
            // _logger.LogInformation("Not enough players in queue");
            return;
        }

        snapshot.Sort((x, y) => x.Performance.CompareTo(y.Performance));

        List<int> usersMovedFromQueue = [];

        // Grouping by exercise type
        Dictionary<ExerciseType, List<MatchEnqueue>> exercisesEnqueues = snapshot.GroupBy(x => x.ExerciseType).ToDictionary(x => x.Key, x => x.ToList());


        // Loop in each exercise and match players
        foreach (var enqueues in exercisesEnqueues)
        {
            if (stoppingToken.IsCancellationRequested) return;
            MatchPlayersInExercise(enqueues.Value, enqueues.Key, stoppingToken);

            usersMovedFromQueue.AddRange(enqueues.Value.Select(x => x.UserId));
        }


        bool didRemoveFromQueue = _matchmakingQueue.RemoveUsersFromQueue(usersMovedFromQueue);


    }

    private void MatchPlayersInExercise(List<MatchEnqueue> Enqueues, ExerciseType exercise, CancellationToken stoppingToken)
    {
        // Grouping by game mode type
        Dictionary<GameModeType, List<MatchEnqueue>> gameModesEnqueues = Enqueues.GroupBy(x => x.GameMode).ToDictionary(x => x.Key, x => x.ToList());

        foreach (var gameModeEnqueues in gameModesEnqueues)
        {
            if (stoppingToken.IsCancellationRequested) return;

            GameModeType gameMode = gameModeEnqueues.Key;

            // Doing a reverse for loop so we start from the top with the highest rating, the loop stops at player index 0 because theres not players below it
            for (int i = gameModeEnqueues.Value.Count - 1; i > 0; i -= 2)
            {
                if (i < 0) break;
                if (stoppingToken.IsCancellationRequested) return;
                MatchEnqueue first = gameModeEnqueues.Value[i];
                MatchEnqueue second = gameModeEnqueues.Value[i - 1];

                _logger.LogInformation("Matched {UserId1} with {UserId2}, {Rating1} vs {Rating2}, Exercise: {Exercise}", first.UserId, second.UserId, first.Performance, second.Performance, exercise);

                _ = _gameManager.StartMatch([first.UserId, second.UserId], exercise, gameMode);
            }
        }

    }
}