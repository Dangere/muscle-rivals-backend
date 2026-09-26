using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MuscleRivalsBackend.Data.Lists;

public class GameHubConnectionList
{
    private readonly ConcurrentDictionary<int, string> _connections = [];

    internal void SetConnection(int userId, string connectionId)
    {
        _connections.AddOrUpdate(userId, (k) => connectionId, (k, v) => connectionId);
    }

    internal void RemoveConnections(int userId)
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

        return _connections.FirstOrDefault(c => c.Key == userId).Value;

    }


    // internal string? GetConnectionIdForDevice(string deviceId)
    // {

    //     return _connections.SelectMany(c => c.Value).Where(c => c.DeviceId == deviceId).Select(c => c.ConnectionId).FirstOrDefault();


    // }

    internal bool UserInHub(int userId)
    {

        return _connections.Any(x => x.Key == userId);

    }

    internal bool UsersInHub(List<int> userIds)
    {

        return _connections.Any(x => userIds.Contains(x.Key));
    }
}