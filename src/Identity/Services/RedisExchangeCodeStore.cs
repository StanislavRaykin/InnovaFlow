using Microsoft.Extensions.Caching.Distributed;
using Contracts.Identity;
using System.Security.Cryptography;
using System.Text.Json;
namespace Identity.Services;

public class RedisExchangeCodeStore(IDistributedCache cache) : IExchangeCodeStore
{
    public async Task<string> StoreAsync(AuthResponse auth, TimeSpan time, CancellationToken ct)
    {
        var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        await cache.SetAsync($"identity:xchg:{code}",
            JsonSerializer.SerializeToUtf8Bytes(auth),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = time }, ct);
        return code;
    }

    public async Task<AuthResponse?> ConsumeAsync(string code, CancellationToken ct)
    {
        var key = $"identity:xchg:{code}";
        var bytes = await cache.GetAsync(key, ct);
        if (bytes is null) return null;
        await cache.RemoveAsync(key, ct);          // single use — consume, don't just read
        return JsonSerializer.Deserialize<AuthResponse>(bytes);
    }
}

public interface IExchangeCodeStore
{
    public Task<string> StoreAsync(AuthResponse auth, TimeSpan time, CancellationToken ct);

    public Task<AuthResponse?> ConsumeAsync(string code, CancellationToken ct);
}