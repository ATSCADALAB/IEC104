// =====================================================
// File: Cache/IOACache.cs
// Cache per IOA with detailed tracking and statistics
// =====================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IEC104.Protocol.Enum;
using IEC104.Common;
using IEC104.Constants;

namespace IEC104.Cache
{
    /// <summary>
    /// Cache for individual IOA data with detailed tracking and statistics
    /// Optimized for IEC104 Information Object Address management
    /// </summary>
    public class IOACache : IDisposable
    {
        #region FIELDS

        private readonly Dictionary<int, IOACacheEntry> ioaData;
        private readonly Dictionary<TypeId, List<int>> typeIdIndex;
        private readonly ReaderWriterLockSlim lockObject;
        private Timer statisticsTimer;
        private bool disposed = false;

        // Statistics tracking
        private long totalReads = 0;
        private long totalWrites = 0;
        private long cacheHits = 0;
        private long cacheMisses = 0;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// Current number of IOAs in cache
        /// </summary>
        public int Count
        {
            get
            {
                lockObject.EnterReadLock();
                try
                {
                    return ioaData.Count;
                }
                finally
                {
                    lockObject.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Maximum number of IOAs to cache (0 = unlimited)
        /// </summary>
        public int MaxIOAs { get; set; } = 10000;

        /// <summary>
        /// Enable detailed statistics tracking
        /// </summary>
        public bool EnableStatistics { get; set; } = true;

        /// <summary>
        /// Cache hit ratio (0.0 to 1.0)
        /// </summary>
        public double HitRatio
        {
            get
            {
                long totalAccess = cacheHits + cacheMisses;
                return totalAccess > 0 ? (double)cacheHits / totalAccess : 0.0;
            }
        }

        #endregion

        #region CONSTRUCTOR

        public IOACache()
        {
            ioaData = new Dictionary<int, IOACacheEntry>();
            typeIdIndex = new Dictionary<TypeId, List<int>>();
            lockObject = new ReaderWriterLockSlim();

            if (EnableStatistics)
            {
                StartStatisticsTimer();
            }
        }

        public IOACache(int maxIOAs, bool enableStatistics = true) : this()
        {
            MaxIOAs = maxIOAs;
            EnableStatistics = enableStatistics;
        }

        #endregion

        #region CACHE OPERATIONS

        /// <summary>
        /// Update IOA data with full information
        /// </summary>
        /// <param name="ioa">Information Object Address</param>
        /// <param name="typeId">IEC104 Type ID</param>
        /// <param name="value">Data value</param>
        /// <param name="quality">Data quality</param>
        /// <param name="timestamp">Data timestamp (optional)</param>
        public void UpdateIOA(int ioa, TypeId typeId, object value, string quality, DateTime? timestamp = null)
        {
            ValidateIOA(ioa);

            var entry = new IOACacheEntry
            {
                IOA = ioa,
                TypeId = typeId,
                DataType = GetDataTypeFromTypeId(typeId),
                Value = value,
                Quality = quality ?? "Bad",
                Timestamp = timestamp ?? DateTime.Now,
                LastUpdateTime = DateTime.Now,
                UpdateCount = 1,
                AccessCount = 0,
                LastAccessTime = DateTime.MinValue
            };

            lockObject.EnterWriteLock();
            try
            {
                // Check capacity
                if (MaxIOAs > 0 && ioaData.Count >= MaxIOAs && !ioaData.ContainsKey(ioa))
                {
                    RemoveLeastUsedIOA();
                }

                // Update existing entry statistics
                if (ioaData.TryGetValue(ioa, out IOACacheEntry existing))
                {
                    entry.UpdateCount = existing.UpdateCount + 1;
                    entry.AccessCount = existing.AccessCount;
                    entry.LastAccessTime = existing.LastAccessTime;
                    entry.FirstUpdateTime = existing.FirstUpdateTime;

                    // Remove from old type index
                    RemoveFromTypeIndex(existing.TypeId, ioa);
                }
                else
                {
                    entry.FirstUpdateTime = entry.Timestamp;
                }

                // Update data
                ioaData[ioa] = entry;

                // Update type index
                AddToTypeIndex(typeId, ioa);

                // Statistics
                if (EnableStatistics)
                {
                    Interlocked.Increment(ref totalWrites);
                }
            }
            finally
            {
                lockObject.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get IOA data entry
        /// </summary>
        /// <param name="ioa">Information Object Address</param>
        /// <param name="entry">Retrieved entry</param>
        /// <returns>True if IOA exists</returns>
        public bool GetIOA(int ioa, out IOACacheEntry entry)
        {
            entry = null;

            lockObject.EnterUpgradeableReadLock();
            try
            {
                if (ioaData.TryGetValue(ioa, out entry))
                {
                    // Update access statistics
                    lockObject.EnterWriteLock();
                    try
                    {
                        entry.AccessCount++;
                        entry.LastAccessTime = DateTime.Now;
                    }
                    finally
                    {
                        lockObject.ExitWriteLock();
                    }

                    // Statistics
                    if (EnableStatistics)
                    {
                        Interlocked.Increment(ref totalReads);
                        Interlocked.Increment(ref cacheHits);
                    }

                    return true;
                }

                // Statistics
                if (EnableStatistics)
                {
                    Interlocked.Increment(ref totalReads);
                    Interlocked.Increment(ref cacheMisses);
                }

                return false;
            }
            finally
            {
                lockObject.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        /// Get IOA value only
        /// </summary>
        /// <param name="ioa">Information Object Address</param>
        /// <param name="value">Retrieved value</param>
        /// <param name="quality">Retrieved quality</param>
        /// <returns>True if IOA exists</returns>
        public bool GetIOAValue(int ioa, out object value, out string quality)
        {
            value = null;
            quality = "Bad";

            if (GetIOA(ioa, out IOACacheEntry entry))
            {
                value = entry.Value;
                quality = entry.Quality;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if IOA exists in cache
        /// </summary>
        /// <param name="ioa">Information Object Address</param>
        /// <returns>True if IOA exists</returns>
        public bool ContainsIOA(int ioa)
        {
            lockObject.EnterReadLock();
            try
            {
                return ioaData.ContainsKey(ioa);
            }
            finally
            {
                lockObject.ExitReadLock();
            }
        }

        /// <summary>
        /// Remove specific IOA from cache
        /// </summary>
        /// <param name="ioa">Information Object Address</param>
        /// <returns>True if IOA was removed</returns>
        public bool RemoveIOA(int ioa)
        {
            lockObject.EnterWriteLock();
            try
            {
                if (ioaData.TryGetValue(ioa, out IOACacheEntry entry))
                {
                    ioaData.Remove(ioa);
                    RemoveFromTypeIndex(entry.TypeId, ioa);
                    return true;
                }
                return false;
            }
            finally
            {
                lockObject.ExitWriteLock();
            }
        }

        #endregion

        #region BULK OPERATIONS

        /// <summary>
        /// Get all IOAs in cache
        /// </summary>
        /// <returns>List of all IOA entries</returns>
        public List<IOACacheEntry> GetAllIOAs()
        {
            lockObject.EnterReadLock();
            try
            {
                return new List<IOACacheEntry>(ioaData.Values);
            }
            finally
            {
                lockObject.ExitReadLock();
            }
        }

        /// <summary>
        /// Get IOAs by TypeId
        /// </summary>
        /// <param name="typeId">IEC104 Type ID</param>
        /// <returns>List of IOA entries with specified TypeId</returns>
        public List<IOACacheEntry> GetIOAsByType(TypeId typeId)
        {
            var result = new List<IOACacheEntry>();

            lockObject.EnterReadLock();
            try
            {
                if (typeIdIndex.TryGetValue(typeId, out List<int> ioaList))
                {
                    foreach (int ioa in ioaList)
                    {
                        if (ioaData.TryGetValue(ioa, out IOACacheEntry entry))
                        {
                            result.Add(entry);
                        }
                    }
                }
            }
            finally
            {
                lockObject.ExitReadLock();
            }

            return result;
        }

        /// <summary>
        /// Get IOAs by data type
        /// </summary>
        /// <param name="dataType">IEC104 data type</param>
        /// <returns>List of IOA entries with specified data type</returns>
        public List<IOACacheEntry> GetIOAsByDataType(IEC104DataType dataType)
        {
            var result = new List<IOACacheEntry>();

            lockObject.EnterReadLock();
            try
            {
                foreach (var entry in ioaData.Values)
                {
                    if (entry.DataType == dataType)
                    {
                        result.Add(entry);
                    }
                }
            }
            finally
            {
                lockObject.ExitReadLock();
            }

            return result;
        }

        /// <summary>
        /// Get IOAs by quality
        /// </summary>
        /// <param name="quality">Data quality</param>
        /// <returns>List of IOA entries with specified quality</returns>
        public List<IOACacheEntry> GetIOAsByQuality(string quality)
        {
            var result = new List<IOACacheEntry>();

            lockObject.EnterReadLock();
            try
            {
                foreach (var entry in ioaData.Values)
                {
                    if (string.Equals(entry.Quality, quality, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(entry);
                    }
                }
            }
            finally
            {
                lockObject.ExitReadLock();
            }

            return result;
        }

        /// <summary>
        /// Get IOAs in range
        /// </summary>
        /// <param name="startIOA">Start IOA (inclusive)</param>
        /// <param name="endIOA">End IOA (inclusive)</param>
        /// <returns>List of IOA entries in range</returns>
        public List<IOACacheEntry> GetIOAsInRange(int startIOA, int endIOA)
        {
            var result = new List<IOACacheEntry>();

            lockObject.EnterReadLock();
            try
            {
                foreach (var kvp in ioaData)
                {
                    if (kvp.Key >= startIOA && kvp.Key <= endIOA)
                    {
                        result.Add(kvp.Value);
                    }
                }
            }
            finally
            {
                lockObject.ExitReadLock();
            }

            // Sort by IOA
            result.Sort((x, y) => x.IOA.CompareTo(y.IOA));
            return result;
        }

        /// <summary>
        /// Update multiple IOAs at once
        /// </summary>
        /// <param name="updates">Dictionary of IOA -> (TypeId, Value, Quality)</param>
        public void UpdateMultipleIOAs(Dictionary<int, (TypeId typeId, object value, string quality)> updates)
        {
            if (updates == null || updates.Count == 0) return;

            lockObject.EnterWriteLock();
            try
            {
                foreach (var kvp in updates)
                {
                    var ioa = kvp.Key;
                    var (typeId, value, quality) = kvp.Value;

                    var entry = new IOACacheEntry
                    {
                        IOA = ioa,
                        TypeId = typeId,
                        DataType = GetDataTypeFromTypeId(typeId),
                        Value = value,
                        Quality = quality ?? "Bad",
                        Timestamp = DateTime.Now,
                        LastUpdateTime = DateTime.Now,
                        UpdateCount = 1,
                        AccessCount = 0,
                        LastAccessTime = DateTime.MinValue
                    };

                    // Update existing entry statistics
                    if (ioaData.TryGetValue(ioa, out IOACacheEntry existing))
                    {
                        entry.UpdateCount = existing.UpdateCount + 1;
                        entry.AccessCount = existing.AccessCount;
                        entry.LastAccessTime = existing.LastAccessTime;
                        entry.FirstUpdateTime = existing.FirstUpdateTime;

                        RemoveFromTypeIndex(existing.TypeId, ioa);
                    }
                    else
                    {
                        entry.FirstUpdateTime = entry.Timestamp;
                    }

                    ioaData[ioa] = entry;
                    AddToTypeIndex(typeId, ioa);
                }

                // Statistics
                if (EnableStatistics)
                {
                    Interlocked.Add(ref totalWrites, updates.Count);
                }
            }
            finally
            {
                lockObject.ExitWriteLock();
            }
        }

        /// <summary>
        /// Clear all IOA data
        /// </summary>
        public void Clear()
        {
            lockObject.EnterWriteLock();
            try
            {
                ioaData.Clear();
                typeIdIndex.Clear();

                // Reset statistics
                if (EnableStatistics)
                {
                    totalReads = 0;
                    totalWrites = 0;
                    cacheHits = 0;
                    cacheMisses = 0;
                }
            }
            finally
            {
                lockObject.ExitWriteLock();
            }
        }

        #endregion

        #region TYPE INDEX MANAGEMENT

        private void AddToTypeIndex(TypeId typeId, int ioa)
        {
            // Must be called within write lock
            if (!typeIdIndex.TryGetValue(typeId, out List<int> ioaList))
            {
                ioaList = new List<int>();
                typeIdIndex[typeId] = ioaList;
            }

            if (!ioaList.Contains(ioa))
            {
                ioaList.Add(ioa);
            }
        }

        private void RemoveFromTypeIndex(TypeId typeId, int ioa)
        {
            // Must be called within write lock
            if (typeIdIndex.TryGetValue(typeId, out List<int> ioaList))
            {
                ioaList.Remove(ioa);

                if (ioaList.Count == 0)
                {
                    typeIdIndex.Remove(typeId);
                }
            }
        }

        /// <summary>
        /// Get all TypeIds in cache
        /// </summary>
        /// <returns>List of TypeIds</returns>
        public List<TypeId> GetAllTypeIds()
        {
            lockObject.EnterReadLock();
            try
            {
                return new List<TypeId>(typeIdIndex.Keys);
            }
            finally
            {
                lockObject.ExitReadLock();
            }
        }

        /// <summary>
        /// Get IOA count by TypeId
        /// </summary>
        /// <returns>Dictionary of TypeId -> Count</returns>
        public Dictionary<TypeId, int> GetTypeIdCounts()
        {
            var result = new Dictionary<TypeId, int>();

            lockObject.EnterReadLock();
            try
            {
                foreach (var kvp in typeIdIndex)
                {
                    result[kvp.Key] = kvp.Value.Count;
                }
            }
            finally
            {
                lockObject.ExitReadLock();
            }

            return result;
        }

        #endregion

        #region MAINTENANCE

        /// <summary>
        /// Remove least recently used IOA
        /// </summary>
        private void RemoveLeastUsedIOA()
        {
            // Must be called within write lock
            if (ioaData.Count == 0) return;

            int lruIOA = 0;
            DateTime oldestAccess = DateTime.MaxValue;
            long lowestAccessCount = long.MaxValue;

            foreach (var kvp in ioaData)
            {
                var entry = kvp.Value;

                // Prioritize by access count, then by last access time
                if (entry.AccessCount < lowestAccessCount ||
                    (entry.AccessCount == lowestAccessCount && entry.LastAccessTime < oldestAccess))
                {
                    lowestAccessCount = entry.AccessCount;
                    oldestAccess = entry.LastAccessTime;
                    lruIOA = kvp.Key;
                }
            }

            if (lruIOA != 0)
            {
                if (ioaData.TryGetValue(lruIOA, out IOACacheEntry entry))
                {
                    ioaData.Remove(lruIOA);
                    RemoveFromTypeIndex(entry.TypeId, lruIOA);
                }
            }
        }

        /// <summary>
        /// Remove IOAs with specific quality
        /// </summary>
        /// <param name="quality">Quality to remove</param>
        /// <returns>Number of IOAs removed</returns>
        public int RemoveIOAsByQuality(string quality)
        {
            var ioasToRemove = new List<int>();

            lockObject.EnterReadLock();
            try
            {
                foreach (var kvp in ioaData)
                {
                    if (string.Equals(kvp.Value.Quality, quality, StringComparison.OrdinalIgnoreCase))
                    {
                        ioasToRemove.Add(kvp.Key);
                    }
                }
            }
            finally
            {
                lockObject.ExitReadLock();
            }

            lockObject.EnterWriteLock();
            try
            {
                foreach (int ioa in ioasToRemove)
                {
                    if (ioaData.TryGetValue(ioa, out IOACacheEntry entry))
                    {
                        ioaData.Remove(ioa);
                        RemoveFromTypeIndex(entry.TypeId, ioa);
                    }
                }
            }
            finally
            {
                lockObject.ExitWriteLock();
            }

            return ioasToRemove.Count;
        }

        /// <summary>
        /// Remove old IOAs (not updated recently)
        /// </summary>
        /// <param name="maxAge">Maximum age to keep</param>
        /// <returns>Number of IOAs removed</returns>
        public int RemoveOldIOAs(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.Now - maxAge;
            var ioasToRemove = new List<int>();

            lockObject.EnterReadLock();
            try
            {
                foreach (var kvp in ioaData)
                {
                    if (kvp.Value.LastUpdateTime < cutoffTime)
                    {
                        ioasToRemove.Add(kvp.Key);
                    }
                }
            }
            finally
            {
                lockObject.ExitReadLock();
            }

            lockObject.EnterWriteLock();
            try
            {
                foreach (int ioa in ioasToRemove)
                {
                    if (ioaData.TryGetValue(ioa, out IOACacheEntry entry))
                    {
                        ioaData.Remove(ioa);
                        RemoveFromTypeIndex(entry.TypeId, ioa);
                    }
                }
            }
            finally
            {
                lockObject.ExitWriteLock();
            }

            return ioasToRemove.Count;
        }

        #endregion

        #region STATISTICS

        private void StartStatisticsTimer()
        {
            // Update statistics every 60 seconds
            statisticsTimer = new Timer(OnStatisticsTimer, null,
                                      TimeSpan.FromSeconds(60),
                                      TimeSpan.FromSeconds(60));
        }

        private void OnStatisticsTimer(object state)
        {
            // Could log statistics or perform optimization here
            var stats = GetStatistics();
            // Optional: Log statistics
        }

        /// <summary>
        /// Get detailed cache statistics
        /// </summary>
        public IOACacheStatistics GetStatistics()
        {
            lockObject.EnterReadLock();
            try
            {
                var stats = new IOACacheStatistics
                {
                    TotalIOAs = ioaData.Count,
                    MaxIOAs = MaxIOAs,
                    TotalReads = totalReads,
                    TotalWrites = totalWrites,
                    CacheHits = cacheHits,
                    CacheMisses = cacheMisses,
                    HitRatio = HitRatio,
                    TypeIdCounts = GetTypeIdCounts(),
                    QualityCounts = new Dictionary<string, int>(),
                    MemoryEstimateBytes = EstimateMemoryUsage()
                };

                // Calculate quality distribution
                foreach (var entry in ioaData.Values)
                {
                    if (stats.QualityCounts.ContainsKey(entry.Quality))
                        stats.QualityCounts[entry.Quality]++;
                    else
                        stats.QualityCounts[entry.Quality] = 1;
                }

                // Calculate age statistics
                if (ioaData.Count > 0)
                {
                    var ages = ioaData.Values.Select(e => DateTime.Now - e.LastUpdateTime).ToList();
                    stats.AverageAgeSeconds = ages.Average(a => a.TotalSeconds);
                    stats.OldestAgeSeconds = ages.Max(a => a.TotalSeconds);
                    stats.NewestAgeSeconds = ages.Min(a => a.TotalSeconds);
                }

                return stats;
            }
            finally
            {
                lockObject.ExitReadLock();
            }
        }

        /// <summary>
        /// Estimate memory usage in bytes
        /// </summary>
        private long EstimateMemoryUsage()
        {
            // Must be called within read lock
            long totalBytes = 0;

            foreach (var kvp in ioaData)
            {
                // IOA key: 4 bytes
                totalBytes += 4;

                // Entry object overhead: ~100 bytes
                totalBytes += 100;

                // Value estimation
                if (kvp.Value.Value is string str)
                    totalBytes += str.Length * 2; // Unicode
                else if (kvp.Value.Value != null)
                    totalBytes += 32; // Rough estimate

                // Quality string
                totalBytes += (kvp.Value.Quality?.Length ?? 0) * 2;
            }

            // Type index overhead
            totalBytes += typeIdIndex.Count * 64;
            foreach (var list in typeIdIndex.Values)
            {
                totalBytes += list.Count * 4; // int per IOA
            }

            return totalBytes;
        }

        /// <summary>
        /// Reset statistics counters
        /// </summary>
        public void ResetStatistics()
        {
            Interlocked.Exchange(ref totalReads, 0);
            Interlocked.Exchange(ref totalWrites, 0);
            Interlocked.Exchange(ref cacheHits, 0);
            Interlocked.Exchange(ref cacheMisses, 0);
        }

        #endregion

        #region UTILITY METHODS

        private void ValidateIOA(int ioa)
        {
            if (ioa < IEC104Constants.MinIOA || ioa > IEC104Constants.MaxIOA)
            {
                throw new ArgumentOutOfRangeException(nameof(ioa),
                    $"IOA must be between {IEC104Constants.MinIOA} and {IEC104Constants.MaxIOA}");
            }
        }

        private IEC104DataType GetDataTypeFromTypeId(TypeId typeId)
        {
            // Convert TypeId to IEC104DataType
            return (IEC104DataType)(int)typeId;
        }

        #endregion

        #region DISPOSE

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            statisticsTimer?.Dispose();
            statisticsTimer = null;

            lockObject.EnterWriteLock();
            try
            {
                ioaData.Clear();
                typeIdIndex.Clear();
            }
            finally
            {
                lockObject.ExitWriteLock();
                lockObject.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// Individual IOA cache entry with comprehensive metadata
    /// </summary>
    public class IOACacheEntry
    {
        public int IOA { get; set; }
        public TypeId TypeId { get; set; }
        public IEC104DataType DataType { get; set; }
        public object Value { get; set; }
        public string Quality { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public DateTime FirstUpdateTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public long UpdateCount { get; set; }
        public long AccessCount { get; set; }

        /// <summary>
        /// Age since last update
        /// </summary>
        public TimeSpan Age => DateTime.Now - LastUpdateTime;

        /// <summary>
        /// Time since last access
        /// </summary>
        public TimeSpan TimeSinceLastAccess => DateTime.Now - LastAccessTime;

        /// <summary>
        /// Total time in cache
        /// </summary>
        public TimeSpan TimeInCache => DateTime.Now - FirstUpdateTime;

        /// <summary>
        /// Update frequency (updates per hour)
        /// </summary>
        public double UpdateFrequency
        {
            get
            {
                var timeInCache = TimeInCache.TotalHours;
                return timeInCache > 0 ? UpdateCount / timeInCache : 0;
            }
        }

        /// <summary>
        /// Access frequency (accesses per hour)
        /// </summary>
        public double AccessFrequency
        {
            get
            {
                var timeInCache = TimeInCache.TotalHours;
                return timeInCache > 0 ? AccessCount / timeInCache : 0;
            }
        }

        public override string ToString()
        {
            return $"IOA {IOA} ({TypeId}): {Value} [{Quality}] - Updates: {UpdateCount}, Accesses: {AccessCount}";
        }
    }

    /// <summary>
    /// Comprehensive statistics for IOA cache
    /// </summary>
    public class IOACacheStatistics
    {
        public int TotalIOAs { get; set; }
        public int MaxIOAs { get; set; }
        public long TotalReads { get; set; }
        public long TotalWrites { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public double HitRatio { get; set; }
        public Dictionary<TypeId, int> TypeIdCounts { get; set; }
        public Dictionary<string, int> QualityCounts { get; set; }
        public double AverageAgeSeconds { get; set; }
        public double OldestAgeSeconds { get; set; }
        public double NewestAgeSeconds { get; set; }
        public long MemoryEstimateBytes { get; set; }

        public double HitRatioPercentage => HitRatio * 100.0;
        public string MemoryEstimateFormatted => FormatBytes(MemoryEstimateBytes);
        public double UtilizationPercentage => MaxIOAs > 0 ? (double)TotalIOAs / MaxIOAs * 100.0 : 0.0;

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
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
            return $"IOACache: {TotalIOAs} IOAs, {HitRatioPercentage:F1}% hit ratio, {MemoryEstimateFormatted}";
        }
    }
}