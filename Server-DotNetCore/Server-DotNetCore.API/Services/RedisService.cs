using StackExchange.Redis;

namespace Server_DotNetCore.API.Services;

public class RedisService : IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private const string FileCountKey = "server:file_count";
    private const string ActiveUsersKey = "server:active_users";

    public RedisService(string connectionString = "localhost:6379")
    {
        try
        {
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _db = _redis.GetDatabase();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to connect to Redis: {ex.Message}", ex);
        }
    }

    public async Task<int> GetFileCountAsync()
    {
        try
        {
            var value = await _db.StringGetAsync(FileCountKey);
            return value.HasValue ? (int)value : 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get file count from Redis: {ex.Message}", ex);
        }
    }

    public async Task<int> GetActiveUsersAsync()
    {
        try
        {
            var value = await _db.StringGetAsync(ActiveUsersKey);
            return value.HasValue ? (int)value : 0;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get active users from Redis: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}

