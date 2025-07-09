using System;
using IEC104.Constants;

namespace IEC104.Exceptions
{
    /// <summary>
    /// Base exception class for IEC104 operations
    /// </summary>
    public class IEC104Exception : Exception
    {
        public IEC104ErrorCode ErrorCode { get; private set; }

        public IEC104Exception(IEC104ErrorCode errorCode)
            : base(ErrorCodeHelper.GetErrorMessage(errorCode))
        {
            ErrorCode = errorCode;
        }

        public IEC104Exception(IEC104ErrorCode errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public IEC104Exception(IEC104ErrorCode errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public override string ToString()
        {
            return $"IEC104Exception [Code: {ErrorCode}]: {Message}";
        }
    }

    /// <summary>
    /// Exception for connection-related errors
    /// </summary>
    public class ConnectionException : IEC104Exception
    {
        public string IPAddress { get; private set; }
        public int Port { get; private set; }

        public ConnectionException(IEC104ErrorCode errorCode, string ipAddress, int port)
            : base(errorCode, $"Connection error to {ipAddress}:{port} - {ErrorCodeHelper.GetErrorMessage(errorCode)}")
        {
            IPAddress = ipAddress;
            Port = port;
        }

        public ConnectionException(IEC104ErrorCode errorCode, string ipAddress, int port, Exception innerException)
            : base(errorCode, $"Connection error to {ipAddress}:{port} - {ErrorCodeHelper.GetErrorMessage(errorCode)}", innerException)
        {
            IPAddress = ipAddress;
            Port = port;
        }
    }

    /// <summary>
    /// Exception for protocol-related errors
    /// </summary>
    public class ProtocolException : IEC104Exception
    {
        public ProtocolException(IEC104ErrorCode errorCode) : base(errorCode)
        {
        }

        public ProtocolException(IEC104ErrorCode errorCode, string message) : base(errorCode, message)
        {
        }

        public ProtocolException(IEC104ErrorCode errorCode, string message, Exception innerException)
            : base(errorCode, message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception for configuration-related errors
    /// </summary>
    public class ConfigurationException : IEC104Exception
    {
        public string ParameterName { get; private set; }
        public string ParameterValue { get; private set; }

        public ConfigurationException(IEC104ErrorCode errorCode, string parameterName, string parameterValue)
            : base(errorCode, $"Configuration error for parameter '{parameterName}' with value '{parameterValue}' - {ErrorCodeHelper.GetErrorMessage(errorCode)}")
        {
            ParameterName = parameterName;
            ParameterValue = parameterValue;
        }
    }
}