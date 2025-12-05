using StackExchange.Redis;

namespace Server_DotNetCore.Services;

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
            Console.WriteLine("Connected to Redis successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to Redis: {ex.Message}");
            // Continue without Redis if connection fails
            _redis = null!;
            _db = null!;
        }
    }

    public async Task UpdateFileCountAsync(int count)
    {
        if (_db == null) return;
        
        try
        {
            await _db.StringSetAsync(FileCountKey, count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update file count in Redis: {ex.Message}");
        }
    }

    public async Task UpdateActiveUsersAsync(int count)
    {
        if (_db == null) return;
        
        try
        {
            await _db.StringSetAsync(ActiveUsersKey, count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to update active users in Redis: {ex.Message}");
        }
    }

    public async Task<int> GetFileCountAsync()
    {
        if (_db == null) return 0;
        
        try
        {
            var value = await _db.StringGetAsync(FileCountKey);
            return value.HasValue ? (int)value : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get file count from Redis: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> GetActiveUsersAsync()
    {
        if (_db == null) return 0;
        
        try
        {
            var value = await _db.StringGetAsync(ActiveUsersKey);
            return value.HasValue ? (int)value : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get active users from Redis: {ex.Message}");
            return 0;
        }
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}

