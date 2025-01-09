using Astra.Hosting.Http.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Astra.Hosting.Caching
{
    public static class HttpCacheManager
    {
        private static readonly ConcurrentBag<HttpCacheEntry> _cacheEntries = new();
        private static readonly object _lock = new();

        public static bool TryAdd(string cacheKey, IHttpResponse response, TimeSpan expiration)
        {
            lock (_lock)
            {
                CleanExpiredEntries();
                if (_cacheEntries.Any(entry => entry.id == cacheKey))
                    return false;

                var cacheEntry = new HttpCacheEntry(
                    id: cacheKey,
                    contentType: response.ContentType,
                    eTag: GetResponseETag(response),
                    content: response.Content,
                    expires: DateTime.UtcNow.Add(expiration)
                );

                _cacheEntries.Add(cacheEntry);
                return true;
            }
        }

        public static bool TryGet(string cacheKey, out HttpCacheEntry cacheEntry)
        {
            lock (_lock)
            {
                CleanExpiredEntries();
                cacheEntry = _cacheEntries.FirstOrDefault(entry => entry.id == cacheKey && entry.expiry > DateTime.UtcNow);
                return cacheEntry.id != null;
            }
        }

        private static string GetResponseETag(IHttpResponse response) => BitConverter.ToString(MD5.HashData(response.Content))
            .Replace("-", "").ToLowerInvariant();

        private static void CleanExpiredEntries()
        {
            lock (_lock)
            {
                var expiredEntries = _cacheEntries.Where(entry => entry.expiry <= DateTime.UtcNow).ToList();
                foreach (var expiredEntry in expiredEntries)
                    _cacheEntries.TryTake(out _);
            }
        }

        public static void ClearAllCacheEntries()
        {
            lock (_lock)
            {
                _cacheEntries.Clear();
            }
        }
    }
}
