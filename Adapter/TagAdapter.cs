using System;
using IEC104.Configuration;
using IEC104.Mapping;
using IEC104.Common;
using IEC104.Protocol.Enum;
using IEC104.Constants;
using IEC104.Exceptions;
using IEC104.Protocol.IE;

namespace IEC104.Adapter
{
    /// <summary>
    /// Tag-level adapter - manages one IEC104 tag (IOA)
    /// </summary>
    public class TagAdapter : IDisposable
    {
        #region FIELDS

        private DeviceAdapter deviceAdapter;
        private TagSettings tagSettings;
        private DateTime lastReadTime;
        private string lastValue;
        private string lastQuality;
        private bool disposed = false;

        #endregion

        #region PROPERTIES

        public int IOA => tagSettings.IOA;
        public TypeId TypeId => tagSettings.TypeId;
        public IEC104DataType DataType => tagSettings.DataType;
        public string TagAddress => tagSettings.TagAddress;
        public string TagType => tagSettings.TagType;
        public bool IsReadable => IEC104DataTypeHelper.IsReadable(DataType);
        public bool IsWritable => IEC104DataTypeHelper.IsWritable(DataType);

        #endregion

        #region CONSTRUCTOR

        public TagAdapter(DeviceAdapter deviceAdapter, string tagAddress, string tagType)
        {
            this.deviceAdapter = deviceAdapter ?? throw new ArgumentNullException(nameof(deviceAdapter));

            // Parse tag settings
            tagSettings = new TagSettings(tagAddress, tagType);

            lastReadTime = DateTime.MinValue;
            lastValue = "Bad";
            lastQuality = "Bad";
        }

        #endregion

        #region DATA OPERATIONS

        /// <summary>
        /// Read value from this tag
        /// </summary>
        public bool ReadValue(out string value, out string quality)
        {
            value = lastValue;
            quality = lastQuality;

            if (!IsReadable)
            {
                value = "Not Readable";
                quality = "Bad";
                return false;
            }

            if (!deviceAdapter.Connected)
            {
                value = "Disconnected";
                quality = "Bad";
                return false;
            }

            try
            {
                var clientAdapter = deviceAdapter.GetClientAdapter();
                if (clientAdapter != null)
                {
                    if (clientAdapter.ReadIOA(IOA, TypeId, out object rawValue, out var ieQuality))
                    {
                        // Convert to string value
                        if (rawValue != null)
                        {
                            value = rawValue.ToString();
                            quality = QualityMapper.ConvertToStatusString(ieQuality);

                            // Update cache
                            lastValue = value;
                            lastQuality = quality;
                            lastReadTime = DateTime.Now;

                            return true;
                        }
                    }
                }

                // Use cached value if recent
                if (DateTime.Now - lastReadTime < TimeSpan.FromMilliseconds(IEC104Constants.DefaultCacheLifetimeMs))
                {
                    value = lastValue;
                    quality = lastQuality;
                    return quality == "Good";
                }

                value = "No Data";
                quality = "Bad";
                return false;
            }
            catch (Exception ex)
            {
                value = "Error";
                quality = "Bad";
                return false;
            }
        }

