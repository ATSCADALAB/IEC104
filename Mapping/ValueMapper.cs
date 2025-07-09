// =====================================================
// File: Mapping/ValueMapper.cs
// Map giữa IEC104 values và ATDriver values
// =====================================================
using System;
using System.Globalization;
using IEC104.Common;
using IEC104.Constants;
using IEC104.Exceptions;
using IEC104.Protocol.IE;
using IEC104.Protocol.IE.Base;

namespace IEC104.Mapping
{
    /// <summary>
    /// Mapping between IEC104 Information Element values and ATDriver string values
    /// </summary>
    public static class ValueMapper
    {
        #region CONSTANTS

        private const string TrueValue = "1";
        private const string FalseValue = "0";
        private const string InvalidValue = "Invalid";
        private const string UndeterminedValue = "Undetermined";

        #endregion

        #region FROM IEC104 TO ATDRIVER

        /// <summary>
        /// Convert IEC104 Information Element to ATDriver string value
        /// </summary>
        public static string ConvertToString(InformationElement ie, IEC104DataType dataType)
        {
            if (ie == null)
                return InvalidValue;

            try
            {
                switch (dataType)
                {
                    case IEC104DataType.SinglePoint:
                    case IEC104DataType.SinglePointWithTime:
                        return ConvertSinglePoint(ie);

                    case IEC104DataType.DoublePoint:
                    case IEC104DataType.DoublePointWithTime:
                        return ConvertDoublePoint(ie);

                    case IEC104DataType.StepPosition:
                    case IEC104DataType.StepPositionWithTime:
                        return ConvertStepPosition(ie);

                    case IEC104DataType.Bitstring32:
                    case IEC104DataType.Bitstring32WithTime:
                        return ConvertBitstring32(ie);

                    case IEC104DataType.MeasuredValueNormalized:
                    case IEC104DataType.MeasuredValueNormalizedWithTime:
                        return ConvertNormalizedValue(ie);

                    case IEC104DataType.MeasuredValueScaled:
                    case IEC104DataType.MeasuredValueScaledWithTime:
                        return ConvertScaledValue(ie);

                    case IEC104DataType.MeasuredValueFloat:
                    case IEC104DataType.MeasuredValueFloatWithTime:
                        return ConvertFloatValue(ie);

                    case IEC104DataType.IntegratedTotals:
                    case IEC104DataType.IntegratedTotalsWithTime:
                        return ConvertIntegratedTotals(ie);

                    default:
                        return ie.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new IEC104Exception(IEC104ErrorCode.DataConversionError,
                    $"Failed to convert IE to string for {dataType}: {ex.Message}", ex);
            }
        }

        private static string ConvertSinglePoint(InformationElement ie)
        {
            if (ie is IeSinglePointWithQuality sp)
            {
                if (sp.IsQualityGood())
                    return sp.IsOn() ? TrueValue : FalseValue;
                else
                    return InvalidValue;
            }

            return InvalidValue;
        }

        private static string ConvertDoublePoint(InformationElement ie)
        {
            if (ie is IeDoublePointWithQuality dp)
            {
                if (dp.IsQualityGood())
                {
                    // 0 = Undetermined, 1 = Off, 2 = On, 3 = Undetermined
                    var value = dp.GetDoublePointState();
                    switch (value)
                    {
                        case 0:
                        case 3:
                            return UndeterminedValue;
                        case 1:
                            return FalseValue;
                        case 2:
                            return TrueValue;
                        default:
                            return InvalidValue;
                    }
                }
                else
                    return InvalidValue;
            }

            return InvalidValue;
        }

        private static string ConvertStepPosition(InformationElement ie)
        {
            // Step position is typically a signed value -64 to +63
            // Convert to string representation
            return ie.ToString();
        }

        private static string ConvertBitstring32(InformationElement ie)
        {
            // Convert 32-bit bitstring to hexadecimal string
            if (ie is IeBinaryStateInformation bits)
            {
                var value = bits.GetValue();
                return $"0x{value:X8}";
            }

            return InvalidValue;
        }

        private static string ConvertNormalizedValue(InformationElement ie)
        {
            if (ie is IeNormalizedValue nv)
            {
                if (nv.IsQualityGood())
                {
                    var value = nv.GetNormalizedValue();
                    return value.ToString("F6", CultureInfo.InvariantCulture);
                }
                else
                    return InvalidValue;
            }

            return InvalidValue;
        }

        private static string ConvertScaledValue(InformationElement ie)
        {
            if (ie is IeScaledValue sv)
            {
                if (sv.IsQualityGood())
                {
                    var value = sv.GetScaledValue();
                    return value.ToString(CultureInfo.InvariantCulture);
                }
                else
                    return InvalidValue;
            }

            return InvalidValue;
        }

        private static string ConvertFloatValue(InformationElement ie)
        {
            if (ie is IeShortFloat sf)
            {
                if (sf.IsQualityGood())
                {
                    var value = sf.GetValue();
                    return value.ToString("F6", CultureInfo.InvariantCulture);
                }
                else
                    return InvalidValue;
            }

            return InvalidValue;
        }

        private static string ConvertIntegratedTotals(InformationElement ie)
        {
            if (ie is IeBinaryCounterReading bcr)
            {
                if (bcr.IsQualityGood())
                {
                    var value = bcr.GetCounterReading();
                    return value.ToString(CultureInfo.InvariantCulture);
                }
                else
                    return InvalidValue;
            }

            return InvalidValue;
        }

        #endregion

        #region FROM ATDRIVER TO IEC104

        /// <summary>
        /// Convert ATDriver string value to appropriate IEC104 Information Element
        /// </summary>
        public static InformationElement ConvertFromString(string value, IEC104DataType dataType)
        {
            if (string.IsNullOrEmpty(value))
                throw new IEC104Exception(IEC104ErrorCode.InvalidDataType, "Value cannot be null or empty");

            try
            {
                switch (dataType)
                {
                    case IEC104DataType.SingleCommand:
                    case IEC104DataType.SingleCommandWithTime:
                        return CreateSingleCommand(value);

                    case IEC104DataType.DoubleCommand:
                    case IEC104DataType.DoubleCommandWithTime:
                        return CreateDoubleCommand(value);

                    case IEC104DataType.SetpointCommandNormalized:
                    case IEC104DataType.SetpointCommandNormalizedWithTime:
                        return CreateNormalizedSetpoint(value);

                    case IEC104DataType.SetpointCommandScaled:
                    case IEC104DataType.SetpointCommandScaledWithTime:
                        return CreateScaledSetpoint(value);

                    case IEC104DataType.SetpointCommandFloat:
                    case IEC104DataType.SetpointCommandFloatWithTime:
                        return CreateFloatSetpoint(value);

                    case IEC104DataType.Bitstring32Command:
                    case IEC104DataType.Bitstring32CommandWithTime:
                        return CreateBitstringCommand(value);

                    default:
                        throw new IEC104Exception(IEC104ErrorCode.InvalidDataType,
                            $"Data type {dataType} is not writable");
                }
            }
            catch (Exception ex)
            {
                throw new IEC104Exception(IEC104ErrorCode.DataConversionError,
                    $"Failed to convert string '{value}' to IE for {dataType}: {ex.Message}", ex);
            }
        }

        private static InformationElement CreateSingleCommand(string value)
        {
            var boolValue = ParseBooleanValue(value);
            return new IeSingleCommand(boolValue);
        }

        private static InformationElement CreateDoubleCommand(string value)
        {
            // Parse double command value: 0=Not permitted, 1=Off, 2=On, 3=Not permitted
            if (int.TryParse(value, out int intValue))
            {
                switch (intValue)
                {
                    case 0:
                    case 1:
                        return new IeDoubleCommand(false); // Off
                    case 2:
                        return new IeDoubleCommand(true);  // On
                    default:
                        throw new IEC104Exception(IEC104ErrorCode.ValueOutOfRange,
                            "Double command value must be 0 (off) or 2 (on)");
                }
            }

            // Try parsing as boolean
            var boolValue = ParseBooleanValue(value);
            return new IeDoubleCommand(boolValue);
        }

        private static InformationElement CreateNormalizedSetpoint(string value)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
                throw new IEC104Exception(IEC104ErrorCode.DataConversionError, $"Invalid normalized value: {value}");

            // Normalized values are typically in range -1.0 to +1.0
            if (doubleValue < -1.0 || doubleValue > 1.0)
                throw new IEC104Exception(IEC104ErrorCode.ValueOutOfRange,
                    "Normalized value must be between -1.0 and +1.0");

            return new IeNormalizedValue((float)doubleValue);
        }

