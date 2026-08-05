using Oddify.Common.Application.Authorization;
using Oddify.Common.Application.Caching;
using Oddify.Common.Infrastructure.Authorization;

namespace Oddify.Modules.Users.Infrastructure.Authorization;

internal sealed class CachedPermissionService(PermissionProvider inner, ICacheService cache) : IPermissionService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        string key = CacheKey(userId);

        HashSet<string>? cached = await cache.GetAsync<HashSet<string>>(key, cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        HashSet<string> permissions = await inner.GetForUserIdAsync(userId, cancellationToken);

        await cache.SetAsync(key, permissions, CacheDuration, cancellationToken);

        return permissions;
    }

    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return cache.RemoveAsync(CacheKey(userId), cancellationToken);
    }

    private static string CacheKey(Guid userId) => $"permissions:{userId}";
}
