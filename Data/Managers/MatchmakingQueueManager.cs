using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MuscleRivalsBackend.Models.Matchmaking;

namespace MuscleRivalsBackend.Data.Managers;

public class MatchmakingQueueManager
{
    private readonly List<MatchEnqueue> _queue = [];

    internal void Enqueue(int userId, double performance)
    {
        lock (_queue)
        {
            // if (!_queue.ContainsKey(userId))
            //     _queue[userId] = [];

            if (_queue.Any(x => x.UserId == userId))
                return;

            else _queue.Add(new MatchEnqueue(userId, performance));
        }
    }

    internal void LeaveQueue(int userId)
    {
        lock (_queue)
        {
            _queue.RemoveAll(x => x.UserId == userId);
        }
    }
}