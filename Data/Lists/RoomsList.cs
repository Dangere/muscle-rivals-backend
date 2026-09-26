using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MuscleRivalsBackend.Enums;
using MuscleRivalsBackend.Models.Entities;
using MuscleRivalsBackend.Models.Matchmaking;

namespace MuscleRivalsBackend.Data.Lists;

public class RoomsList
{
    internal readonly ConcurrentDictionary<int, MatchRoom> _rooms = [];

    /// <summary>
    ///     Creates a room
    /// </summary>
    /// <param name="userIds"></param>
    /// <returns>Returns the id of the room</returns>
    internal int CreateRoom(List<int> userIds, ExerciseType exercise, GameMode mode)
    {
        if (userIds.Count != 2) throw new Exception("Room must have 2 players!");
        // Creates a random id
        int roomId = Guid.NewGuid().GetHashCode();

        _rooms.AddOrUpdate(roomId, new MatchRoom(roomId, userIds, mode, exercise), (key, value) => new MatchRoom(roomId, userIds, mode, exercise));

        return roomId;
    }

    internal void RemoveRoom(int roomId)
    {
        _rooms.TryRemove(roomId, out _);
    }


    internal List<int> GetRoomMembers(int roomId)
    {
        return _rooms.GetValueOrDefault(roomId)?.UserIds.ToList() ?? [];
    }

    /// <summary>
    ///     Counts a rep and returns total reps for that user
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    internal int CountRep(int roomId, int userId)
    {

        return _rooms[roomId].IncreasePlayerScore(userId);
    }


    /// <summary>
    ///     Checks if a user is in a room
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>returns the room if the user is in one</returns>
    internal MatchRoom? UserInRoom(int userId)
    {

        KeyValuePair<int, MatchRoom> room = _rooms.FirstOrDefault(x => x.Value.UserIds.Contains(userId));

        if (room.Key == 0)
        {
            return null;
        }
        return room.Value;
    }

    internal void PauseGame(int roomId, bool pause)
    {
        var room = _rooms.FirstOrDefault(x => x.Key == roomId);
        if (room.Key == 0)
        {
            return;
        }
        if (pause)
        {
            room.Value.Pause();
            return;
        }
        room.Value.Resume();

    }

    /// <summary>
    ///     Ticks paused rooms
    /// </summary>
    internal void TickPausedRooms()
    {

        _rooms.Select(x => x.Value).Where(room => room.IsPaused).ToList().ForEach(room => room.TickPausedSeconds());

    }
}