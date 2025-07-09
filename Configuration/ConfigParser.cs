using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using IEC104.Constants;
using IEC104.Exceptions;

namespace IEC104.Configuration
{
    /// <summary>
    /// Helper class for parsing configuration strings
    /// </summary>
    public static class ConfigParser
    {
        #region IP ADDRESS VALIDATION

        /// <summary>
        /// Validate IP address string
        /// </summary>
        public static bool IsValidIPAddress(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return false;

            return IPAddress.TryParse(ipAddress, out _);
        }

        /// <summary>
        /// Parse and validate IP address
        /// </summary>
        public static IPAddress ParseIPAddress(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out IPAddress result))
                throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "IPAddress", ipAddress);

            return result;
        }

        #endregion

        #region PORT VALIDATION

        /// <summary>
        /// Validate TCP port number
        /// </summary>
        public static bool IsValidPort(int port)
        {
            return port > 0 && port <= 65535;
        }

        /// <summary>
        /// Parse and validate port number
        /// </summary>
        public static int ParsePort(string portString)
        {
            if (!int.TryParse(portString, out int port) || !IsValidPort(port))
                throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "Port", portString);

            return port;
        }

        #endregion

        #region IOA VALIDATION

        /// <summary>
        /// Validate Information Object Address
        /// </summary>
        public static bool IsValidIOA(int ioa)
        {
            return ioa >= IEC104Constants.MinIOA && ioa <= IEC104Constants.MaxIOA;
        }

        /// <summary>
        /// Parse and validate IOA
        /// </summary>
        public static int ParseIOA(string ioaString)
        {
            if (!int.TryParse(ioaString, out int ioa) || !IsValidIOA(ioa))
                throw new ConfigurationException(IEC104ErrorCode.InvalidIOA, "IOA", ioaString);

            return ioa;
        }

        #endregion

        #region COMMON ADDRESS VALIDATION

        /// <summary>
        /// Validate Common Address
        /// </summary>
        public static bool IsValidCommonAddress(int commonAddress)
        {
            return commonAddress >= 0 && commonAddress <= 65535;
        }

        /// <summary>
        /// Parse and validate Common Address
        /// </summary>
        public static int ParseCommonAddress(string commonAddressString)
        {
            if (!int.TryParse(commonAddressString, out int commonAddress) || !IsValidCommonAddress(commonAddress))
                throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "CommonAddress", commonAddressString);

            return commonAddress;
        }

        #endregion

        #region TIMEOUT VALIDATION

        /// <summary>
        /// Validate timeout value
        /// </summary>
        public static bool IsValidTimeout(int timeout)
        {
            return timeout > 0 && timeout <= int.MaxValue;
        }

        /// <summary>
        /// Parse and validate timeout
        /// </summary>
        public static int ParseTimeout(string timeoutString)
        {
            if (!int.TryParse(timeoutString, out int timeout) || !IsValidTimeout(timeout))
                throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "Timeout", timeoutString);

            return timeout;
        }

        #endregion

        #region PROTOCOL PARAMETER VALIDATION

        /// <summary>
        /// Validate K parameter
        /// </summary>
        public static bool IsValidK(int k)
        {
            return k > 0 && k <= 32767;
        }

        /// <summary>
        /// Validate W parameter
        /// </summary>
        public static bool IsValidW(int w, int k)
        {
            return w > 0 && w <= k;
        }

        /// <summary>
        /// Parse and validate K parameter
        /// </summary>
        public static int ParseK(string kString)
        {
            if (!int.TryParse(kString, out int k) || !IsValidK(k))
                throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "K", kString);

            return k;
        }

        /// <summary>
        /// Parse and validate W parameter
        /// </summary>
        public static int ParseW(string wString, int k)
        {
            if (!int.TryParse(wString, out int w) || !IsValidW(w, k))
                throw new ConfigurationException(IEC104ErrorCode.InvalidDeviceID, "W", wString);

            return w;
        }

        #endregion

        #region STRING UTILITIES

        /// <summary>
        /// Split string and trim parts
        /// </summary>
        public static string[] SplitAndTrim(string input, char separator)
        {
            if (string.IsNullOrEmpty(input))
                return new string[0];

            return input.Split(separator).Select(s => s.Trim()).ToArray();
        }

        /// <summary>
        /// Parse key-value pairs from string
        /// Format: "key1=value1;key2=value2"
        /// </summary>
        public static Dictionary<string, string> ParseKeyValuePairs(string input, char pairSeparator = ';', char keyValueSeparator = '=')
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(input))
                return result;

            var pairs = SplitAndTrim(input, pairSeparator);
            foreach (var pair in pairs)
            {
                if (string.IsNullOrEmpty(pair))
                    continue;

                var keyValue = SplitAndTrim(pair, keyValueSeparator);
                if (keyValue.Length == 2 && !string.IsNullOrEmpty(keyValue[0]))
                {
                    result[keyValue[0]] = keyValue[1];
                }
            }

            return result;
        }

        /// <summary>
        /// Safe parse integer with default value
        /// </summary>
        public static int ParseIntSafe(string input, int defaultValue)
        {
            if (int.TryParse(input, out int result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// Safe parse boolean with default value
        /// </summary>
        public static bool ParseBoolSafe(string input, bool defaultValue)
        {
            if (bool.TryParse(input, out bool result))
                return result;
            return defaultValue;
        }

        /// <summary>
        /// Safe parse double with default value
        /// </summary>
        public static double ParseDoubleSafe(string input, double defaultValue)
        {
            if (double.TryParse(input, out double result))
                return result;
            return defaultValue;
        }

        #endregion

        #region VALIDATION HELPERS

        /// <summary>
        /// Validate DeviceID format
        /// </summary>
        public static bool IsValidDeviceIDFormat(string deviceID)
        {
            if (string.IsNullOrEmpty(deviceID))
                return false;

            var parts = deviceID.Split('|');
            return parts.Length >= 4; // Minimum: IP|Port|CommonAddress|Timeout
        }

        /// <summary>
        /// Validate TagAddress format
        /// </summary>
        public static bool IsValidTagAddressFormat(string tagAddress)
        {
            if (string.IsNullOrEmpty(tagAddress))
                return false;

            var parts = tagAddress.Split(':');
            return parts.Length >= 1 && int.TryParse(parts[0], out _);
        }

        /// <summary>
        /// Get configuration summary for logging
        /// </summary>
        public static string GetConfigurationSummary(DeviceSettings deviceSettings)
        {
            if (deviceSettings == null)
                return "No configuration";

            return $"IEC104 Server: {deviceSettings.IPAddress}:{deviceSettings.Port}, " +
                   $"CA: {deviceSettings.CommonAddress}, " +
                   $"Timeout: {deviceSettings.Timeout}ms, " +
                   $"K: {deviceSettings.K}, W: {deviceSettings.W}";
        }

        #endregion
    }
}