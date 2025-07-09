using System;
using System.Linq;
using IEC104.Constants;
using IEC104.Exceptions;

namespace IEC104.Configuration
{
    /// <summary>
    /// Device configuration settings parsed from DeviceID string
    /// Format: "IP|Port|CommonAddress|Timeout|K|W|Options"
    /// Example: "192.168.1.100|2404|1|30000|12|8|"
    /// </summary>
    public class DeviceSettings
    {
        #region PROPERTIES

        /// <summary>
        /// IP Address of IEC104 server
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// TCP Port (default 2404)
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Common Address of ASDU
        /// </summary>
        public int CommonAddress { get; set; }

        /// <summary>
        /// Connection timeout in milliseconds
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Maximum number of outstanding I-format APDUs
        /// </summary>
        public int K { get; set; }

        /// <summary>
        /// Latest acknowledge after receiving W I-format APDUs
        /// </summary>
        public int W { get; set; }

        /// <summary>
        /// Timeout of connection establishment (seconds)
        /// </summary>
        public int T0 { get; set; }

        /// <summary>
        /// Timeout of send or test APDUs (seconds)
        /// </summary>
        public int T1 { get; set; }

        /// <summary>
        /// Timeout for acknowledges in case of no data messages (seconds)
        /// </summary>
        public int T2 { get; set; }

        /// <summary>
        /// Timeout for sending test frames in case of long idle state (seconds)
        /// </summary>
        public int T3 { get; set; }

        /// <summary>
        /// Auto reconnect on connection lost
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// Reconnection interval in milliseconds
        /// </summary>
        public int ReconnectInterval { get; set; }

        /// <summary>
        /// Enable automatic general interrogation
        /// </summary>
        public bool AutoInterrogation { get; set; }

        /// <summary>
        /// General interrogation interval in milliseconds
        /// </summary>
        public int InterrogationInterval { get; set; }

        /// <summary>
        /// Original DeviceID string
        /// </summary>
        public string DeviceID { get; set; }

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Default constructor with default values
        /// </summary>
        public DeviceSettings()
        {
            SetDefaults();
        }

        /// <summary>
        /// Constructor with DeviceID parsing
        /// </summary>
        public DeviceSettings(string deviceID)
        {
            SetDefaults();
            DeviceID = deviceID;
            Parse(deviceID);
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Set default values
        /// </summary>
        private void SetDefaults()
        {
            IPAddress = DefaultValues.DefaultIP;
            Port = DefaultValues.DefaultPort;
            CommonAddress = DefaultValues.DefaultCommonAddress;
            Timeout = DefaultValues.DefaultTimeout;
            K = DefaultValues.DefaultK;
            W = DefaultValues.DefaultW;
            T0 = DefaultValues.DefaultT0;
            T1 = DefaultValues.DefaultT1;
            T2 = DefaultValues.DefaultT2;
            T3 = DefaultValues.DefaultT3;
            AutoReconnect = DefaultValues.DefaultAutoReconnect;
            ReconnectInterval = DefaultValues.DefaultReconnectInterval;
            AutoInterrogation = DefaultValues.DefaultAutoInterrogation;
            InterrogationInterval = DefaultValues.DefaultInterrogationInterval;
        }

        /// <summary>
        /// Parse DeviceID string
        /// Format: "IP|Port|CommonAddress|Timeout|K|W|Options"
        /// Minimum: "IP|Port|CommonAddress|Timeout"
        /// </summary>
        public void Parse(string deviceID)
        {
            if (string.IsNullOrEmpty(deviceID))
                throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "DeviceID", deviceID);

            try
            {
                var parts = deviceID.Split('|');

                if (parts.Length < 4)
                    throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "DeviceID",
                        $"Minimum format: IP|Port|CommonAddress|Timeout. Got: {deviceID}");

                // Required parameters
                IPAddress = parts[0].Trim();
                if (string.IsNullOrEmpty(IPAddress))
                    throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "IPAddress", parts[0]);

                if (!int.TryParse(parts[1], out int port) || port <= 0 || port > 65535)
                    throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "Port", parts[1]);
                Port = port;

