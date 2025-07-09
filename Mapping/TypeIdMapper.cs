using System;
using System.Collections.Generic;
using IEC104.Common;
using IEC104.Protocol.Enum;
using IEC104.Constants;
using IEC104.Exceptions;

namespace IEC104.Mapping
{
    /// <summary>
    /// Mapping between ATDriver TagType strings and IEC104 TypeId
    /// </summary>
    public static class TypeIdMapper
    {
        #region STATIC MAPPINGS

        /// <summary>
        /// TagType string to IEC104DataType mapping
        /// </summary>
        private static readonly Dictionary<string, IEC104DataType> TagTypeToDataType =
            new Dictionary<string, IEC104DataType>(StringComparer.OrdinalIgnoreCase)
        {
            // === PROCESS INFORMATION ===
            
            // Single Point
            {"SinglePoint", IEC104DataType.SinglePoint},
            {"SP", IEC104DataType.SinglePoint},
            {"Binary", IEC104DataType.SinglePoint},
            {"Bool", IEC104DataType.SinglePoint},
            {"M_SP_NA_1", IEC104DataType.SinglePoint},

            {"SinglePointWithTime", IEC104DataType.SinglePointWithTime},
            {"SPT", IEC104DataType.SinglePointWithTime},
            {"M_SP_TB_1", IEC104DataType.SinglePointWithTime},
            
            // Double Point
            {"DoublePoint", IEC104DataType.DoublePoint},
            {"DP", IEC104DataType.DoublePoint},
            {"M_DP_NA_1", IEC104DataType.DoublePoint},

            {"DoublePointWithTime", IEC104DataType.DoublePointWithTime},
            {"DPT", IEC104DataType.DoublePointWithTime},
            {"M_DP_TB_1", IEC104DataType.DoublePointWithTime},
            
            // Step Position
            {"StepPosition", IEC104DataType.StepPosition},
            {"Step", IEC104DataType.StepPosition},
            {"M_ST_NA_1", IEC104DataType.StepPosition},

            {"StepPositionWithTime", IEC104DataType.StepPositionWithTime},
            {"StepT", IEC104DataType.StepPositionWithTime},
            {"M_ST_TB_1", IEC104DataType.StepPositionWithTime},
            
            // Bitstring
            {"Bitstring32", IEC104DataType.Bitstring32},
            {"Bitstring", IEC104DataType.Bitstring32},
            {"Bits", IEC104DataType.Bitstring32},
            {"M_BO_NA_1", IEC104DataType.Bitstring32},

            {"Bitstring32WithTime", IEC104DataType.Bitstring32WithTime},
            {"BitstringT", IEC104DataType.Bitstring32WithTime},
            {"M_BO_TB_1", IEC104DataType.Bitstring32WithTime},
            
            // Measured Values
            {"MeasuredValueNormalized", IEC104DataType.MeasuredValueNormalized},
            {"MVN", IEC104DataType.MeasuredValueNormalized},
            {"Normalized", IEC104DataType.MeasuredValueNormalized},
            {"M_ME_NA_1", IEC104DataType.MeasuredValueNormalized},

            {"MeasuredValueScaled", IEC104DataType.MeasuredValueScaled},
            {"MVS", IEC104DataType.MeasuredValueScaled},
            {"Scaled", IEC104DataType.MeasuredValueScaled},
            {"M_ME_NB_1", IEC104DataType.MeasuredValueScaled},

            {"MeasuredValueFloat", IEC104DataType.MeasuredValueFloat},
            {"MVF", IEC104DataType.MeasuredValueFloat},
            {"Float", IEC104DataType.MeasuredValueFloat},
            {"Analog", IEC104DataType.MeasuredValueFloat},
            {"MeasuredValue", IEC104DataType.MeasuredValueFloat},
            {"M_ME_NC_1", IEC104DataType.MeasuredValueFloat},
            
            // With Time
            {"MeasuredValueNormalizedWithTime", IEC104DataType.MeasuredValueNormalizedWithTime},
            {"MVNT", IEC104DataType.MeasuredValueNormalizedWithTime},
            {"M_ME_TD_1", IEC104DataType.MeasuredValueNormalizedWithTime},

            {"MeasuredValueScaledWithTime", IEC104DataType.MeasuredValueScaledWithTime},
            {"MVST", IEC104DataType.MeasuredValueScaledWithTime},
            {"M_ME_TE_1", IEC104DataType.MeasuredValueScaledWithTime},

            {"MeasuredValueFloatWithTime", IEC104DataType.MeasuredValueFloatWithTime},
            {"MVFT", IEC104DataType.MeasuredValueFloatWithTime},
            {"FloatT", IEC104DataType.MeasuredValueFloatWithTime},
            {"M_ME_TF_1", IEC104DataType.MeasuredValueFloatWithTime},
            
            // Integrated Totals
            {"IntegratedTotals", IEC104DataType.IntegratedTotals},
            {"IT", IEC104DataType.IntegratedTotals},
            {"Counter", IEC104DataType.IntegratedTotals},
            {"M_IT_NA_1", IEC104DataType.IntegratedTotals},

            {"IntegratedTotalsWithTime", IEC104DataType.IntegratedTotalsWithTime},
            {"ITT", IEC104DataType.IntegratedTotalsWithTime},
            {"CounterT", IEC104DataType.IntegratedTotalsWithTime},
            {"M_IT_TB_1", IEC104DataType.IntegratedTotalsWithTime},
            
            // === CONTROL COMMANDS ===
            
            // Single Command
            {"SingleCommand", IEC104DataType.SingleCommand},
            {"SC", IEC104DataType.SingleCommand},
            {"Command", IEC104DataType.SingleCommand},
            {"BinaryCommand", IEC104DataType.SingleCommand},
            {"C_SC_NA_1", IEC104DataType.SingleCommand},

            {"SingleCommandWithTime", IEC104DataType.SingleCommandWithTime},
            {"SCT", IEC104DataType.SingleCommandWithTime},
            {"C_SC_TA_1", IEC104DataType.SingleCommandWithTime},
            
            // Double Command
            {"DoubleCommand", IEC104DataType.DoubleCommand},
            {"DC", IEC104DataType.DoubleCommand},
            {"C_DC_NA_1", IEC104DataType.DoubleCommand},

            {"DoubleCommandWithTime", IEC104DataType.DoubleCommandWithTime},
            {"DCT", IEC104DataType.DoubleCommandWithTime},
            {"C_DC_TA_1", IEC104DataType.DoubleCommandWithTime},
            
            // Regulating Step Command
            {"RegulatingStepCommand", IEC104DataType.RegulatingStepCommand},
            {"RSC", IEC104DataType.RegulatingStepCommand},
            {"StepCommand", IEC104DataType.RegulatingStepCommand},
            {"C_RC_NA_1", IEC104DataType.RegulatingStepCommand},

            {"RegulatingStepCommandWithTime", IEC104DataType.RegulatingStepCommandWithTime},
            {"RSCT", IEC104DataType.RegulatingStepCommandWithTime},
            {"C_RC_TA_1", IEC104DataType.RegulatingStepCommandWithTime},
            
            // Setpoint Commands
            {"SetpointCommandNormalized", IEC104DataType.SetpointCommandNormalized},
            {"SetpointNormalized", IEC104DataType.SetpointCommandNormalized},
            {"SCN", IEC104DataType.SetpointCommandNormalized},
            {"C_SE_NA_1", IEC104DataType.SetpointCommandNormalized},

            {"SetpointCommandScaled", IEC104DataType.SetpointCommandScaled},
            {"SetpointScaled", IEC104DataType.SetpointCommandScaled},
            {"SCS", IEC104DataType.SetpointCommandScaled},
            {"C_SE_NB_1", IEC104DataType.SetpointCommandScaled},

            {"SetpointCommandFloat", IEC104DataType.SetpointCommandFloat},
            {"SetpointFloat", IEC104DataType.SetpointCommandFloat},
            {"Setpoint", IEC104DataType.SetpointCommandFloat},
            {"SCF", IEC104DataType.SetpointCommandFloat},
            {"AnalogCommand", IEC104DataType.SetpointCommandFloat},
            {"C_SE_NC_1", IEC104DataType.SetpointCommandFloat},
            
            // With Time
            {"SetpointCommandNormalizedWithTime", IEC104DataType.SetpointCommandNormalizedWithTime},
            {"SCNT", IEC104DataType.SetpointCommandNormalizedWithTime},
            {"C_SE_TA_1", IEC104DataType.SetpointCommandNormalizedWithTime},

            {"SetpointCommandScaledWithTime", IEC104DataType.SetpointCommandScaledWithTime},
            {"SCST", IEC104DataType.SetpointCommandScaledWithTime},
            {"C_SE_TB_1", IEC104DataType.SetpointCommandScaledWithTime},

            {"SetpointCommandFloatWithTime", IEC104DataType.SetpointCommandFloatWithTime},
            {"SCFT", IEC104DataType.SetpointCommandFloatWithTime},
            {"SetpointT", IEC104DataType.SetpointCommandFloatWithTime},
            {"C_SE_TC_1", IEC104DataType.SetpointCommandFloatWithTime},
            
            // Bitstring Command
            {"Bitstring32Command", IEC104DataType.Bitstring32Command},
            {"BitstringCommand", IEC104DataType.Bitstring32Command},
            {"BitsCommand", IEC104DataType.Bitstring32Command},
            {"C_BO_NA_1", IEC104DataType.Bitstring32Command},

            {"Bitstring32CommandWithTime", IEC104DataType.Bitstring32CommandWithTime},
            {"BitstringCommandT", IEC104DataType.Bitstring32CommandWithTime},
            {"C_BO_TA_1", IEC104DataType.Bitstring32CommandWithTime},
            
            // === SYSTEM COMMANDS ===
            
            // Interrogation
            {"GeneralInterrogation", IEC104DataType.GeneralInterrogation},
            {"GI", IEC104DataType.GeneralInterrogation},
            {"Interrogation", IEC104DataType.GeneralInterrogation},
            {"C_IC_NA_1", IEC104DataType.GeneralInterrogation},

            {"CounterInterrogation", IEC104DataType.CounterInterrogation},
            {"CI", IEC104DataType.CounterInterrogation},
            {"C_CI_NA_1", IEC104DataType.CounterInterrogation},
            
            // Other system commands
            {"ReadCommand", IEC104DataType.ReadCommand},
            {"Read", IEC104DataType.ReadCommand},
            {"C_RD_NA_1", IEC104DataType.ReadCommand},

            {"ClockSynchronization", IEC104DataType.ClockSynchronization},
            {"ClockSync", IEC104DataType.ClockSynchronization},
            {"C_CS_NA_1", IEC104DataType.ClockSynchronization},

            {"TestCommand", IEC104DataType.TestCommand},
            {"Test", IEC104DataType.TestCommand},
            {"C_TS_NA_1", IEC104DataType.TestCommand},

            {"ResetProcess", IEC104DataType.ResetProcess},
            {"Reset", IEC104DataType.ResetProcess},
            {"C_RP_NA_1", IEC104DataType.ResetProcess},

            {"DelayAcquisition", IEC104DataType.DelayAcquisition},
            {"Delay", IEC104DataType.DelayAcquisition},
            {"C_CD_NA_1", IEC104DataType.DelayAcquisition},
            
            // End of Initialization
            {"EndOfInitialization", IEC104DataType.EndOfInitialization},
            {"EOI", IEC104DataType.EndOfInitialization},
            {"M_EI_NA_1", IEC104DataType.EndOfInitialization}
        };

