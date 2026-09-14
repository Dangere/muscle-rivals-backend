using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MuscleRivalsBackend.Models.Matchmaking;

namespace MuscleRivalsBackend.Data.Lists;

public class MatchmakingQueueList
{
    private readonly List<MatchEnqueue> _queue =
    [
        new MatchEnqueue(2121,29),
        new MatchEnqueue(2321,29),
        new MatchEnqueue(23,60),
        new MatchEnqueue(254,50),
        new MatchEnqueue(2095,20),
        new MatchEnqueue(26,2),
        new MatchEnqueue(25397,90),
        new MatchEnqueue(227,93),
        new MatchEnqueue(21397,90),
        new MatchEnqueue(266,100),
        new MatchEnqueue(22216,0),

    ];

    public IReadOnlyList<MatchEnqueue> GetSnapshot()
    {
        lock (_queue)
        {
            return [.. _queue];
        }
    }

    internal void Enqueue(MatchEnqueue enqueue)
    {
        lock (_queue)
        {
            // if (!_queue.ContainsKey(userId))
            //     _queue[userId] = [];

            if (_queue.Any(x => x.UserId == enqueue.UserId))
                return;

            else _queue.Add(enqueue);
        }
    }

    internal void LeaveQueue(int userId)
    {
        lock (_queue)
        {
            _queue.RemoveAll(x => x.UserId == userId);
        }
    }


    /// <summary>
    /// Removes a list of users from the queue
    /// Returns true if all users were removed
    /// </summary>
    /// <param name="userIds"></param>
    /// <returns></returns>
    internal bool RemoveUsersFromQueue(List<int> userIds)
    {
        lock (_queue)
        {
            return _queue.RemoveAll(x => userIds.Contains(x.UserId)) == userIds.Count;
        }
    }
}