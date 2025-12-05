using Server_DotNetCore.Services;

namespace Server_DotNetCore.Tests;

public class RedisServiceTests
{
    [Fact]
    public void RedisService_CanBeInstantiated()
    {
        // This test will fail if Redis is not running, but that's okay for minimal tests
        // In a real scenario, you'd mock Redis or skip this test if Redis is unavailable
        var service = new RedisService("localhost:6379");
        Assert.NotNull(service);
        service.Dispose();
    }

    [Fact]
    public async Task RedisService_HandlesInvalidConnectionGracefully()
    {
        // Test that service handles Redis unavailability gracefully (returns 0, doesn't throw)
        var service = new RedisService("invalid-host:6379");
        var fileCount = await service.GetFileCountAsync();
        var activeUsers = await service.GetActiveUsersAsync();
        
        // Service should return 0 when Redis is unavailable (graceful degradation)
        Assert.Equal(0, fileCount);
        Assert.Equal(0, activeUsers);
        service.Dispose();
    }
}
