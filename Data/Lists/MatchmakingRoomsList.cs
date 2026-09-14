using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MuscleRivalsBackend.Models.Matchmaking;

namespace MuscleRivalsBackend.Data.Lists;

public class MatchmakingRoomsList
{
    private readonly ConcurrentDictionary<int, MatchRoom> _rooms = [];

    /// <summary>
    ///     Creates a room
    /// </summary>
    /// <param name="userIds"></param>
    /// <returns>Returns the id of the room</returns>
    internal int CreateRoom(List<int> userIds)
    {
        // Creates a random id
        int roomId = Guid.NewGuid().GetHashCode();

        _rooms.AddOrUpdate(roomId, new MatchRoom(userIds), (key, value) => new MatchRoom(userIds));

        return roomId;
    }

    internal List<int> GetRoomMembers(int roomId)
    {
        return _rooms.GetValueOrDefault(roomId)?.UserIds.ToList() ?? [];
    }


    /// <summary>
    ///     Checks if a user is in a room
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>returns room id if the user is in a room</returns>
    internal int? UserInRoom(int userId)
    {
        return _rooms.FirstOrDefault(x => x.Value.UserIds.Contains(userId)).Key;
    }
}