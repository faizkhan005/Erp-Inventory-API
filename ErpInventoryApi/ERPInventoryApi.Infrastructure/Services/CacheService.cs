using ERPInventoryApi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace ERPInventoryApi.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CacheService> _logger;
    private readonly IDatabase _db;

    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CacheService(IConnectionMultiplexer redis, ILogger<CacheService> logger)
    {
        _redis = redis;
        _logger = logger;
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache MISS: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache HIT: {Key}", key);
            return JsonSerializer.Deserialize<T>((string)value!, JsonOptions);
        }
        catch (Exception ex)
        {
            // Never let cache failures break the application
            _logger.LogWarning(ex, "Cache GET failed for key {Key} — falling through to database", key);
            return default;
        }

    }
    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, JsonOptions);
            await _db.StringSetAsync(key, serialized, ttl ?? DefaultTtl);
            _logger.LogDebug("Cache SET: {Key} (TTL: {TTL})", key, ttl ?? DefaultTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key {Key} — continuing without cache", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
            _logger.LogDebug("Cache REMOVE: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        try
        {
            // Get the server to scan for keys matching the prefix
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();

            if (keys.Length == 0) return;

            await _db.KeyDeleteAsync(keys);
            _logger.LogDebug("Cache INVALIDATE: removed {Count} keys with prefix {Prefix}",
                keys.Length, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache INVALIDATE failed for prefix {Prefix}", prefix);
        }
    }
}