        /// <summary>
        /// IEC104DataType to TypeId mapping
        /// </summary>
        private static readonly Dictionary<IEC104DataType, TypeId> DataTypeToTypeId =
            new Dictionary<IEC104DataType, TypeId>
        {
            {IEC104DataType.SinglePoint, TypeId.M_SP_NA_1},
            {IEC104DataType.SinglePointWithTime, TypeId.M_SP_TB_1},
            {IEC104DataType.DoublePoint, TypeId.M_DP_NA_1},
            {IEC104DataType.DoublePointWithTime, TypeId.M_DP_TB_1},
            {IEC104DataType.StepPosition, TypeId.M_ST_NA_1},
            {IEC104DataType.StepPositionWithTime, TypeId.M_ST_TB_1},
            {IEC104DataType.Bitstring32, TypeId.M_BO_NA_1},
            {IEC104DataType.Bitstring32WithTime, TypeId.M_BO_TB_1},
            {IEC104DataType.MeasuredValueNormalized, TypeId.M_ME_NA_1},
            {IEC104DataType.MeasuredValueScaled, TypeId.M_ME_NB_1},
            {IEC104DataType.MeasuredValueFloat, TypeId.M_ME_NC_1},
            {IEC104DataType.MeasuredValueNormalizedWithTime, TypeId.M_ME_TD_1},
            {IEC104DataType.MeasuredValueScaledWithTime, TypeId.M_ME_TE_1},
            {IEC104DataType.MeasuredValueFloatWithTime, TypeId.M_ME_TF_1},
            {IEC104DataType.IntegratedTotals, TypeId.M_IT_NA_1},
            {IEC104DataType.IntegratedTotalsWithTime, TypeId.M_IT_TB_1},
            {IEC104DataType.SingleCommand, TypeId.C_SC_NA_1},
            {IEC104DataType.SingleCommandWithTime, TypeId.C_SC_TA_1},
            {IEC104DataType.DoubleCommand, TypeId.C_DC_NA_1},
            {IEC104DataType.DoubleCommandWithTime, TypeId.C_DC_TA_1},
            {IEC104DataType.RegulatingStepCommand, TypeId.C_RC_NA_1},
            {IEC104DataType.RegulatingStepCommandWithTime, TypeId.C_RC_TA_1},
            {IEC104DataType.SetpointCommandNormalized, TypeId.C_SE_NA_1},
            {IEC104DataType.SetpointCommandScaled, TypeId.C_SE_NB_1},
            {IEC104DataType.SetpointCommandFloat, TypeId.C_SE_NC_1},
            {IEC104DataType.SetpointCommandNormalizedWithTime, TypeId.C_SE_TA_1},
            {IEC104DataType.SetpointCommandScaledWithTime, TypeId.C_SE_TB_1},
            {IEC104DataType.SetpointCommandFloatWithTime, TypeId.C_SE_TC_1},
            {IEC104DataType.Bitstring32Command, TypeId.C_BO_NA_1},
            {IEC104DataType.Bitstring32CommandWithTime, TypeId.C_BO_TA_1},
            {IEC104DataType.GeneralInterrogation, TypeId.C_IC_NA_1},
            {IEC104DataType.CounterInterrogation, TypeId.C_CI_NA_1},
            {IEC104DataType.ReadCommand, TypeId.C_RD_NA_1},
            {IEC104DataType.ClockSynchronization, TypeId.C_CS_NA_1},
            {IEC104DataType.TestCommand, TypeId.C_TS_NA_1},
            {IEC104DataType.ResetProcess, TypeId.C_RP_NA_1},
            {IEC104DataType.DelayAcquisition, TypeId.C_CD_NA_1},
            {IEC104DataType.EndOfInitialization, TypeId.M_EI_NA_1}
        };

