using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MuscleRivalsBackend.Data.Lists;
using MuscleRivalsBackend.Services;

namespace MuscleRivalsBackend.Workers;

public class RoomLifetimeBackgroundService(RoomsList roomsList, ILogger<RoomLifetimeBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);
    private readonly RoomsList _roomsList = roomsList;
    private readonly ILogger<RoomLifetimeBackgroundService> _logger = logger;


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {

            try
            {
                _roomsList.TickPausedRooms();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Room lifetime tick failed");
            }
        }
    }
}