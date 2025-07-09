using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IEC104.Adapter;
using IEC104.Constants;
using IEC104.Events;

namespace IEC104.Core
{
    /// <summary>
    /// Manages multiple IEC104 connections
    /// </summary>
    public class ConnectionManager : IDisposable
    {
        #region FIELDS

        private readonly List<DeviceAdapter> registeredDevices;
        private readonly object lockObject = new object();
        private Timer connectionCheckTimer;
        private Timer blockReadingTimer;
        private bool disposed = false;
        private bool blockReadingEnabled = false;

        #endregion

        #region EVENTS

        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        #endregion

        #region CONSTRUCTOR

        public ConnectionManager()
        {
            registeredDevices = new List<DeviceAdapter>();
            StartConnectionMonitoring();
        }

        #endregion

        #region DEVICE REGISTRATION

        /// <summary>
        /// Register device for management
        /// </summary>
        public void RegisterDevice(DeviceAdapter device)
        {
            if (device == null) return;

            lock (lockObject)
            {
                if (!registeredDevices.Contains(device))
                {
                    registeredDevices.Add(device);

                    // Subscribe to device events
                    device.ConnectionStateChanged += OnDeviceConnectionStateChanged;
                    device.ErrorOccurred += OnDeviceErrorOccurred;
                }
            }
        }

        /// <summary>
        /// Unregister device from management
        /// </summary>
        public void UnregisterDevice(DeviceAdapter device)
        {
            if (device == null) return;

            lock (lockObject)
            {
                if (registeredDevices.Contains(device))
                {
                    // Unsubscribe from device events
                    device.ConnectionStateChanged -= OnDeviceConnectionStateChanged;
                    device.ErrorOccurred -= OnDeviceErrorOccurred;

                    registeredDevices.Remove(device);
                }
            }
        }

        #endregion

        #region CONNECTION MONITORING

        private void StartConnectionMonitoring()
        {
            // Check connections every 30 seconds
            connectionCheckTimer = new Timer(OnConnectionCheckTimer, null,
                                           TimeSpan.FromSeconds(30),
                                           TimeSpan.FromSeconds(30));
        }

        private void OnConnectionCheckTimer(object state)
        {
            CheckAllConnections();
        }

        /// <summary>
        /// Check all registered connections
        /// </summary>
        public void CheckAllConnections()
        {
            List<DeviceAdapter> devicesToCheck;

            lock (lockObject)
            {
                devicesToCheck = new List<DeviceAdapter>(registeredDevices);
            }

            foreach (var device in devicesToCheck)
            {
                try
                {
                    device.CheckConnection();
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(IEC104ErrorCode.ConnectionFailed,
                        $"Connection check failed for device {device.DeviceName}: {ex.Message}");
                }
            }
        }

        #endregion

        #region BLOCK READING

        /// <summary>
        /// Start block reading for all devices
        /// </summary>
        public void StartBlockReading()
        {
            if (blockReadingEnabled) return;

            blockReadingEnabled = true;

            // Start block reading timer (every 5 seconds)
            blockReadingTimer = new Timer(OnBlockReadingTimer, null,
                                        TimeSpan.FromSeconds(5),
                                        TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Stop block reading for all devices
        /// </summary>
        public void StopBlockReading()
        {
            blockReadingEnabled = false;
            blockReadingTimer?.Dispose();
            blockReadingTimer = null;
        }

        private void OnBlockReadingTimer(object state)
        {
            if (!blockReadingEnabled) return;

            PerformBlockReading();
        }

        private void PerformBlockReading()
        {
            List<DeviceAdapter> connectedDevices;

            lock (lockObject)
            {
                connectedDevices = new List<DeviceAdapter>();
                foreach (var device in registeredDevices)
                {
                    if (device.Connected)
                    {
                        connectedDevices.Add(device);
                    }
                }
            }

            // Perform parallel block reading
            Parallel.ForEach(connectedDevices, device =>
            {
                try
                {
                    // Send general interrogation periodically
                    device.SendGeneralInterrogation();
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(IEC104ErrorCode.ProtocolError,
                        $"Block reading failed for device {device.DeviceName}: {ex.Message}");
                }
            });
        }

        #endregion

        #region STATISTICS

        /// <summary>
        /// Get connection statistics
        /// </summary>
        public ConnectionStatistics GetStatistics()
        {
            lock (lockObject)
            {
                int connected = 0;
                int total = registeredDevices.Count;

                foreach (var device in registeredDevices)
                {
                    if (device.Connected)
                        connected++;
                }

                return new ConnectionStatistics
                {
                    TotalDevices = total,
                    ConnectedDevices = connected,
                    DisconnectedDevices = total - connected,
                    BlockReadingEnabled = blockReadingEnabled
                };
            }
        }

        #endregion

        #region EVENT HANDLERS

        private void OnDeviceConnectionStateChanged(object sender, ConnectionEventArgs e)
        {
            ConnectionStateChanged?.Invoke(sender, e);
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

            connectionCheckTimer?.Dispose();
            connectionCheckTimer = null;

            lock (lockObject)
            {
                registeredDevices.Clear();
            }
        }

        #endregion
    }

    public class ConnectionStatistics
    {
        public int TotalDevices { get; set; }
        public int ConnectedDevices { get; set; }
        public int DisconnectedDevices { get; set; }
        public bool BlockReadingEnabled { get; set; }
    }
}