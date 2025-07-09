// =====================================================
// File: Cache/DataCacheManager.cs
// Main cache manager for multiple devices
// =====================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IEC104.Core;
using IEC104.Protocol.Enum;
using IEC104.Constants;

namespace IEC104.Cache
{
    /// <summary>
    /// Manages multiple data caches for different devices
    /// Thread-safe implementation for multi-device IEC104 environments
    /// </summary>
    public class DataCacheManager : IDisposable
    {
        #region FIELDS

        private readonly Dictionary<string, DataCache> deviceCaches;
        private readonly Dictionary<string, DateTime> deviceLastActivity;
        private readonly ReaderWriterLockSlim cachesLock;
        private Timer maintenanceTimer;
        private bool disposed = false;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// Default cache lifetime for new caches (milliseconds)
        /// </summary>
        public int DefaultCacheLifetime { get; set; } = IEC104Constants.DefaultCacheLifetimeMs;

        /// <summary>
        /// Maximum items per device cache
        /// </summary>
        public int MaxItemsPerCache { get; set; } = 1000;

        /// <summary>
        /// Auto cleanup interval (milliseconds)
        /// </summary>
        public int MaintenanceInterval { get; set; } = 300000; // 5 minutes

        /// <summary>
        /// Device cache expiration time (milliseconds) - remove unused device caches
        /// </summary>
        public int DeviceCacheExpiration { get; set; } = 3600000; // 1 hour

        /// <summary>
        /// Total number of device caches
        /// </summary>
        public int DeviceCount
        {
            get
            {
                cachesLock.EnterReadLock();
                try
                {
                    return deviceCaches.Count;
                }
                finally
                {
                    cachesLock.ExitReadLock();
                }
            }
        }

        #endregion

        #region CONSTRUCTOR

        public DataCacheManager()
        {
            deviceCaches = new Dictionary<string, DataCache>();
            deviceLastActivity = new Dictionary<string, DateTime>();
            cachesLock = new ReaderWriterLockSlim();

            StartMaintenanceTimer();
        }

        public DataCacheManager(int defaultLifetime, int maxItemsPerCache) : this()
        {
            DefaultCacheLifetime = defaultLifetime;
            MaxItemsPerCache = maxItemsPerCache;
        }

        #endregion

        #region CACHE MANAGEMENT

