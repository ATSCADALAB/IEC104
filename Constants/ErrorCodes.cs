namespace IEC104.Constants
{
    public enum IEC104ErrorCode
    {
        // === SUCCESS ===
        Success = 0,

        // === CONNECTION ERRORS ===
        ConnectionFailed = 1001,
        ConnectionTimeout = 1002,
        ConnectionLost = 1003,
        ConnectionRefused = 1004,
        SocketError = 1005,

        // === PROTOCOL ERRORS ===
        ProtocolError = 2001,
        InvalidAPDU = 2002,
        InvalidTypeId = 2003,
        InvalidCOT = 2004,
        InvalidIOA = 2005,
        SequenceError = 2006,

        // === DATA ERRORS ===
        InvalidDataType = 3001,
        DataConversionError = 3002,
        ValueOutOfRange = 3003,
        QualityInvalid = 3004,
        TimestampInvalid = 3005,

        // === COMMAND ERRORS ===
        CommandFailed = 4001,
        CommandTimeout = 4002,
        CommandNotSupported = 4003,
        CommandInvalidParameter = 4004,
        CommandPermissionDenied = 4005,

        // === CONFIGURATION ERRORS ===
        ConfigurationError = 5001,
        InvalidDeviceID = 5002,
        InvalidTagAddress = 5003,
        InvalidTagType = 5004,
        MissingConfiguration = 5005,

        // === CACHE ERRORS ===
        CacheError = 6001,
        CacheOverflow = 6002,
        CacheExpired = 6003,

        // === GENERAL ERRORS ===
        UnknownError = 9999,
        NotImplemented = 9998,
        NotSupported = 9997
    }

    public static class ErrorCodeHelper
    {
        public static string GetErrorMessage(IEC104ErrorCode errorCode)
        {
            switch (errorCode)
            {
                case IEC104ErrorCode.Success:
                    return "Operation completed successfully";

                // Connection errors
                case IEC104ErrorCode.ConnectionFailed:
                    return "Failed to establish connection to IEC104 server";
                case IEC104ErrorCode.ConnectionTimeout:
                    return "Connection timeout";
                case IEC104ErrorCode.ConnectionLost:
                    return "Connection lost unexpectedly";
                case IEC104ErrorCode.ConnectionRefused:
                    return "Connection refused by server";
                case IEC104ErrorCode.SocketError:
                    return "Network socket error";

                // Protocol errors
                case IEC104ErrorCode.ProtocolError:
                    return "IEC 60870-5-104 protocol error";
                case IEC104ErrorCode.InvalidTypeId:
                    return "Invalid or unsupported Type ID";
                case IEC104ErrorCode.InvalidIOA:
                    return "Invalid Information Object Address";

                // Data errors
                case IEC104ErrorCode.InvalidDataType:
                    return "Invalid data type specified";
                case IEC104ErrorCode.DataConversionError:
                    return "Failed to convert data value";
                case IEC104ErrorCode.QualityInvalid:
                    return "Data quality indicates invalid value";

                // Command errors
                case IEC104ErrorCode.CommandFailed:
                    return "Command execution failed";
                case IEC104ErrorCode.CommandTimeout:
                    return "Command execution timeout";
                case IEC104ErrorCode.CommandNotSupported:
                    return "Command not supported by device";

                // Configuration errors
                case IEC104ErrorCode.InvalidDeviceID:
                    return "Invalid device ID format";
                case IEC104ErrorCode.InvalidTagAddress:
                    return "Invalid tag address format";

                default:
                    return $"Unknown error: {errorCode}";
            }
        }

        public static bool IsConnectionError(IEC104ErrorCode errorCode)
        {
            return (int)errorCode >= 1001 && (int)errorCode <= 1999;
        }

        public static bool IsProtocolError(IEC104ErrorCode errorCode)
        {
            return (int)errorCode >= 2001 && (int)errorCode <= 2999;
        }

        public static bool IsDataError(IEC104ErrorCode errorCode)
        {
            return (int)errorCode >= 3001 && (int)errorCode <= 3999;
        }
    }
}