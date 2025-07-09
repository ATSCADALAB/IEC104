using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IEC104.Adapter;
using IEC104.Configuration;
using IEC104.Constants;
using IEC104.Exceptions;
using IEC104.Cache;
using ATDriver_Server;

namespace IEC104.Core
{
    /// <summary>
    /// Main IEC104 Manager - orchestrates all IEC104 operations
    /// This is the primary interface used by ATDriver
    /// </summary>
    public class IEC104Manager : IDisposable
    {
        #region FIELDS

        private ConnectionManager connectionManager;
        private DataCacheManager cacheManager;
        private readonly Dictionary<string, DeviceAdapter> deviceAdapters;
        private readonly object lockObject = new object();
        private bool disposed = false;

        // Configuration
        private uint channelLifetime = 3600;

        #endregion

        #region PROPERTIES

        /// <summary>
        /// Channel lifetime in seconds
        /// </summary>
        public uint ChannelLifetime
        {
            get => channelLifetime;
            set
            {
                channelLifetime = value;
                // Update all device adapters
                lock (lockObject)
                {
                    foreach (var adapter in deviceAdapters.Values)
                    {
                        if (adapter.GetClientAdapter() != null)
                        {
                            adapter.GetClientAdapter().Lifetime = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Number of active connections
        /// </summary>
        public int ActiveConnections
        {
            get
            {
                lock (lockObject)
                {
                    return deviceAdapters.Values.Count(d => d.Connected);
                }
            }
        }

        /// <summary>
        /// Total number of devices
        /// </summary>
        public int TotalDevices
        {
            get
            {
                lock (lockObject)
                {
                    return deviceAdapters.Count;
                }
            }
        }

        #endregion

        #region EVENTS

        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;
        public event EventHandler<DataChangedEventArgs> DataReceived;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        #endregion

        #region CONSTRUCTOR

        public IEC104Manager()
        {
            deviceAdapters = new Dictionary<string, DeviceAdapter>();
            connectionManager = new ConnectionManager();
            cacheManager = new DataCacheManager();

            // Subscribe to manager events
            connectionManager.ConnectionStateChanged += OnConnectionStateChanged;
            connectionManager.ErrorOccurred += OnErrorOccurred;
        }

        #endregion

        #region DEVICE MANAGEMENT

        /// <summary>
        /// Get or create device adapter
        /// </summary>
        public DeviceAdapter GetOrCreateDevice(string deviceName, string deviceID)
        {
            if (string.IsNullOrEmpty(deviceID))
                throw new ArgumentException("DeviceID cannot be null or empty", nameof(deviceID));

            lock (lockObject)
            {
                var key = GetDeviceKey(deviceName, deviceID);

                if (!deviceAdapters.TryGetValue(key, out DeviceAdapter adapter))
                {
                    // Create new device adapter
                    adapter = new DeviceAdapter(deviceName, deviceID);

                    // Subscribe to events
                    adapter.ConnectionStateChanged += OnDeviceConnectionStateChanged;
                    adapter.DataReceived += OnDeviceDataReceived;
                    adapter.ErrorOccurred += OnDeviceErrorOccurred;

                    // Add to collection
                    deviceAdapters[key] = adapter;

                    // Register with connection manager
                    connectionManager.RegisterDevice(adapter);
                }

                return adapter;
            }
        }

        /// <summary>
        /// Remove device adapter
        /// </summary>
        public bool RemoveDevice(string deviceName, string deviceID)
        {
            lock (lockObject)
            {
                var key = GetDeviceKey(deviceName, deviceID);

                if (deviceAdapters.TryGetValue(key, out DeviceAdapter adapter))
                {
                    // Unsubscribe events
                    adapter.ConnectionStateChanged -= OnDeviceConnectionStateChanged;
                    adapter.DataReceived -= OnDeviceDataReceived;
                    adapter.ErrorOccurred -= OnDeviceErrorOccurred;

                    // Unregister from connection manager
                    connectionManager.UnregisterDevice(adapter);

                    // Dispose adapter
                    adapter.Dispose();

                    // Remove from collection
                    return deviceAdapters.Remove(key);
                }

                return false;
            }
        }

        /// <summary>
        /// Get device adapter if exists
        /// </summary>
        public DeviceAdapter GetDevice(string deviceName, string deviceID)
        {
            lock (lockObject)
            {
                var key = GetDeviceKey(deviceName, deviceID);
                deviceAdapters.TryGetValue(key, out DeviceAdapter adapter);
                return adapter;
            }
        }

        /// <summary>
        /// Check if device exists
        /// </summary>
        public bool DeviceExists(string deviceName, string deviceID)
        {
            lock (lockObject)
            {
                var key = GetDeviceKey(deviceName, deviceID);
                return deviceAdapters.ContainsKey(key);
            }
        }

        private string GetDeviceKey(string deviceName, string deviceID)
        {
            return $"{deviceName}|{deviceID}";
        }

        #endregion

        #region CONNECTION MANAGEMENT

        /// <summary>
        /// Connect to device
        /// </summary>
        public bool Connect(string deviceName, string deviceID)
        {
            try
            {
                var device = GetOrCreateDevice(deviceName, deviceID);
                return device.Connect();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(IEC104ErrorCode.ConnectionFailed,
                    $"Failed to connect device {deviceName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Connect to device using DeviceSettings
        /// </summary>
        public bool Connect(DeviceSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            return Connect(DefaultValues.DefaultDeviceName, settings.DeviceID);
        }

        /// <summary>
        /// Disconnect from device
        /// </summary>
        public bool Disconnect(string deviceName, string deviceID)
        {
            var device = GetDevice(deviceName, deviceID);
            if (device != null)
            {
                return device.Disconnect();
            }
            return false;
        }

        /// <summary>
        /// Check all connections
        /// </summary>
        public void CheckConnections()
        {
            lock (lockObject)
            {
                foreach (var adapter in deviceAdapters.Values)
                {
                    adapter.CheckConnection();
                }
            }
        }

        #endregion

        #region DATA OPERATIONS

        /// <summary>
        /// Read tag value - Main method called by ATDriver
        /// </summary>
        public SendPack ReadTag(string deviceName, string deviceID, string tagAddress, string tagType)
        {
            try
            {
                // Get device adapter
                var device = GetDevice(deviceName, deviceID);
                if (device == null)
                {
                    return CreateErrorSendPack(deviceName, deviceID, tagAddress, tagType, "Device not found");
                }

                // Check connection
                if (!device.Connected)
                {
                    return CreateErrorSendPack(deviceName, deviceID, tagAddress, tagType, "Device not connected");
                }

                // Read tag
                if (device.ReadTag(tagAddress, tagType, out string value, out string quality))
                {
                    return new SendPack
                    {
                        ChannelAddress = ChannelLifetime.ToString(),
                        DeviceID = deviceID,
                        TagAddress = tagAddress,
                        TagType = tagType,
                        Value = value
                    };
                }
                else
                {
                    return CreateErrorSendPack(deviceName, deviceID, tagAddress, tagType, "Read failed");
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(IEC104ErrorCode.DataConversionError,
                    $"Read tag error: {ex.Message}");
                return CreateErrorSendPack(deviceName, deviceID, tagAddress, tagType, "Exception");
            }
        }

        /// <summary>
        /// Write tag value - Main method called by ATDriver
        /// </summary>
        public string WriteTag(SendPack sendPack)
        {
            if (sendPack == null)
                return IEC104Constants.QualityBad;

            try
            {
                // Parse device info from sendPack
                var deviceName = DefaultValues.DefaultDeviceName; // Could be extracted from sendPack if needed

                // Get device adapter
                var device = GetDevice(deviceName, sendPack.DeviceID);
                if (device == null)
                {
                    OnErrorOccurred(IEC104ErrorCode.InvalidDeviceID, "Device not found for write operation");
                    return IEC104Constants.QualityBad;
                }

                // Check connection
                if (!device.Connected)
                {
                    OnErrorOccurred(IEC104ErrorCode.ConnectionLost, "Device not connected for write operation");
                    return IEC104Constants.QualityBad;
                }

                // Write tag
                bool success = device.WriteTag(sendPack.TagAddress, sendPack.TagType, sendPack.Value?.Trim());

                return success ? IEC104Constants.QualityGood : IEC104Constants.QualityBad;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(IEC104ErrorCode.CommandFailed,
                    $"Write tag error: {ex.Message}");
                return IEC104Constants.QualityBad;
            }
        }

        private SendPack CreateErrorSendPack(string deviceName, string deviceID, string tagAddress, string tagType, string errorReason)
        {
            return new SendPack
            {
                ChannelAddress = ChannelLifetime.ToString(),
                DeviceID = deviceID,
                TagAddress = tagAddress,
                TagType = tagType,
                Value = null // ATDriver will interpret null as bad quality
            };
        }

        #endregion

        #region INTERROGATION

        /// <summary>
        /// Send General Interrogation to device
        /// </summary>
        public bool SendGeneralInterrogation(string deviceName, string deviceID)
        {
            var device = GetDevice(deviceName, deviceID);
            if (device != null && device.Connected)
            {
                device.SendGeneralInterrogation();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Send General Interrogation to all connected devices
        /// </summary>
        public void SendGeneralInterrogationToAll()
        {
            lock (lockObject)
            {
                foreach (var adapter in deviceAdapters.Values)
                {
                    if (adapter.Connected)
                    {
                        try
                        {
                            adapter.SendGeneralInterrogation();
                        }
                        catch (Exception ex)
                        {
                            OnErrorOccurred(IEC104ErrorCode.ProtocolError,
                                $"General Interrogation failed for device: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Send Counter Interrogation to device
        /// </summary>
        public bool SendCounterInterrogation(string deviceName, string deviceID)
        {
            var device = GetDevice(deviceName, deviceID);
            if (device != null && device.Connected)
            {
                device.SendCounterInterrogation();
                return true;
            }
            return false;
        }

        #endregion

        #region BLOCK READING

        /// <summary>
        /// Start block reading for all devices
        /// </summary>
        public void StartBlockReading()
        {
            connectionManager.StartBlockReading();
        }

        /// <summary>
        /// Stop block reading for all devices
        /// </summary>
        public void StopBlockReading()
        {
            connectionManager.StopBlockReading();
        }

        #endregion

        #region CACHE MANAGEMENT

        /// <summary>
        /// Clear cache for specific device
        /// </summary>
        public void ClearCache(string deviceName, string deviceID)
        {
            cacheManager.ClearDeviceCache($"{deviceName}|{deviceID}");
        }

        /// <summary>
        /// Clear all cache
        /// </summary>
        public void ClearAllCache()
        {
            cacheManager.ClearAllCache();
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStatistics GetCacheStatistics()
        {
            return cacheManager.GetStatistics();
        }

        #endregion

        #region STATUS & MONITORING

        /// <summary>
        /// Get manager status
        /// </summary>
        public IEC104ManagerStatus GetStatus()
        {
            lock (lockObject)
            {
                return new IEC104ManagerStatus
                {
                    TotalDevices = deviceAdapters.Count,
                    ConnectedDevices = deviceAdapters.Values.Count(d => d.Connected),
                    ChannelLifetime = channelLifetime,
                    CacheStatistics = cacheManager.GetStatistics(),
                    Devices = deviceAdapters.Values.Select(d => new DeviceStatus
                    {
                        DeviceID = d.DeviceID,
                        DeviceName = d.DeviceName,
                        Connected = d.Connected,
                        Settings = d.Settings
                    }).ToList()
                };
            }
        }

        /// <summary>
        /// Get device status list
        /// </summary>
        public List<DeviceStatus> GetDeviceStatusList()
        {
            lock (lockObject)
            {
                return deviceAdapters.Values.Select(d => new DeviceStatus
                {
                    DeviceID = d.DeviceID,
                    DeviceName = d.DeviceName,
                    Connected = d.Connected,
                    Settings = d.Settings
                }).ToList();
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void OnConnectionStateChanged(object sender, ConnectionEventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, e);
        }

        private void OnErrorOccurred(object sender, ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        private void OnDeviceConnectionStateChanged(object sender, ConnectionEventArgs e)
        {
            ConnectionStateChanged?.Invoke(sender, e);
        }

        private void OnDeviceDataReceived(object sender, DataChangedEventArgs e)
        {
            // Update cache
            if (sender is DeviceAdapter device)
            {
                cacheManager.UpdateCache($"{device.DeviceName}|{device.DeviceID}", e.IOA, e.Value, e.Quality);
            }

            DataReceived?.Invoke(sender, e);
        }

        private void OnDeviceErrorOccurred(object sender, ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(sender, e);
        }

        private void OnErrorOccurred(IEC104ErrorCode errorCode, string message)
        {
            ErrorOccurred?.Invoke(this, new ErrorEventArgs
            {
                ErrorCode = errorCode,
                Message = message,
                Timestamp = DateTime.Now
            });
        }

        #endregion

        #region DISPOSE

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            StopBlockReading();

            // Dispose all device adapters
            lock (lockObject)
            {
                foreach (var adapter in deviceAdapters.Values)
                {
                    try
                    {
                        adapter.ConnectionStateChanged -= OnDeviceConnectionStateChanged;
                        adapter.DataReceived -= OnDeviceDataReceived;
                        adapter.ErrorOccurred -= OnDeviceErrorOccurred;
                        adapter.Dispose();
                    }
                    catch { }
                }
                deviceAdapters.Clear();
            }

            // Dispose managers
            connectionManager?.Dispose();
            cacheManager?.Dispose();
        }

        #endregion
    }

    #region STATUS CLASSES

    public class IEC104ManagerStatus
    {
        public int TotalDevices { get; set; }
        public int ConnectedDevices { get; set; }
        public uint ChannelLifetime { get; set; }
        public CacheStatistics CacheStatistics { get; set; }
        public List<DeviceStatus> Devices { get; set; }
    }

    public class DeviceStatus
    {
        public string DeviceID { get; set; }
        public string DeviceName { get; set; }
        public bool Connected { get; set; }
        public DeviceSettings Settings { get; set; }
    }

    #endregion
}