        /// <summary>
        /// TypeId to IEC104DataType mapping (reverse lookup)
        /// </summary>
        private static readonly Dictionary<TypeId, IEC104DataType> TypeIdToDataType =
            new Dictionary<TypeId, IEC104DataType>();

        #endregion

        #region STATIC CONSTRUCTOR

        static TypeIdMapper()
        {
            // Build reverse lookup dictionary
            foreach (var kvp in DataTypeToTypeId)
            {
                TypeIdToDataType[kvp.Value] = kvp.Key;
            }
        }

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Get IEC104DataType from TagType string
        /// </summary>
        public static IEC104DataType? GetDataType(string tagType)
        {
            if (string.IsNullOrEmpty(tagType))
                return null;

            if (TagTypeToDataType.TryGetValue(tagType.Trim(), out IEC104DataType dataType))
                return dataType;

            return null;
        }

        /// <summary>
        /// Get IEC104DataType from TagType string with exception on failure
        /// </summary>
        public static IEC104DataType GetDataTypeOrThrow(string tagType)
        {
            var dataType = GetDataType(tagType);
            if (!dataType.HasValue)
                throw new ConfigurationException(IEC104ErrorCode.InvalidDataType, "TagType", tagType);

            return dataType.Value;
        }

        /// <summary>
        /// Get TypeId from IEC104DataType
        /// </summary>
        public static TypeId GetTypeId(IEC104DataType dataType)
        {
            if (DataTypeToTypeId.TryGetValue(dataType, out TypeId typeId))
                return typeId;

            throw new ProtocolException(IEC104ErrorCode.InvalidTypeId, $"No TypeId mapping for DataType: {dataType}");
        }

