using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MuscleRivalsBackend.Data.Managers;

public class MatchmakingHubConnectionManager
{
    private readonly Dictionary<int, HashSet<(string ConnectionId, string DeviceId)>> _connections = [];

    internal void AddConnection(int userId, string connectionId, string deviceId)
    {
        lock (_connections)
        {
            if (!_connections.ContainsKey(userId))
                _connections[userId] = [];

            _connections[userId].Add((connectionId, deviceId));
        }
    }

    internal void RemoveConnection(int userId, string connectionId)
    {
        lock (_connections)
        {
            if (_connections.TryGetValue(userId, out var conns))
            {
                conns.RemoveWhere(c => c.ConnectionId == connectionId);
                if (conns.Count == 0)
                    _connections.Remove(userId);
            }
        }
    }

    public IReadOnlyList<string> GetConnections(int userId, string? excludeDeviceId = null)
    {
        lock (_connections)
        {
            return _connections.TryGetValue(userId, out var conns)
                ? conns.Where(c => c.DeviceId != excludeDeviceId).Select(c => c.ConnectionId).ToList()
                : [];
        }
    }


    internal string? GetConnectionIdForDevice(string deviceId)
    {
        lock (_connections)
        {
            return _connections.SelectMany(c => c.Value).Where(c => c.DeviceId == deviceId).Select(c => c.ConnectionId).FirstOrDefault();
        }

    }

    internal bool IsUserIdInHub(int userId)
    {
        lock (_connections)
        {
            return _connections.Any(x => x.Key == userId);
        }
    }
}