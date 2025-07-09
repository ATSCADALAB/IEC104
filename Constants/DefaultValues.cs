namespace IEC104.Constants
{
    public static class DefaultValues
    {
        // === CONNECTION DEFAULTS ===
        public static readonly string DefaultIP = "127.0.0.1";
        public static readonly int DefaultPort = IEC104Constants.DefaultPort;
        public static readonly int DefaultCommonAddress = IEC104Constants.DefaultCommonAddress;
        public static readonly int DefaultTimeout = IEC104Constants.DefaultTimeout;

        // === PROTOCOL DEFAULTS ===
        public static readonly int DefaultK = IEC104Constants.DefaultK;
        public static readonly int DefaultW = IEC104Constants.DefaultW;
        public static readonly int DefaultT0 = IEC104Constants.DefaultT0;
        public static readonly int DefaultT1 = IEC104Constants.DefaultT1;
        public static readonly int DefaultT2 = IEC104Constants.DefaultT2;
        public static readonly int DefaultT3 = IEC104Constants.DefaultT3;

        // === CHANNEL DEFAULTS ===
        public static readonly uint DefaultChannelLifetime = 3600; // 1 hour in seconds
        public static readonly int DefaultScanRate = 1000; // 1 second

        // === TAG DEFAULTS ===
        public static readonly int DefaultIOA = 1;
        public static readonly string DefaultDataType = "SinglePoint";
        public static readonly string DefaultAccessRight = "ReadWrite";

        // === DEVICE DEFAULTS ===
        public static readonly string DefaultDeviceName = "IEC104Device";
        public static readonly bool DefaultAutoReconnect = true;
        public static readonly int DefaultReconnectInterval = 5000; // 5 seconds

        // === CACHE DEFAULTS ===
        public static readonly int DefaultCacheLifetime = IEC104Constants.DefaultCacheLifetimeMs;
        public static readonly bool DefaultCacheEnabled = true;
        public static readonly int DefaultMaxCacheEntries = 1000;

        // === UI DEFAULTS ===
        public static readonly int DefaultControlWidth = 200;
        public static readonly int DefaultControlHeight = 25;
        public static readonly string DefaultFontName = "Microsoft Sans Serif";
        public static readonly float DefaultFontSize = 8.25f;

        // === INTERROGATION DEFAULTS ===
        public static readonly bool DefaultAutoInterrogation = true;
        public static readonly int DefaultInterrogationInterval = 300000; // 5 minutes
        public static readonly bool DefaultCounterInterrogation = false;

        // === COMMAND DEFAULTS ===
        public static readonly int DefaultCommandTimeout = 10000; // 10 seconds
        public static readonly bool DefaultCommandConfirmation = true;
        public static readonly byte DefaultCommandQualifier = IEC104Constants.DefaultCommandQualifier;

        /// <summary>
        /// Get default DeviceID string format
        /// </summary>
        public static string GetDefaultDeviceID()
        {
            return $"{DefaultIP}|{DefaultPort}|{DefaultCommonAddress}|{DefaultTimeout}";
        }

        /// <summary>
        /// Get default ChannelAddress string format  
        /// </summary>
        public static string GetDefaultChannelAddress()
        {
            return DefaultChannelLifetime.ToString();
        }
    }
}