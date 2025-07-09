// =====================================================
// File: Core/DataCache.cs
// Cache read values with expiration and thread safety
// =====================================================
using System;
using System.Collections.Generic;
using System.Threading;
using IEC104.Constants;

namespace IEC104.Core
{
    /// <summary>
    /// Caches IEC104 data values with expiration and automatic cleanup
    /// Thread-safe implementation using ReaderWriterLockSlim
    /// </summary>
    public class DataCache : IDisposable
    {
        #region FIELDS

        private readonly Dictionary<string, CachedItem> cache;
        private readonly ReaderWriterLockSlim cacheLock;
        private Timer cleanupTimer;
        private bool disposed = false;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// Current number of items in cache
        /// </summary>
        public int Count
        {
            get
            {
                cacheLock.EnterReadLock();
                try
                {
                    return cache.Count;
                }
                finally
                {
                    cacheLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Default cache lifetime in milliseconds
        /// </summary>
        public int DefaultLifetimeMs { get; set; } = IEC104Constants.DefaultCacheLifetimeMs;

        /// <summary>
        /// Maximum number of items in cache (0 = unlimited)
        /// </summary>
        public int MaxItems { get; set; } = IEC104Constants.MaxCacheSize;

        #endregion

        #region CONSTRUCTOR

        public DataCache()
        {
            cache = new Dictionary<string, CachedItem>();
            cacheLock = new ReaderWriterLockSlim();

            // Start cleanup timer (every 5 minutes)
            cleanupTimer = new Timer(OnCleanupTimer, null,
                                   TimeSpan.FromMilliseconds(IEC104Constants.CacheCleanupIntervalMs),
                                   TimeSpan.FromMilliseconds(IEC104Constants.CacheCleanupIntervalMs));
        }

        public DataCache(int defaultLifetimeMs, int maxItems = 0) : this()
        {
            DefaultLifetimeMs = defaultLifetimeMs;
            MaxItems = maxItems;
        }

        #endregion

        #region CACHE OPERATIONS

        /// <summary>
        /// Store value in cache with optional custom lifetime
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="value">Value to cache</param>
        /// <param name="quality">Data quality</param>
        /// <param name="lifetimeMs">Custom lifetime in milliseconds (-1 = use default)</param>
        public void Set(string key, object value, string quality, int lifetimeMs = -1)
        {
            if (string.IsNullOrEmpty(key)) return;

            var lifetime = lifetimeMs > 0 ? lifetimeMs : DefaultLifetimeMs;
            var item = new CachedItem
            {
                Value = value,
                Quality = quality ?? "Bad",
                Timestamp = DateTime.Now,
                ExpiryTime = DateTime.Now.AddMilliseconds(lifetime),
                AccessCount = 1,
                LastAccessTime = DateTime.Now
            };

            cacheLock.EnterWriteLock();
            try
            {
                // Check if we need to make room
                if (MaxItems > 0 && cache.Count >= MaxItems && !cache.ContainsKey(key))
                {
                    RemoveOldestItem();
                }

                // Update existing item's access count if it exists
                if (cache.TryGetValue(key, out CachedItem existing))
                {
                    item.AccessCount = existing.AccessCount + 1;
                }

                cache[key] = item;
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get value from cache if not expired
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <param name="value">Retrieved value</param>
        /// <param name="quality">Retrieved quality</param>
        /// <param name="timestamp">When value was cached</param>
        /// <returns>True if value found and not expired</returns>
        public bool TryGet(string key, out object value, out string quality, out DateTime timestamp)
        {
            value = null;
            quality = "Bad";
            timestamp = DateTime.MinValue;

            if (string.IsNullOrEmpty(key)) return false;

            cacheLock.EnterUpgradeableReadLock();
            try
            {
                if (cache.TryGetValue(key, out CachedItem item))
                {
                    // Check if expired
                    if (DateTime.Now <= item.ExpiryTime)
                    {
                        // Update access statistics
                        cacheLock.EnterWriteLock();
                        try
                        {
                            item.AccessCount++;
                            item.LastAccessTime = DateTime.Now;
                        }
                        finally
                        {
                            cacheLock.ExitWriteLock();
                        }

                        value = item.Value;
                        quality = item.Quality;
                        timestamp = item.Timestamp;
                        return true;
                    }
                    else
                    {
                        // Item expired, remove it
                        cacheLock.EnterWriteLock();
                        try
                        {
                            cache.Remove(key);
                        }
                        finally
                        {
                            cacheLock.ExitWriteLock();
                        }
                    }
                }

                return false;
            }
            finally
            {
                cacheLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Get value from cache regardless of expiration (for debugging)
        /// </summary>
        public bool TryGetRaw(string key, out CachedItem item)
        {
            item = null;
            if (string.IsNullOrEmpty(key)) return false;

            cacheLock.EnterReadLock();
            try
            {
                return cache.TryGetValue(key, out item);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Remove specific value from cache
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>True if item was removed</returns>
        public bool Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            cacheLock.EnterWriteLock();
            try
            {
                return cache.Remove(key);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Clear all items from cache
        /// </summary>
        public void Clear()
        {
            cacheLock.EnterWriteLock();
            try
            {
                cache.Clear();
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Check if key exists and is not expired
        /// </summary>
        /// <param name="key">Cache key</param>
        /// <returns>True if key exists and not expired</returns>
        public bool ContainsKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            cacheLock.EnterReadLock();
            try
            {
                if (cache.TryGetValue(key, out CachedItem item))
                {
                    return DateTime.Now <= item.ExpiryTime;
                }
                return false;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get all cache keys (including expired ones)
        /// </summary>
        public List<string> GetAllKeys()
        {
            cacheLock.EnterReadLock();
            try
            {
                return new List<string>(cache.Keys);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get all valid (non-expired) cache keys
        /// </summary>
        public List<string> GetValidKeys()
        {
            var validKeys = new List<string>();
            var now = DateTime.Now;

            cacheLock.EnterReadLock();
            try
            {
                foreach (var kvp in cache)
                {
                    if (now <= kvp.Value.ExpiryTime)
                    {
                        validKeys.Add(kvp.Key);
                    }
                }
            }
            finally
            {
                cacheLock.ExitReadLock();
            }

            return validKeys;
        }

        #endregion

        #region CLEANUP OPERATIONS

        private void OnCleanupTimer(object state)
        {
            CleanupExpiredItems();
        }

        /// <summary>
        /// Remove all expired items from cache
        /// </summary>
        public int CleanupExpiredItems()
        {
            var now = DateTime.Now;
            var keysToRemove = new List<string>();

            // First pass: identify expired items
            cacheLock.EnterReadLock();
            try
            {
                foreach (var kvp in cache)
                {
                    if (now > kvp.Value.ExpiryTime)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }
            finally
            {
                cacheLock.ExitReadLock();
            }

            // Second pass: remove expired items
            if (keysToRemove.Count > 0)
            {
                cacheLock.EnterWriteLock();
                try
                {
                    foreach (var key in keysToRemove)
                    {
                        cache.Remove(key);
                    }
                }
                finally
                {
                    cacheLock.ExitWriteLock();
                }
            }

            return keysToRemove.Count;
        }

        /// <summary>
        /// Remove oldest item based on last access time
        /// </summary>
        private void RemoveOldestItem()
        {
            // Must be called within write lock
            if (cache.Count == 0) return;

            string oldestKey = null;
            DateTime oldestTime = DateTime.MaxValue;

            foreach (var kvp in cache)
            {
                if (kvp.Value.LastAccessTime < oldestTime)
                {
                    oldestTime = kvp.Value.LastAccessTime;
                    oldestKey = kvp.Key;
                }
            }

            if (oldestKey != null)
            {
                cache.Remove(oldestKey);
            }
        }

        /// <summary>
        /// Remove least frequently used items
        /// </summary>
        /// <param name="count">Number of items to remove</param>
        public int RemoveLFUItems(int count)
        {
            if (count <= 0) return 0;

            var itemsToRemove = new List<string>();

            cacheLock.EnterWriteLock();
            try
            {
                var sortedItems = new List<KeyValuePair<string, CachedItem>>(cache);
                sortedItems.Sort((x, y) => x.Value.AccessCount.CompareTo(y.Value.AccessCount));

                int removeCount = Math.Min(count, sortedItems.Count);
                for (int i = 0; i < removeCount; i++)
                {
                    itemsToRemove.Add(sortedItems[i].Key);
                }

                foreach (var key in itemsToRemove)
                {
                    cache.Remove(key);
                }
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }

            return itemsToRemove.Count;
        }

        #endregion

        #region STATISTICS

        /// <summary>
        /// Get detailed cache statistics
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            cacheLock.EnterReadLock();
            try
            {
                var now = DateTime.Now;
                int validItems = 0;
                int expiredItems = 0;
                long totalAccessCount = 0;
                DateTime? oldestTimestamp = null;
                DateTime? newestTimestamp = null;

                foreach (var item in cache.Values)
                {
                    if (now <= item.ExpiryTime)
                    {
                        validItems++;
                    }
                    else
                    {
                        expiredItems++;
                    }

                    totalAccessCount += item.AccessCount;

                    if (!oldestTimestamp.HasValue || item.Timestamp < oldestTimestamp.Value)
                        oldestTimestamp = item.Timestamp;

                    if (!newestTimestamp.HasValue || item.Timestamp > newestTimestamp.Value)
                        newestTimestamp = item.Timestamp;
                }

                return new CacheStatistics
                {
                    TotalItems = cache.Count,
                    ValidItems = validItems,
                    ExpiredItems = expiredItems,
                    DefaultLifetimeMs = DefaultLifetimeMs,
                    MaxItems = MaxItems,
                    TotalAccessCount = totalAccessCount,
                    AverageAccessCount = cache.Count > 0 ? (double)totalAccessCount / cache.Count : 0,
                    OldestItemTimestamp = oldestTimestamp,
                    NewestItemTimestamp = newestTimestamp
                };
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get memory usage estimation in bytes
        /// </summary>
        public long GetEstimatedMemoryUsage()
        {
            cacheLock.EnterReadLock();
            try
            {
                long totalBytes = 0;

                foreach (var kvp in cache)
                {
                    // Estimate key size
                    totalBytes += kvp.Key.Length * 2; // Unicode chars

                    // Estimate value size
                    if (kvp.Value.Value is string str)
                        totalBytes += str.Length * 2;
                    else if (kvp.Value.Value != null)
                        totalBytes += 64; // Rough estimate for other types

                    // Quality string
                    totalBytes += (kvp.Value.Quality?.Length ?? 0) * 2;

                    // DateTime and other fields
                    totalBytes += 64;
                }

                return totalBytes;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        #endregion

        #region DISPOSE

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            // Stop cleanup timer
            cleanupTimer?.Dispose();
            cleanupTimer = null;

            // Clear cache and dispose lock
            cacheLock.EnterWriteLock();
            try
            {
                cache.Clear();
            }
            finally
            {
                cacheLock.ExitWriteLock();
                cacheLock.Dispose();
            }
        }

        #endregion

        #region NESTED CLASSES

        /// <summary>
        /// Individual cached item with metadata
        /// </summary>
        private class CachedItem
        {
            public object Value { get; set; }
            public string Quality { get; set; }
            public DateTime Timestamp { get; set; }
            public DateTime ExpiryTime { get; set; }
            public DateTime LastAccessTime { get; set; }
            public long AccessCount { get; set; }

            public bool IsExpired => DateTime.Now > ExpiryTime;
            public TimeSpan Age => DateTime.Now - Timestamp;
            public TimeSpan TimeToExpiry => ExpiryTime - DateTime.Now;
        }

        #endregion
    }

    /// <summary>
    /// Cache statistics for monitoring and debugging
    /// </summary>
    public class CacheStatistics
    {
        public int TotalItems { get; set; }
        public int ValidItems { get; set; }
        public int ExpiredItems { get; set; }
        public int DefaultLifetimeMs { get; set; }
        public int MaxItems { get; set; }
        public long TotalAccessCount { get; set; }
        public double AverageAccessCount { get; set; }
        public DateTime? OldestItemTimestamp { get; set; }
        public DateTime? NewestItemTimestamp { get; set; }

        /// <summary>
        /// Cache hit ratio (valid items / total items)
        /// </summary>
        public double HitRatio => TotalItems > 0 ? (double)ValidItems / TotalItems : 0.0;

        /// <summary>
        /// Cache efficiency percentage
        /// </summary>
        public double EfficiencyPercentage => HitRatio * 100.0;

        /// <summary>
        /// Time span from oldest to newest item
        /// </summary>
        public TimeSpan? DataSpan => (OldestItemTimestamp.HasValue && NewestItemTimestamp.HasValue)
            ? NewestItemTimestamp.Value - OldestItemTimestamp.Value
            : null;

        public override string ToString()
        {
            return $"Cache: {ValidItems}/{TotalItems} valid ({EfficiencyPercentage:F1}%), " +
                   $"Avg Access: {AverageAccessCount:F1}, " +
                   $"Lifetime: {DefaultLifetimeMs}ms";
        }
    }
}