        /// <summary>
        /// Get or create cache for device
        /// </summary>
        /// <param name="deviceKey">Unique device identifier</param>
        /// <returns>DataCache for the device</returns>
        private DataCache GetOrCreateCache(string deviceKey)
        {
            if (string.IsNullOrEmpty(deviceKey))
                throw new ArgumentException("Device key cannot be null or empty", nameof(deviceKey));

            cachesLock.EnterUpgradeableReadLock();
            try
            {
                if (!deviceCaches.TryGetValue(deviceKey, out DataCache cache))
                {
                    cachesLock.EnterWriteLock();
                    try
                    {
                        // Double-check pattern
                        if (!deviceCaches.TryGetValue(deviceKey, out cache))
                        {
                            cache = new DataCache(DefaultCacheLifetime, MaxItemsPerCache);
                            deviceCaches[deviceKey] = cache;
                            deviceLastActivity[deviceKey] = DateTime.Now;
                        }
                    }
                    finally
                    {
                        cachesLock.ExitWriteLock();
                    }
                }
                else
                {
                    // Update last activity
                    cachesLock.EnterWriteLock();
                    try
                    {
                        deviceLastActivity[deviceKey] = DateTime.Now;
                    }
                    finally
                    {
                        cachesLock.ExitWriteLock();
                    }
                }

                return cache;
            }
            finally
            {
                cachesLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Get existing cache for device (without creating)
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <returns>DataCache if exists, null otherwise</returns>
        private DataCache GetExistingCache(string deviceKey)
        {
            if (string.IsNullOrEmpty(deviceKey)) return null;

            cachesLock.EnterReadLock();
            try
            {
                deviceCaches.TryGetValue(deviceKey, out DataCache cache);
                return cache;
            }
            finally
            {
                cachesLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Remove cache for specific device
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <returns>True if cache was removed</returns>
        public bool RemoveDeviceCache(string deviceKey)
        {
            if (string.IsNullOrEmpty(deviceKey)) return false;

            cachesLock.EnterWriteLock();
            try
            {
                if (deviceCaches.TryGetValue(deviceKey, out DataCache cache))
                {
                    cache.Dispose();
                    deviceCaches.Remove(deviceKey);
                    deviceLastActivity.Remove(deviceKey);
                    return true;
                }
                return false;
            }
            finally
            {
                cachesLock.ExitWriteLock();
            }
        }

        #endregion

        #region DATA OPERATIONS

        /// <summary>
        /// Update cache with new IOA data
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <param name="ioa">Information Object Address</param>
        /// <param name="value">Data value</param>
        /// <param name="quality">Data quality</param>
        /// <param name="customLifetime">Custom cache lifetime (optional)</param>
        public void UpdateCache(string deviceKey, int ioa, string value, string quality, int customLifetime = -1)
        {
            var cache = GetOrCreateCache(deviceKey);
            var key = CreateIOAKey(ioa);
            cache.Set(key, value, quality, customLifetime);
        }

        /// <summary>
        /// Update cache with TypeId information
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <param name="ioa">Information Object Address</param>
        /// <param name="typeId">Type ID</param>
        /// <param name="value">Data value</param>
        /// <param name="quality">Data quality</param>
        /// <param name="customLifetime">Custom cache lifetime (optional)</param>
        public void UpdateCache(string deviceKey, int ioa, TypeId typeId, string value, string quality, int customLifetime = -1)
        {
            var cache = GetOrCreateCache(deviceKey);
            var key = CreateIOAKey(ioa, typeId);
            cache.Set(key, value, quality, customLifetime);
        }

        /// <summary>
        /// Get cached IOA data
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <param name="ioa">Information Object Address</param>
        /// <param name="value">Retrieved value</param>
        /// <param name="quality">Retrieved quality</param>
        /// <param name="timestamp">Cache timestamp</param>
        /// <returns>True if data found and valid</returns>
        public bool GetCachedData(string deviceKey, int ioa, out string value, out string quality, out DateTime timestamp)
        {
            value = null;
            quality = "Bad";
            timestamp = DateTime.MinValue;

            var cache = GetExistingCache(deviceKey);
            if (cache != null)
            {
                var key = CreateIOAKey(ioa);
                if (cache.TryGet(key, out object objValue, out quality, out timestamp))
                {
                    value = objValue?.ToString();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get cached IOA data with TypeId
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <param name="ioa">Information Object Address</param>
        /// <param name="typeId">Type ID</param>
        /// <param name="value">Retrieved value</param>
        /// <param name="quality">Retrieved quality</param>
        /// <param name="timestamp">Cache timestamp</param>
        /// <returns>True if data found and valid</returns>
        public bool GetCachedData(string deviceKey, int ioa, TypeId typeId, out string value, out string quality, out DateTime timestamp)
        {
            value = null;
            quality = "Bad";
            timestamp = DateTime.MinValue;

            var cache = GetExistingCache(deviceKey);
            if (cache != null)
            {
                var key = CreateIOAKey(ioa, typeId);
                if (cache.TryGet(key, out object objValue, out quality, out timestamp))
                {
                    value = objValue?.ToString();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if IOA data exists in cache
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <param name="ioa">Information Object Address</param>
        /// <returns>True if data exists and not expired</returns>
        public bool ContainsIOA(string deviceKey, int ioa)
        {
            var cache = GetExistingCache(deviceKey);
            if (cache != null)
            {
                var key = CreateIOAKey(ioa);
                return cache.ContainsKey(key);
            }
            return false;
        }

        /// <summary>
        /// Remove specific IOA from cache
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <param name="ioa">Information Object Address</param>
        /// <returns>True if IOA was removed</returns>
        public bool RemoveIOA(string deviceKey, int ioa)
        {
            var cache = GetExistingCache(deviceKey);
            if (cache != null)
            {
                var key = CreateIOAKey(ioa);
                return cache.Remove(key);
            }
            return false;
        }

        #endregion

        #region BULK OPERATIONS

        /// <summary>
        /// Update multiple IOAs at once
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <param name="ioaData">Dictionary of IOA -> (value, quality)</param>
        /// <param name="customLifetime">Custom cache lifetime (optional)</param>
        public void UpdateMultipleIOAs(string deviceKey, Dictionary<int, (string value, string quality)> ioaData, int customLifetime = -1)
        {
            if (ioaData == null || ioaData.Count == 0) return;

            var cache = GetOrCreateCache(deviceKey);

            foreach (var kvp in ioaData)
            {
                var key = CreateIOAKey(kvp.Key);
                cache.Set(key, kvp.Value.value, kvp.Value.quality, customLifetime);
            }
        }

        /// <summary>
        /// Get all cached IOAs for a device
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <returns>Dictionary of IOA -> (value, quality, timestamp)</returns>
        public Dictionary<int, (string value, string quality, DateTime timestamp)> GetAllIOAs(string deviceKey)
        {
            var result = new Dictionary<int, (string value, string quality, DateTime timestamp)>();

            var cache = GetExistingCache(deviceKey);
            if (cache != null)
            {
                var validKeys = cache.GetValidKeys();

                foreach (var key in validKeys)
                {
                    if (TryParseIOAKey(key, out int ioa))
                    {
                        if (cache.TryGet(key, out object value, out string quality, out DateTime timestamp))
                        {
                            result[ioa] = (value?.ToString(), quality, timestamp);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Clear cache for specific device
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        public void ClearDeviceCache(string deviceKey)
        {
            var cache = GetExistingCache(deviceKey);
            cache?.Clear();
        }

        /// <summary>
        /// Clear all caches for all devices
        /// </summary>
        public void ClearAllCache()
        {
            cachesLock.EnterReadLock();
            try
            {
                foreach (var cache in deviceCaches.Values)
                {
                    cache.Clear();
                }
            }
            finally
            {
                cachesLock.ExitReadLock();
            }
        }

        #endregion

        #region KEY MANAGEMENT

        /// <summary>
        /// Create cache key for IOA
        /// </summary>
        private string CreateIOAKey(int ioa)
        {
            return $"IOA_{ioa}";
        }

        /// <summary>
        /// Create cache key for IOA with TypeId
        /// </summary>
        private string CreateIOAKey(int ioa, TypeId typeId)
        {
            return $"IOA_{ioa}_{(int)typeId}";
        }

        /// <summary>
        /// Parse IOA from cache key
        /// </summary>
        private bool TryParseIOAKey(string key, out int ioa)
        {
            ioa = 0;
            if (string.IsNullOrEmpty(key) || !key.StartsWith("IOA_"))
                return false;

            var parts = key.Split('_');
            if (parts.Length >= 2)
            {
                return int.TryParse(parts[1], out ioa);
            }

            return false;
        }

        /// <summary>
        /// Parse IOA and TypeId from cache key
        /// </summary>
        private bool TryParseIOAKey(string key, out int ioa, out TypeId typeId)
        {
            ioa = 0;
            typeId = TypeId.M_SP_NA_1;

            if (string.IsNullOrEmpty(key) || !key.StartsWith("IOA_"))
                return false;

            var parts = key.Split('_');
            if (parts.Length >= 3)
            {
                if (int.TryParse(parts[1], out ioa) &&
                    Enum.TryParse(parts[2], out TypeId parsedTypeId))
                {
                    typeId = parsedTypeId;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region STATISTICS & MONITORING

        /// <summary>
        /// Get combined cache statistics for all devices
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            var totalStats = new CacheStatistics();

            cachesLock.EnterReadLock();
            try
            {
                foreach (var cache in deviceCaches.Values)
                {
                    var stats = cache.GetStatistics();
                    totalStats.TotalItems += stats.TotalItems;
                    totalStats.ValidItems += stats.ValidItems;
                    totalStats.ExpiredItems += stats.ExpiredItems;
                    totalStats.TotalAccessCount += stats.TotalAccessCount;
                }

                totalStats.DefaultLifetimeMs = DefaultCacheLifetime;
                totalStats.MaxItems = MaxItemsPerCache * deviceCaches.Count;

                if (deviceCaches.Count > 0)
                {
                    totalStats.AverageAccessCount = totalStats.TotalItems > 0 ?
                        (double)totalStats.TotalAccessCount / totalStats.TotalItems : 0;
                }
            }
            finally
            {
                cachesLock.ExitReadLock();
            }

            return totalStats;
        }

        /// <summary>
        /// Get statistics for specific device
        /// </summary>
        /// <param name="deviceKey">Device identifier</param>
        /// <returns>Cache statistics for the device</returns>
        public CacheStatistics GetDeviceStatistics(string deviceKey)
        {
            var cache = GetExistingCache(deviceKey);
            return cache?.GetStatistics() ?? new CacheStatistics();
        }

        /// <summary>
        /// Get detailed manager statistics
        /// </summary>
        public DataCacheManagerStatistics GetManagerStatistics()
        {
            cachesLock.EnterReadLock();
            try
            {
                var deviceStats = new Dictionary<string, CacheStatistics>();
                long totalMemoryUsage = 0;

                foreach (var kvp in deviceCaches)
                {
                    var stats = kvp.Value.GetStatistics();
                    deviceStats[kvp.Key] = stats;
                    totalMemoryUsage += kvp.Value.GetEstimatedMemoryUsage();
                }

                var combinedStats = GetStatistics();

                return new DataCacheManagerStatistics
                {
                    DeviceCount = deviceCaches.Count,
                    TotalItems = combinedStats.TotalItems,
                    TotalValidItems = combinedStats.ValidItems,
                    TotalExpiredItems = combinedStats.ExpiredItems,
                    OverallHitRatio = combinedStats.HitRatio,
                    TotalMemoryUsage = totalMemoryUsage,
                    DeviceStatistics = deviceStats,
                    DefaultLifetimeMs = DefaultCacheLifetime,
                    MaxItemsPerCache = MaxItemsPerCache
                };
            }
            finally
            {
                cachesLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Get list of all device keys
        /// </summary>
        public List<string> GetDeviceKeys()
        {
            cachesLock.EnterReadLock();
            try
            {
                return new List<string>(deviceCaches.Keys);
            }
            finally
            {
                cachesLock.ExitReadLock();
            }
        }

        #endregion

        #region MAINTENANCE

        private void StartMaintenanceTimer()
        {
            maintenanceTimer = new Timer(OnMaintenanceTimer, null,
                                       TimeSpan.FromMilliseconds(MaintenanceInterval),
                                       TimeSpan.FromMilliseconds(MaintenanceInterval));
        }

        private void OnMaintenanceTimer(object state)
        {
            PerformMaintenance();
        }

        /// <summary>
        /// Perform maintenance tasks (cleanup, optimization)
        /// </summary>
        public void PerformMaintenance()
        {
            CleanupExpiredItems();
            RemoveInactiveDeviceCaches();
        }

        /// <summary>
        /// Cleanup expired items in all device caches
        /// </summary>
        public int CleanupExpiredItems()
        {
            int totalCleaned = 0;

            cachesLock.EnterReadLock();
            try
            {
                Parallel.ForEach(deviceCaches.Values, cache =>
                {
                    var cleaned = cache.CleanupExpiredItems();
                    Interlocked.Add(ref totalCleaned, cleaned);
                });
            }
            finally
            {
                cachesLock.ExitReadLock();
            }

            return totalCleaned;
        }

        /// <summary>
        /// Remove device caches that haven't been used recently
        /// </summary>
        public int RemoveInactiveDeviceCaches()
        {
            var inactiveDevices = new List<string>();
            var cutoffTime = DateTime.Now.AddMilliseconds(-DeviceCacheExpiration);

            cachesLock.EnterReadLock();
            try
            {
                foreach (var kvp in deviceLastActivity)
                {
                    if (kvp.Value < cutoffTime)
                    {
                        inactiveDevices.Add(kvp.Key);
                    }
                }
            }
            finally
            {
                cachesLock.ExitReadLock();
            }

            foreach (var deviceKey in inactiveDevices)
            {
                RemoveDeviceCache(deviceKey);
            }

            return inactiveDevices.Count;
        }

        #endregion

        #region DISPOSE

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            // Stop maintenance timer
            maintenanceTimer?.Dispose();
            maintenanceTimer = null;

            // Dispose all caches
            cachesLock.EnterWriteLock();
            try
            {
                foreach (var cache in deviceCaches.Values)
                {
                    cache.Dispose();
                }
                deviceCaches.Clear();
                deviceLastActivity.Clear();
            }
            finally
            {
                cachesLock.ExitWriteLock();
                cachesLock.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// Detailed statistics for DataCacheManager
    /// </summary>
    public class DataCacheManagerStatistics
    {
        public int DeviceCount { get; set; }
        public int TotalItems { get; set; }
        public int TotalValidItems { get; set; }
        public int TotalExpiredItems { get; set; }
        public double OverallHitRatio { get; set; }
        public long TotalMemoryUsage { get; set; }
        public Dictionary<string, CacheStatistics> DeviceStatistics { get; set; }
        public int DefaultLifetimeMs { get; set; }
        public int MaxItemsPerCache { get; set; }

        public string TotalMemoryUsageFormatted => FormatBytes(TotalMemoryUsage);
        public double OverallHitRatioPercentage => OverallHitRatio * 100.0;

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        public override string ToString()
        {
            return $"CacheManager: {DeviceCount} devices, {TotalValidItems}/{TotalItems} items ({OverallHitRatioPercentage:F1}%), {TotalMemoryUsageFormatted}";
        }
    }
}