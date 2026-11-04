using System.Text.Json;
using Lumen.Application.Abstractions;
using StackExchange.Redis;

namespace Lumen.Infrastructure.Caching;

internal sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var value = await _redis.GetDatabase().StringGetAsync(key);
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<T>((string)value!);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
        => _redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(value), ttl);

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _redis.GetDatabase().KeyDeleteAsync(key);
}
