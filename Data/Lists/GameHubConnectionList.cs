using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MuscleRivalsBackend.Data.Lists;

public class GameHubConnectionList
{
    private readonly ConcurrentDictionary<int, (string ConnectionId, string DeviceId)> _connections = [];

    internal void AddConnection(int userId, string connectionId, string deviceId)
    {

        _connections.GetOrAdd(userId, _ => (connectionId, deviceId));

        // if (!_connections.ContainsKey(userId))
        //     _connections[userId] = [];

        // _connections[userId].Add((connectionId, deviceId));

    }

    internal void RemoveConnection(int userId, string connectionId)
    {

        _connections.TryRemove(userId, out _);
        // if (_connections.TryGetValue(userId, out var conns))
        // {
        //     conns.RemoveWhere(c => c.ConnectionId == connectionId);
        //     if (conns.Count == 0)
        //         _connections.Remove(userId);
        // }

    }

    public string? GetConnection(int userId)
    {

        return _connections.FirstOrDefault(c => c.Key == userId).Value.ConnectionId;

    }


    // internal string? GetConnectionIdForDevice(string deviceId)
    // {

    //     return _connections.SelectMany(c => c.Value).Where(c => c.DeviceId == deviceId).Select(c => c.ConnectionId).FirstOrDefault();


    // }

    internal bool UserInHub(int userId)
    {

        return _connections.Any(x => x.Key == userId);

    }
}