using System.Text.Json;
using ThreadQueueLib.Core.Interfaces;
using ThreadQueueLib.Tasks;
using StackExchange.Redis;

namespace ThreadQueueLib.Utils;

/// <summary>
/// Persistencia en Redis para la cola de tareas.
/// </summary>
public class RedisTaskQueuePersistence<T> : ITaskQueuePersistence<T>
{
    private readonly IDatabase _db;
    private readonly string _queueKey;

    public RedisTaskQueuePersistence(string redisConnectionString, string queueKey)
    {
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
        _db = redis.GetDatabase();
        _queueKey = queueKey;
    }

    public async Task EnqueueAsync(QueuedTaskItem<T> task)
    {
        var json = JsonSerializer.Serialize(task);
        await _db.ListRightPushAsync(_queueKey, json);
    }

    public async Task<QueuedTaskItem<T>?> DequeueAsync()
    {
        var json = await _db.ListLeftPopAsync(_queueKey);
        if (json.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<QueuedTaskItem<T>>(json!);
    }

    public async Task<int> GetQueueLengthAsync()
    {
        return (int)await _db.ListLengthAsync(_queueKey);
    }
}