        /// <summary>
        /// Get IEC104DataType from TypeId
        /// </summary>
        public static IEC104DataType GetDataType(TypeId typeId)
        {
            if (TypeIdToDataType.TryGetValue(typeId, out IEC104DataType dataType))
                return dataType;

            throw new ProtocolException(IEC104ErrorCode.InvalidTypeId, $"No DataType mapping for TypeId: {typeId}");
        }

        /// <summary>
        /// Get TypeId from TagType string
        /// </summary>
        public static TypeId? GetTypeId(string tagType)
        {
            var dataType = GetDataType(tagType);
            return dataType.HasValue ? GetTypeId(dataType.Value) : (TypeId?)null;
        }

        /// <summary>
        /// Get TypeId from TagType string with exception on failure
        /// </summary>
        public static TypeId GetTypeIdOrThrow(string tagType)
        {
            var typeId = GetTypeId(tagType);
            if (!typeId.HasValue)
                throw new ConfigurationException(IEC104ErrorCode.InvalidTagType, "TagType", tagType);

            return typeId.Value;
        }

        /// <summary>
        /// Get standard TagType string from IEC104DataType
        /// </summary>
        public static string GetTagType(IEC104DataType dataType)
        {
            // Return the primary/standard name for each data type
            switch (dataType)
            {
                case IEC104DataType.SinglePoint: return "SinglePoint";
                case IEC104DataType.SinglePointWithTime: return "SinglePointWithTime";
                case IEC104DataType.DoublePoint: return "DoublePoint";
                case IEC104DataType.DoublePointWithTime: return "DoublePointWithTime";
                case IEC104DataType.StepPosition: return "StepPosition";
                case IEC104DataType.StepPositionWithTime: return "StepPositionWithTime";
                case IEC104DataType.Bitstring32: return "Bitstring32";
                case IEC104DataType.Bitstring32WithTime: return "Bitstring32WithTime";
                case IEC104DataType.MeasuredValueNormalized: return "MeasuredValueNormalized";
                case IEC104DataType.MeasuredValueScaled: return "MeasuredValueScaled";
                case IEC104DataType.MeasuredValueFloat: return "MeasuredValueFloat";
                case IEC104DataType.MeasuredValueNormalizedWithTime: return "MeasuredValueNormalizedWithTime";
                case IEC104DataType.MeasuredValueScaledWithTime: return "MeasuredValueScaledWithTime";
                case IEC104DataType.MeasuredValueFloatWithTime: return "MeasuredValueFloatWithTime";
                case IEC104DataType.IntegratedTotals: return "IntegratedTotals";
                case IEC104DataType.IntegratedTotalsWithTime: return "IntegratedTotalsWithTime";
                case IEC104DataType.SingleCommand: return "SingleCommand";
                case IEC104DataType.SingleCommandWithTime: return "SingleCommandWithTime";
                case IEC104DataType.DoubleCommand: return "DoubleCommand";
                case IEC104DataType.DoubleCommandWithTime: return "DoubleCommandWithTime";
                case IEC104DataType.RegulatingStepCommand: return "RegulatingStepCommand";
                case IEC104DataType.RegulatingStepCommandWithTime: return "RegulatingStepCommandWithTime";
                case IEC104DataType.SetpointCommandNormalized: return "SetpointCommandNormalized";
                case IEC104DataType.SetpointCommandScaled: return "SetpointCommandScaled";
                case IEC104DataType.SetpointCommandFloat: return "SetpointCommandFloat";
                case IEC104DataType.SetpointCommandNormalizedWithTime: return "SetpointCommandNormalizedWithTime";
                case IEC104DataType.SetpointCommandScaledWithTime: return "SetpointCommandScaledWithTime";
                case IEC104DataType.SetpointCommandFloatWithTime: return "SetpointCommandFloatWithTime";
                case IEC104DataType.Bitstring32Command: return "Bitstring32Command";
                case IEC104DataType.Bitstring32CommandWithTime: return "Bitstring32CommandWithTime";
                case IEC104DataType.GeneralInterrogation: return "GeneralInterrogation";
                case IEC104DataType.CounterInterrogation: return "CounterInterrogation";
                case IEC104DataType.ReadCommand: return "ReadCommand";
                case IEC104DataType.ClockSynchronization: return "ClockSynchronization";
                case IEC104DataType.TestCommand: return "TestCommand";
                case IEC104DataType.ResetProcess: return "ResetProcess";
                case IEC104DataType.DelayAcquisition: return "DelayAcquisition";
                case IEC104DataType.EndOfInitialization: return "EndOfInitialization";
                default:
                    return dataType.ToString();
            }
        }