        private static InformationElement CreateScaledSetpoint(string value)
        {
            if (!short.TryParse(value, out short shortValue))
                throw new IEC104Exception(IEC104ErrorCode.DataConversionError, $"Invalid scaled value: {value}");

            return new IeScaledValue(shortValue);
        }

        private static InformationElement CreateFloatSetpoint(string value)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                throw new IEC104Exception(IEC104ErrorCode.DataConversionError, $"Invalid float value: {value}");

            return new IeShortFloat(floatValue);
        }

        private static InformationElement CreateBitstringCommand(string value)
        {
            uint uintValue;

            // Try parsing as hexadecimal (0x prefix)
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (!uint.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uintValue))
                    throw new IEC104Exception(IEC104ErrorCode.DataConversionError, $"Invalid hex bitstring value: {value}");
            }
            // Try parsing as decimal
            else if (!uint.TryParse(value, out uintValue))
            {
                throw new IEC104Exception(IEC104ErrorCode.DataConversionError, $"Invalid bitstring value: {value}");
            }

            return new IeBinaryStateInformation(uintValue);
        }

        #endregion

        #region UTILITY METHODS

        /// <summary>
        /// Parse string value to boolean
        /// </summary>
        private static bool ParseBooleanValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Try parsing as boolean
            if (bool.TryParse(value, out bool boolResult))
                return boolResult;

            // Try parsing as integer
            if (int.TryParse(value, out int intResult))
                return intResult != 0;

            // Try parsing specific strings
            switch (value.ToLower().Trim())
            {
                case "true":
                case "on":
                case "yes":
                case "1":
                    return true;
                case "false":
                case "off":
                case "no":
                case "0":
                    return false;
                default:
                    throw new IEC104Exception(IEC104ErrorCode.DataConversionError,
                        $"Cannot convert '{value}' to boolean");
            }
        }

        /// <summary>
        /// Check if string value represents a valid boolean
        /// </summary>
        public static bool IsValidBooleanValue(string value)
        {
            try
            {
                ParseBooleanValue(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if string value is valid for data type
        /// </summary>
        public static bool IsValidValue(string value, IEC104DataType dataType)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            try
            {
                ConvertFromString(value, dataType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get default value for data type
        /// </summary>
        public static string GetDefaultValue(IEC104DataType dataType)
        {
            switch (dataType)
            {
                case IEC104DataType.SinglePoint:
                case IEC104DataType.SinglePointWithTime:
                case IEC104DataType.SingleCommand:
                case IEC104DataType.SingleCommandWithTime:
                    return FalseValue;

                case IEC104DataType.DoublePoint:
                case IEC104DataType.DoublePointWithTime:
                case IEC104DataType.DoubleCommand:
                case IEC104DataType.DoubleCommandWithTime:
                    return "0";

                case IEC104DataType.MeasuredValueNormalized:
                case IEC104DataType.MeasuredValueNormalizedWithTime:
                case IEC104DataType.SetpointCommandNormalized:
                case IEC104DataType.SetpointCommandNormalizedWithTime:
                    return "0.0";

                case IEC104DataType.MeasuredValueScaled:
                case IEC104DataType.MeasuredValueScaledWithTime:
                case IEC104DataType.SetpointCommandScaled:
                case IEC104DataType.SetpointCommandScaledWithTime:
                case IEC104DataType.StepPosition:
                case IEC104DataType.StepPositionWithTime:
                    return "0";

                case IEC104DataType.MeasuredValueFloat:
                case IEC104DataType.MeasuredValueFloatWithTime:
                case IEC104DataType.SetpointCommandFloat:
                case IEC104DataType.SetpointCommandFloatWithTime:
                    return "0.0";

                case IEC104DataType.IntegratedTotals:
                case IEC104DataType.IntegratedTotalsWithTime:
                    return "0";

                case IEC104DataType.Bitstring32:
                case IEC104DataType.Bitstring32WithTime:
                case IEC104DataType.Bitstring32Command:
                case IEC104DataType.Bitstring32CommandWithTime:
                    return "0x00000000";

                default:
                    return "0";
            }
        }

        #endregion
    }
}