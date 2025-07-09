namespace IEC104.Constants
{
    public static class IEC104Constants
    {
        // === CONNECTION CONSTANTS ===
        public const int DefaultPort = 2404;
        public const int DefaultTimeout = 30000; // 30 seconds
        public const int DefaultCommonAddress = 1;

        // === PROTOCOL PARAMETERS ===
        public const int DefaultK = 12;  // Maximum number of outstanding I-format APDUs
        public const int DefaultW = 8;   // Latest acknowledge after receiving W I-format APDUs
        public const int DefaultT0 = 30; // Timeout of connection establishment (seconds)
        public const int DefaultT1 = 15; // Timeout of send or test APDUs (seconds)
        public const int DefaultT2 = 10; // Timeout for acknowledges in case of no data messages (seconds)
        public const int DefaultT3 = 20; // Timeout for sending test frames in case of long idle state (seconds)

        // === RETRY CONSTANTS ===
        public const int MaxConnectionRetries = 3;
        public const int MaxReadRetries = 2;
        public const int MaxWriteRetries = 2;
        public const int RetryDelayMs = 1000;

        // === CACHE CONSTANTS ===
        public const int DefaultCacheLifetimeMs = 60000; // 1 minute
        public const int MaxCacheSize = 10000;
        public const int CacheCleanupIntervalMs = 300000; // 5 minutes

        // === IOA RANGES ===
        public const int MinIOA = 1;
        public const int MaxIOA = 16777215; // 2^24 - 1 (3 bytes)

        // === COMMAND CONSTANTS ===
        public const byte DefaultCommandQualifier = 0;
        public const bool DefaultCommandSelect = false;

        // === INTERROGATION CONSTANTS ===
        public const int GeneralInterrogationIOA = 0;
        public const int CounterInterrogationIOA = 0;
        public const byte GeneralInterrogationQualifier = 20; // Station interrogation
        public const byte CounterInterrogationQualifier = 5;  // Request counter group 1

        // === QUALITY CONSTANTS ===
        public const string QualityGood = "Good";
        public const string QualityBad = "Bad";
        public const string QualityUncertain = "Uncertain";

        // === RESULT CODES ===
        public const int ResultOK = 0;
        public const int ResultError = -1;
        public const int ResultTimeout = -2;
        public const int ResultConnectionFailed = -3;
        public const int ResultInvalidData = -4;
        public const int ResultProtocolError = -5;
    }
}