        /// <summary>
        /// Get standard TagType string from TypeId
        /// </summary>
        public static string GetTagType(TypeId typeId)
        {
            var dataType = GetDataType(typeId);
            return GetTagType(dataType);
        }

        /// <summary>
        /// Check if TagType is supported
        /// </summary>
        public static bool IsSupported(string tagType)
        {
            return GetDataType(tagType).HasValue;
        }

        /// <summary>
        /// Check if TypeId is supported
        /// </summary>
        public static bool IsSupported(TypeId typeId)
        {
            return TypeIdToDataType.ContainsKey(typeId);
        }

        /// <summary>
        /// Check if IEC104DataType is supported
        /// </summary>
        public static bool IsSupported(IEC104DataType dataType)
        {
            return DataTypeToTypeId.ContainsKey(dataType);
        }

        /// <summary>
        /// Get all supported TagType strings
        /// </summary>
        public static IEnumerable<string> GetSupportedTagTypes()
        {
            return TagTypeToDataType.Keys;
        }

        /// <summary>
        /// Get all supported TypeIds
        /// </summary>
        public static IEnumerable<TypeId> GetSupportedTypeIds()
        {
            return DataTypeToTypeId.Values;
        }

        /// <summary>
        /// Get all supported IEC104DataTypes
        /// </summary>
        public static IEnumerable<IEC104DataType> GetSupportedDataTypes()
        {
            return DataTypeToTypeId.Keys;
        }

        /// <summary>
        /// Get mapping information for debugging
        /// </summary>
        public static string GetMappingInfo(string tagType)
        {
            var dataType = GetDataType(tagType);
            if (!dataType.HasValue)
                return $"TagType '{tagType}' is not supported";

            var typeId = GetTypeId(dataType.Value);
            return $"TagType: {tagType} → DataType: {dataType.Value} → TypeId: {typeId} ({(int)typeId})";
        }

        #endregion
    }
}