                if (!int.TryParse(parts[2], out int commonAddr) || commonAddr < 0 || commonAddr > 65535)
                    throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "CommonAddress", parts[2]);
                CommonAddress = commonAddr;

                if (!int.TryParse(parts[3], out int timeout) || timeout <= 0)
                    throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "Timeout", parts[3]);
                Timeout = timeout;

                // Optional parameters
                if (parts.Length > 4 && !string.IsNullOrEmpty(parts[4]))
                {
                    if (int.TryParse(parts[4], out int k) && k > 0)
                        K = k;
                }

                if (parts.Length > 5 && !string.IsNullOrEmpty(parts[5]))
                {
                    if (int.TryParse(parts[5], out int w) && w > 0)
                        W = w;
                }

                // Extended options (parts[6] could contain additional settings)
                if (parts.Length > 6 && !string.IsNullOrEmpty(parts[6]))
                {
                    ParseExtendedOptions(parts[6]);
                }
            }
            catch (ConfigurationException)
            {
                throw; // Re-throw configuration exceptions
            }
            catch (Exception ex)
            {
                throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "DeviceID", deviceID);
            }
        }

        /// <summary>
        /// Parse extended options from options string
        /// Format: "option1=value1;option2=value2"
        /// </summary>
        private void ParseExtendedOptions(string optionsString)
        {
            if (string.IsNullOrEmpty(optionsString))
                return;

            try
            {
                var options = optionsString.Split(';');
                foreach (var option in options)
                {
                    if (string.IsNullOrEmpty(option))
                        continue;

                    var keyValue = option.Split('=');
                    if (keyValue.Length != 2)
                        continue;

                    var key = keyValue[0].Trim().ToLower();
                    var value = keyValue[1].Trim();

                    switch (key)
                    {
                        case "t0":
                            if (int.TryParse(value, out int t0))
                                T0 = t0;
                            break;
                        case "t1":
                            if (int.TryParse(value, out int t1))
                                T1 = t1;
                            break;
                        case "t2":
                            if (int.TryParse(value, out int t2))
                                T2 = t2;
                            break;
                        case "t3":
                            if (int.TryParse(value, out int t3))
                                T3 = t3;
                            break;
                        case "autoreconnect":
                            if (bool.TryParse(value, out bool autoReconnect))
                                AutoReconnect = autoReconnect;
                            break;
                        case "reconnectinterval":
                            if (int.TryParse(value, out int reconnectInterval))
                                ReconnectInterval = reconnectInterval;
                            break;
                        case "autointerrogation":
                            if (bool.TryParse(value, out bool autoInterrogation))
                                AutoInterrogation = autoInterrogation;
                            break;
                        case "interrogationinterval":
                            if (int.TryParse(value, out int interrogationInterval))
                                InterrogationInterval = interrogationInterval;
                            break;
                    }
                }
            }
            catch
            {
                // Ignore parsing errors for extended options
            }
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrEmpty(IPAddress))
            {
                errorMessage = "IP Address is required";
                return false;
            }

            if (Port <= 0 || Port > 65535)
            {
                errorMessage = "Port must be between 1 and 65535";
                return false;
            }

            if (CommonAddress < 0 || CommonAddress > 65535)
            {
                errorMessage = "Common Address must be between 0 and 65535";
                return false;
            }

            if (Timeout <= 0)
            {
                errorMessage = "Timeout must be greater than 0";
                return false;
            }

            if (K <= 0 || K > 32767)
            {
                errorMessage = "K parameter must be between 1 and 32767";
                return false;
            }

            if (W <= 0 || W > K)
            {
                errorMessage = "W parameter must be between 1 and K";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get connection string for display
        /// </summary>
        public string GetConnectionString()
        {
            return $"{IPAddress}:{Port} (CA={CommonAddress})";
        }

        /// <summary>
        /// Generate DeviceID string from current settings
        /// </summary>
        public string ToDeviceIDString()
        {
            return $"{IPAddress}|{Port}|{CommonAddress}|{Timeout}|{K}|{W}|";
        }

        #endregion

        #region STATIC METHODS

        /// <summary>
        /// Create DeviceSettings from DeviceID string
        /// </summary>
        public static DeviceSettings Initialize(string deviceID)
        {
            return new DeviceSettings(deviceID);
        }

        /// <summary>
        /// Create default DeviceSettings
        /// </summary>
        public static DeviceSettings CreateDefault()
        {
            return new DeviceSettings();
        }

        /// <summary>
        /// Validate DeviceID format without creating instance
        /// </summary>
        public static bool ValidateDeviceID(string deviceID, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                var settings = new DeviceSettings(deviceID);
                return settings.IsValid(out errorMessage);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        #endregion

        #region OVERRIDES

        public override string ToString()
        {
            return GetConnectionString();
        }

        public override bool Equals(object obj)
        {
            if (obj is DeviceSettings other)
            {
                return IPAddress == other.IPAddress &&
                       Port == other.Port &&
                       CommonAddress == other.CommonAddress;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return $"{IPAddress}:{Port}:{CommonAddress}".GetHashCode();
        }

        #endregion
    }
}