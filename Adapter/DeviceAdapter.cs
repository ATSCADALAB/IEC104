using System;
using System.Collections.Generic;
using IEC104.Configuration;
using IEC104.Constants;
using IEC104.Exceptions;

namespace IEC104.Adapter
{
    /// <summary>
    /// Device-level adapter - manages one IEC104 device connection
    /// </summary>
    public class DeviceAdapter : IDisposable
    {
        #region FIELDS

        private IEC104ClientAdapter clientAdapter;
        private DeviceSettings deviceSettings;
        private readonly Dictionary<string, TagAdapter> tagAdapters;
        private bool disposed = false;

        #endregion

        #region PROPERTIES

        public string DeviceID { get; private set; }
        public string DeviceName { get; set; }
        public bool Connected => clientAdapter?.Connected ?? false;
        public DeviceSettings Settings => deviceSettings;

        #endregion

        #region EVENTS

        public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;
        public event EventHandler<DataChangedEventArgs> DataReceived;
        public event EventHandler<ErrorEventArgs> ErrorOccurred;

        #endregion

        #region CONSTRUCTOR

        public DeviceAdapter(string deviceName, string deviceID)
        {
            DeviceName = deviceName;
            DeviceID = deviceID;
            tagAdapters = new Dictionary<string, TagAdapter>();

            // Parse device settings
            deviceSettings = DeviceSettings.Initialize(deviceID);

            // Create client adapter
            clientAdapter = new IEC104ClientAdapter(deviceName, deviceSettings);

            // Subscribe to events
            clientAdapter.ConnectionStateChanged += OnConnectionStateChanged;
            clientAdapter.DataReceived += OnDataReceived;
            clientAdapter.ErrorOccurred += OnErrorOccurred;
        }

        #endregion

        #region CONNECTION MANAGEMENT

        public bool Connect()
        {
            return clientAdapter?.Connect() ?? false;
        }

        public bool Disconnect()
        {
            return clientAdapter?.Disconnect() ?? false;
        }

        public void CheckConnection()
        {
            clientAdapter?.CheckLifeTime();
        }

        #endregion

        #region TAG MANAGEMENT

        public TagAdapter GetOrCreateTagAdapter(string tagAddress, string tagType)
        {
            var key = $"{tagAddress}:{tagType}";

            if (!tagAdapters.TryGetValue(key, out TagAdapter tagAdapter))
            {
                tagAdapter = new TagAdapter(this, tagAddress, tagType);
                tagAdapters[key] = tagAdapter;
            }

            return tagAdapter;
        }

        public bool RemoveTagAdapter(string tagAddress, string tagType)
        {
            var key = $"{tagAddress}:{tagType}";

            if (tagAdapters.TryGetValue(key, out TagAdapter tagAdapter))
            {
                tagAdapter.Dispose();
                return tagAdapters.Remove(key);
            }

            return false;
        }

        #endregion

        #region DATA OPERATIONS

        public bool ReadTag(string tagAddress, string tagType, out string value, out string quality)
        {
            value = null;
            quality = "Bad";

            try
            {
                var tagAdapter = GetOrCreateTagAdapter(tagAddress, tagType);
                return tagAdapter.ReadValue(out value, out quality);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(IEC104ErrorCode.DataConversionError, $"Read tag failed: {ex.Message}");
                return false;
            }
        }

        public bool WriteTag(string tagAddress, string tagType, string value)
        {
            try
            {
                var tagAdapter = GetOrCreateTagAdapter(tagAddress, tagType);
                return tagAdapter.WriteValue(value);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(IEC104ErrorCode.CommandFailed, $"Write tag failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region INTERROGATION

        public void SendGeneralInterrogation()
        {
            clientAdapter?.SendGeneralInterrogation();
        }

        public void SendCounterInterrogation()
        {
            clientAdapter?.SendCounterInterrogation();
        }

        #endregion

        #region EVENT HANDLERS

        private void OnConnectionStateChanged(object sender, ConnectionEventArgs e)
        {
            ConnectionStateChanged?.Invoke(this, e);
        }

        private void OnDataReceived(object sender, DataChangedEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }

        private void OnErrorOccurred(object sender, ErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        #endregion

        #region DISPOSE

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;

            // Dispose all tag adapters
            foreach (var tagAdapter in tagAdapters.Values)
            {
                tagAdapter.Dispose();
            }
            tagAdapters.Clear();

            // Dispose client adapter
            if (clientAdapter != null)
            {
                clientAdapter.ConnectionStateChanged -= OnConnectionStateChanged;
                clientAdapter.DataReceived -= OnDataReceived;
                clientAdapter.ErrorOccurred -= OnErrorOccurred;
                clientAdapter.Dispose();
                clientAdapter = null;
            }
        }

        #endregion

        #region INTERNAL ACCESS

        internal IEC104ClientAdapter GetClientAdapter()
        {
            return clientAdapter;
        }

        #endregion
    }
}