        /// <summary>
        /// Write value to this tag
        /// </summary>
        public bool WriteValue(string value)
        {
            if (!IsWritable)
                return false;

            if (!deviceAdapter.Connected)
                return false;

            if (string.IsNullOrEmpty(value))
                return false;

            try
            {
                // Convert string value to appropriate type
                var convertedValue = ConvertValueForWrite(value);

                var clientAdapter = deviceAdapter.GetClientAdapter();
                if (clientAdapter != null)
                {
                    return clientAdapter.WriteIOA(IOA, TypeId, convertedValue);
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion

        #region VALUE CONVERSION

        private object ConvertValueForWrite(string value)
        {
            switch (DataType)
            {
                case IEC104DataType.SingleCommand:
                case IEC104DataType.SingleCommandWithTime:
                    return ValueMapper.IsValidBooleanValue(value) ?
                           Convert.ToBoolean(value.Trim()) : false;

                case IEC104DataType.DoubleCommand:
                case IEC104DataType.DoubleCommandWithTime:
                    return ValueMapper.IsValidBooleanValue(value) ?
                           Convert.ToBoolean(value.Trim()) : false;

                case IEC104DataType.SetpointCommandFloat:
                case IEC104DataType.SetpointCommandFloatWithTime:
                    return float.TryParse(value, out float floatVal) ? floatVal : 0.0f;

                case IEC104DataType.SetpointCommandScaled:
                case IEC104DataType.SetpointCommandScaledWithTime:
                    return short.TryParse(value, out short shortVal) ? shortVal : (short)0;

                case IEC104DataType.SetpointCommandNormalized:
                case IEC104DataType.SetpointCommandNormalizedWithTime:
                    if (float.TryParse(value, out float normVal))
                        return Math.Max(-1.0f, Math.Min(1.0f, normVal)); // Clamp to range
                    return 0.0f;

                case IEC104DataType.Bitstring32Command:
                case IEC104DataType.Bitstring32CommandWithTime:
                    // Parse hex or decimal
                    if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        return uint.TryParse(value.Substring(2), System.Globalization.NumberStyles.HexNumber,
                                           null, out uint hexVal) ? hexVal : 0u;
                    }
                    return uint.TryP// =====================================================
// File: Adapter/IEC104ClientAdapter.cs
// 🔥 CORE CLASS - Protocol + Connection management
// =====================================================
                    using System;
                    using System.Collections.Generic;
                    using System.Threading;
                    using IEC104.Protocol.SAP;
                    using IEC104.Protocol.Object;
                    using IEC104.Protocol.Enum;
                    using IEC104.Protocol.IE;
                    using IEC104.Protocol.IE.Base;
                    using IEC104.Common;
                    using IEC104.Constants;
                    using IEC104.Configuration;
                    using IEC104.Mapping;
                    using IEC104.Exceptions;

namespace IEC104.Adapter
    {
        /// <summary>
        /// Main IEC104 Client Adapter - handles protocol + connection management
        /// Combines ClientAdapter functionality with IEC104 protocol
        /// </summary>
        public class IEC104ClientAdapter : IDisposable
        {
            #region FIELDS

            private ClientSAP clientSAP;
            private DeviceSettings deviceSettings;
            private readonly Dictionary<int, CachedValue> dataCache;
            private readonly object lockObject = new object();
            private DateTime lastConnected;
            private bool disposed = false;

            // Connection state
            private bool connected = false;
            private bool connecting = false;

            // Event handling
            private Timer reconnectTimer;
            private Timer interrogationTimer;

            #endregion

            #region PROPERTIES

            /// <summary>
            /// Connection name for identification
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Connection lifetime in seconds
            /// </summary>
            public uint Lifetime { get; set; } = 3600;

            /// <summary>
            /// Current connection status
            /// </summary>
            public bool Connected => connected && clientSAP != null;

            /// <summary>
            /// Device configuration
            /// </summary>
            public DeviceSettings Settings => deviceSettings;

            #endregion

            #region EVENTS

            public event EventHandler<ConnectionEventArgs> ConnectionStateChanged;
            public event EventHandler<DataChangedEventArgs> DataReceived;
            public event EventHandler<ErrorEventArgs> ErrorOccurred;

            #endregion

            #region CONSTRUCTOR

            public IEC104ClientAdapter(string name, DeviceSettings settings)
            {
                Name = name;
                deviceSettings = settings ?? throw new ArgumentNullException(nameof(settings));
                dataCache = new Dictionary<int, CachedValue>();
                lastConnected = DateTime.MinValue;
            }

            #endregion

            #region CONNECTION MANAGEMENT

            /// <summary>
            /// Connect to IEC104 server
            /// </summary>
            public bool Connect()
            {
                if (Connected)
                    return true;

                if (connecting)
                    return false;

                return TryConnect(IEC104Constants.MaxConnectionRetries);
            }

            /// <summary>
            /// Try connecting with retry logic
            /// </summary>
            public bool TryConnect(int maxRetries = IEC104Constants.MaxConnectionRetries)
            {
                lock (lockObject)
                {
                    if (Connected)
                        return true;

                    if (connecting)
                        return false;

                    connecting = true;
                }

                try
                {
                    for (int attempt = 1; attempt <= maxRetries; attempt++)
                    {
                        try
                        {
                            OnConnectionStateChanged(false, $"Connecting to {deviceSettings.GetConnectionString()} (attempt {attempt}/{maxRetries})");

                            // Create ClientSAP
                            clientSAP = new ClientSAP(deviceSettings.IPAddress, deviceSettings.Port);

                            // Configure protocol parameters
                            ConfigureProtocolParameters();

                            // Set event handlers
                            clientSAP.NewASdu += OnNewASdu;
                            clientSAP.ConnectionClosed += OnConnectionClosed;

                            // Connect
                            clientSAP.Connect();

                            // Mark as connected
                            connected = true;
                            lastConnected = DateTime.Now;

                            OnConnectionStateChanged(true, $"Connected to {deviceSettings.GetConnectionString()}");

                            // Start timers
                            StartTimers();

                            // Send initial interrogation
                            if (deviceSettings.AutoInterrogation)
                            {
                                Thread.Sleep(1000); // Wait for connection to stabilize
                                SendGeneralInterrogation();
                            }

                            return true;
                        }
                        catch (Exception ex)
                        {
                            OnErrorOccurred(IEC104ErrorCode.ConnectionFailed, $"Connection attempt {attempt} failed: {ex.Message}");

                            if (attempt < maxRetries)
                            {
                                Thread.Sleep(IEC104Constants.RetryDelayMs);
                            }
                        }
                    }

                    return false;
                }
                finally
                {
                    connecting = false;
                }
            }

            /// <summary>
            /// Disconnect from server
            /// </summary>
            public bool Disconnect()
            {
                try
                {
                    StopTimers();

                    if (clientSAP != null)
                    {
                        // Unsubscribe events
                        clientSAP.NewASdu -= OnNewASdu;
                        clientSAP.ConnectionClosed -= OnConnectionClosed;

                        clientSAP = null;
                    }

                    connected = false;
                    OnConnectionStateChanged(false, "Disconnected");

                    return true;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(IEC104ErrorCode.ConnectionFailed, $"Disconnect error: {ex.Message}");
                    return false;
                }
            }

            /// <summary>
            /// Check connection lifetime and reconnect if needed
            /// </summary>
            public void CheckLifeTime()
            {
                if (Lifetime == 0) return; // No lifetime check

                var elapsed = DateTime.Now - lastConnected;
                if (elapsed.TotalSeconds > Lifetime)
                {
                    OnErrorOccurred(IEC104ErrorCode.ConnectionTimeout, "Connection lifetime exceeded, reconnecting...");

                    if (deviceSettings.AutoReconnect)
                    {
                        Disconnect();
                        Connect();
                    }
                }
            }

            #endregion

            #region PROTOCOL CONFIGURATION

            private void ConfigureProtocolParameters()
            {
                if (clientSAP != null)
                {
                    // Configure timeouts and protocol parameters
                    // Note: Actual ClientSAP API might be different
                    // clientSAP.SetTimeout(deviceSettings.Timeout);
                    // clientSAP.SetProtocolParameters(deviceSettings.K, deviceSettings.W);
                }
            }

            #endregion

            #region DATA OPERATIONS

            /// <summary>
            /// Read value from IOA
            /// </summary>
            public bool ReadIOA(int ioa, TypeId typeId, out object value, out IeQuality quality)
            {
                value = null;
                quality = null;

                if (!Connected)
                    return false;

                lock (lockObject)
                {
                    if (dataCache.TryGetValue(ioa, out CachedValue cachedValue))
                    {
                        // Check cache validity
                        if (DateTime.Now - cachedValue.Timestamp < TimeSpan.FromMilliseconds(IEC104Constants.DefaultCacheLifetimeMs))
                        {
                            value = cachedValue.Value;
                            quality = cachedValue.Quality;
                            return true;
                        }
                    }
                }

                // Cache miss or expired - value should come from spontaneous data or interrogation
                return false;
            }

            /// <summary>
            /// Write command to IOA
            /// </summary>
            public bool WriteIOA(int ioa, TypeId typeId, object value)
            {
                if (!Connected)
                    return false;

                return TryWrite(ioa, typeId, value, IEC104Constants.MaxWriteRetries);
            }

            /// <summary>
            /// Try writing with retry logic
            /// </summary>
            public bool TryWrite(int ioa, TypeId typeId, object value, int maxRetries = IEC104Constants.MaxWriteRetries)
            {
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        // Create appropriate ASDU for command
                        var asdu = CreateCommandASdu(ioa, typeId, value);
                        if (asdu != null)
                        {
                            clientSAP.SendASdu(asdu);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        OnErrorOccurred(IEC104ErrorCode.CommandFailed, $"Write attempt {attempt} failed: {ex.Message}");

                        if (attempt < maxRetries)
                        {
                            Thread.Sleep(IEC104Constants.RetryDelayMs);
                        }
                    }
                }

                return false;
            }

            #endregion

            #region INTERROGATION

            /// <summary>
            /// Send General Interrogation
            /// </summary>
            public void SendGeneralInterrogation()
            {
                if (!Connected) return;

                try
                {
                    var asdu = new ASdu(TypeId.C_IC_NA_1, false, CauseOfTransmission.ACTIVATION,
                                      false, false, 0, deviceSettings.CommonAddress,
                                      new InformationObject[] {
                                      new InformationObject(IEC104Constants.GeneralInterrogationIOA,
                                          new InformationElement[] {
                                              new IeQualifierOfInterrogation(IEC104Constants.GeneralInterrogationQualifier)
                                          })
                                      });

                    clientSAP.SendASdu(asdu);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(IEC104ErrorCode.ProtocolError, $"General Interrogation failed: {ex.Message}");
                }
            }

            /// <summary>
            /// Send Counter Interrogation
            /// </summary>
            public void SendCounterInterrogation()
            {
                if (!Connected) return;

                try
                {
                    var asdu = new ASdu(TypeId.C_CI_NA_1, false, CauseOfTransmission.ACTIVATION,
                                      false, false, 0, deviceSettings.CommonAddress,
                                      new InformationObject[] {
                                      new InformationObject(IEC104Constants.CounterInterrogationIOA,
                                          new InformationElement[] {
                                              new IeQualifierOfCounterInterrogation(IEC104Constants.CounterInterrogationQualifier)
                                          })
                                      });

                    clientSAP.SendASdu(asdu);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(IEC104ErrorCode.ProtocolError, $"Counter Interrogation failed: {ex.Message}");
                }
            }

            #endregion

            #region COMMAND CREATION

            private ASdu CreateCommandASdu(int ioa, TypeId typeId, object value)
            {
                try
                {
                    InformationElement[] elements = null;

                    switch (typeId)
                    {
                        case TypeId.C_SC_NA_1: // Single Command
                            var singleCmd = new IeSingleCommand(Convert.ToBoolean(value));
                            elements = new InformationElement[] { singleCmd };
                            break;

                        case TypeId.C_DC_NA_1: // Double Command
                            var doubleCmd = new IeDoubleCommand(Convert.ToBoolean(value));
                            elements = new InformationElement[] { doubleCmd };
                            break;

                        case TypeId.C_SE_NC_1: // Setpoint Float
                            var setpointFloat = new IeShortFloat(Convert.ToSingle(value));
                            var qualifier = new IeQualifierOfSetPointCommand(0, false);
                            elements = new InformationElement[] { setpointFloat, qualifier };
                            break;

                        default:
                            throw new IEC104Exception(IEC104ErrorCode.InvalidTypeId, $"Unsupported command TypeId: {typeId}");
                    }

                    if (elements != null)
                    {
                        return new ASdu(typeId, false, CauseOfTransmission.ACTIVATION,
                                      false, false, 0, deviceSettings.CommonAddress,
                                      new InformationObject[] {
                                      new InformationObject(ioa, elements)
                                      });
                    }
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(IEC104ErrorCode.DataConversionError, $"Command creation failed: {ex.Message}");
                }

                return null;
            }

            #endregion

            #region EVENT HANDLERS

            private void OnNewASdu(ASdu asdu)
            {
                try
                {
                    ProcessReceivedASdu(asdu);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(IEC104ErrorCode.ProtocolError, $"ASDU processing error: {ex.Message}");
                }
            }

            private void OnConnectionClosed(Exception exception)
            {
                connected = false;
                OnConnectionStateChanged(false, $"Connection closed: {exception?.Message}");

                // Auto reconnect if enabled
                if (deviceSettings.AutoReconnect && !disposed)
                {
                    StartReconnectTimer();
                }
            }

            #endregion

            #region ASDU PROCESSING

            private void ProcessReceivedASdu(ASdu asdu)
            {
                if (asdu == null) return;

                var typeId = asdu.GetTypeId();
                var informationObjects = asdu.GetInformationObjects();

                foreach (var io in informationObjects)
                {
                    var ioa = io.GetInformationObjectAddress();
                    var elements = io.GetInformationElements();

                    foreach (var element in elements)
                    {
                        ProcessInformationElement(ioa, typeId, element);
                    }
                }
            }

            private void ProcessInformationElement(int ioa, TypeId typeId, InformationElement element)
            {
                try
                {
                    // Extract value and quality
                    var dataType = TypeIdMapper.GetDataType(typeId);
                    var stringValue = ValueMapper.ConvertToString(element, dataType);
                    var quality = QualityMapper.ConvertToStatusString(element);

                    // Update cache
                    lock (lockObject)
                    {
                        dataCache[ioa] = new CachedValue
                        {
                            Value = stringValue,
                            Quality = element is IeQuality q ? q : IeQuality.CreateGood(),
                            Timestamp = DateTime.Now,
                            TypeId = typeId
                        };
                    }

                    // Raise data received event
                    OnDataReceived(ioa, stringValue, quality, typeId);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(IEC104ErrorCode.DataConversionError, $"IE processing failed for IOA {ioa}: {ex.Message}");
                }
            }

            #endregion

            #region TIMERS

            private void StartTimers()
            {
                // Auto interrogation timer
                if (deviceSettings.AutoInterrogation && deviceSettings.InterrogationInterval > 0)
                {
                    interrogationTimer = new Timer(OnInterrogationTimer, null,
                                                 deviceSettings.InterrogationInterval,
                                                 deviceSettings.InterrogationInterval);
                }
            }

            private void StopTimers()
            {
                reconnectTimer?.Dispose();
                interrogationTimer?.Dispose();
                reconnectTimer = null;
                interrogationTimer = null;
            }

            private void StartReconnectTimer()
            {
                if (deviceSettings.ReconnectInterval > 0)
                {
                    reconnectTimer = new Timer(OnReconnectTimer, null,
                                             deviceSettings.ReconnectInterval,
                                             Timeout.Infinite);
                }
            }

            private void OnInterrogationTimer(object state)
            {
                if (Connected)
                {
                    SendGeneralInterrogation();
                }
            }

            private void OnReconnectTimer(object state)
            {
                if (!Connected && !connecting)
                {
                    Connect();
                }
            }

            #endregion

            #region EVENT RAISING

            private void OnConnectionStateChanged(bool isConnected, string message)
            {
                ConnectionStateChanged?.Invoke(this, new ConnectionEventArgs
                {
                    IsConnected = isConnected,
                    Message = message,
                    Timestamp = DateTime.Now
                });
            }

            private void OnDataReceived(int ioa, string value, string quality, TypeId typeId)
            {
                DataReceived?.Invoke(this, new DataChangedEventArgs
                {
                    IOA = ioa,
                    Value = value,
                    Quality = quality,
                    TypeId = typeId,
                    Timestamp = DateTime.Now
                });
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
                StopTimers();
                Disconnect();

                lock (lockObject)
                {
                    dataCache.Clear();
                }
            }

            #endregion

            #region NESTED CLASSES

            private class CachedValue
            {
                public object Value { get; set; }
                public IeQuality Quality { get; set; }
                public DateTime Timestamp { get; set; }
                public TypeId TypeId { get; set; }
            }

            #endregion
        }
    }
}