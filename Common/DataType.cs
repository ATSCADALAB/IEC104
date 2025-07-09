using IEC104.Protocol.Enum;

namespace IEC104.Common
{
    /// <summary>
    /// IEC104 data types mapping to ATDriver TagType
    /// </summary>
    public enum IEC104DataType
    {
        // === PROCESS INFORMATION ===
        
        // Single-point information
        SinglePoint = 1,           // M_SP_NA_1 (TypeId 1) - Binary status
        SinglePointWithTime = 30,  // M_SP_TB_1 (TypeId 30) - Binary status with time
        
        // Double-point information  
        DoublePoint = 3,           // M_DP_NA_1 (TypeId 3) - 3-state status
        DoublePointWithTime = 31,  // M_DP_TB_1 (TypeId 31) - 3-state status with time
        
        // Step position information
        StepPosition = 5,          // M_ST_NA_1 (TypeId 5) - Step position
        StepPositionWithTime = 32, // M_ST_TB_1 (TypeId 32) - Step position with time
        
        // Bitstring information
        Bitstring32 = 7,           // M_BO_NA_1 (TypeId 7) - 32-bit bitstring
        Bitstring32WithTime = 33,  // M_BO_TB_1 (TypeId 33) - 32-bit bitstring with time
        
        // Measured values
        MeasuredValueNormalized = 9,        // M_ME_NA_1 (TypeId 9) - Normalized value
        MeasuredValueScaled = 11,           // M_ME_NB_1 (TypeId 11) - Scaled value  
        MeasuredValueFloat = 13,            // M_ME_NC_1 (TypeId 13) - Short floating point
        MeasuredValueNormalizedWithTime = 34, // M_ME_TD_1 (TypeId 34)
        MeasuredValueScaledWithTime = 35,   // M_ME_TE_1 (TypeId 35)
        MeasuredValueFloatWithTime = 36,    // M_ME_TF_1 (TypeId 36)
        
        // Integrated totals
        IntegratedTotals = 15,      // M_IT_NA_1 (TypeId 15) - Counter values
        IntegratedTotalsWithTime = 37, // M_IT_TB_1 (TypeId 37) - Counter with time
        
        // === CONTROL COMMANDS ===
        
        // Single commands
        SingleCommand = 45,         // C_SC_NA_1 (TypeId 45) - Single command
        SingleCommandWithTime = 58, // C_SC_TA_1 (TypeId 58) - Single command with time
        
        // Double commands
        DoubleCommand = 46,         // C_DC_NA_1 (TypeId 46) - Double command
        DoubleCommandWithTime = 59, // C_DC_TA_1 (TypeId 59) - Double command with time
        
        // Regulating step commands
        RegulatingStepCommand = 47,     // C_RC_NA_1 (TypeId 47) - Step command
        RegulatingStepCommandWithTime = 60, // C_RC_TA_1 (TypeId 60) - Step command with time
        
        // Set point commands
        SetpointCommandNormalized = 48,     // C_SE_NA_1 (TypeId 48) - Normalized setpoint
        SetpointCommandScaled = 49,         // C_SE_NB_1 (TypeId 49) - Scaled setpoint
        SetpointCommandFloat = 50,          // C_SE_NC_1 (TypeId 50) - Float setpoint
        SetpointCommandNormalizedWithTime = 61, // C_SE_TA_1 (TypeId 61)
        SetpointCommandScaledWithTime = 62, // C_SE_TB_1 (TypeId 62)
        SetpointCommandFloatWithTime = 63,  // C_SE_TC_1 (TypeId 63)
        
        // Bitstring commands
        Bitstring32Command = 51,        // C_BO_NA_1 (TypeId 51) - 32-bit bitstring command
        Bitstring32CommandWithTime = 64, // C_BO_TA_1 (TypeId 64)
        
        // === SYSTEM COMMANDS ===
        
        // Interrogation commands
        GeneralInterrogation = 100,     // C_IC_NA_1 (TypeId 100) - General interrogation
        CounterInterrogation = 101,     // C_CI_NA_1 (TypeId 101) - Counter interrogation
        ReadCommand = 102,              // C_RD_NA_1 (TypeId 102) - Read command
        ClockSynchronization = 103,     // C_CS_NA_1 (TypeId 103) - Clock sync
        TestCommand = 104,              // C_TS_NA_1 (TypeId 104) - Test command
        ResetProcess = 105,             // C_RP_NA_1 (TypeId 105) - Reset process
        DelayAcquisition = 106,         // C_CD_NA_1 (TypeId 106) - Delay acquisition
        
        // End of initialization
        EndOfInitialization = 70        // M_EI_NA_1 (TypeId 70) - End of initialization
    }
    
    /// <summary>
    /// Helper class for IEC104DataType operations
    /// </summary>
    public static class IEC104DataTypeHelper
    {
        /// <summary>
        /// Check if data type is readable (process information)
        /// </summary>
        public static bool IsReadable(IEC104DataType dataType)
        {
            switch (dataType)
            {
                case IEC104DataType.SinglePoint:
                case IEC104DataType.SinglePointWithTime:
                case IEC104DataType.DoublePoint:
                case IEC104DataType.DoublePointWithTime:
                case IEC104DataType.StepPosition:
                case IEC104DataType.StepPositionWithTime:
                case IEC104DataType.Bitstring32:
                case IEC104DataType.Bitstring32WithTime:
                case IEC104DataType.MeasuredValueNormalized:
                case IEC104DataType.MeasuredValueScaled:
                case IEC104DataType.MeasuredValueFloat:
                case IEC104DataType.MeasuredValueNormalizedWithTime:
                case IEC104DataType.MeasuredValueScaledWithTime:
                case IEC104DataType.MeasuredValueFloatWithTime:
                case IEC104DataType.IntegratedTotals:
                case IEC104DataType.IntegratedTotalsWithTime:
                case IEC104DataType.EndOfInitialization:
                    return true;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Check if data type is writable (commands)
        /// </summary>
        public static bool IsWritable(IEC104DataType dataType)
        {
            switch (dataType)
            {
                case IEC104DataType.SingleCommand:
                case IEC104DataType.SingleCommandWithTime:
                case IEC104DataType.DoubleCommand:
                case IEC104DataType.DoubleCommandWithTime:
                case IEC104DataType.RegulatingStepCommand:
                case IEC104DataType.RegulatingStepCommandWithTime:
                case IEC104DataType.SetpointCommandNormalized:
                case IEC104DataType.SetpointCommandScaled:
                case IEC104DataType.SetpointCommandFloat:
                case IEC104DataType.SetpointCommandNormalizedWithTime:
                case IEC104DataType.SetpointCommandScaledWithTime:
                case IEC104DataType.SetpointCommandFloatWithTime:
                case IEC104DataType.Bitstring32Command:
                case IEC104DataType.Bitstring32CommandWithTime:
                case IEC104DataType.GeneralInterrogation:
                case IEC104DataType.CounterInterrogation:
                case IEC104DataType.ReadCommand:
                case IEC104DataType.ClockSynchronization:
                case IEC104DataType.TestCommand:
                case IEC104DataType.ResetProcess:
                case IEC104DataType.DelayAcquisition:
                    return true;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Check if data type has timestamp
        /// </summary>
        public static bool HasTimestamp(IEC104DataType dataType)
        {
            switch (dataType)
            {
                case IEC104DataType.SinglePointWithTime:
                case IEC104DataType.DoublePointWithTime:
                case IEC104DataType.StepPositionWithTime:
                case IEC104DataType.Bitstring32WithTime:
                case IEC104DataType.MeasuredValueNormalizedWithTime:
                case IEC104DataType.MeasuredValueScaledWithTime:
                case IEC104DataType.MeasuredValueFloatWithTime:
                case IEC104DataType.IntegratedTotalsWithTime:
                case IEC104DataType.SingleCommandWithTime:
                case IEC104DataType.DoubleCommandWithTime:
                case IEC104DataType.RegulatingStepCommandWithTime:
                case IEC104DataType.SetpointCommandNormalizedWithTime:
                case IEC104DataType.SetpointCommandScaledWithTime:
                case IEC104DataType.SetpointCommandFloatWithTime:
                case IEC104DataType.Bitstring32CommandWithTime:
                    return true;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Get TypeId from IEC104DataType
        /// </summary>
        public static TypeId GetTypeId(IEC104DataType dataType)
        {
            return (TypeId)(int)dataType;
        }
        
        /// <summary>
        /// Get IEC104DataType from TypeId
        /// </summary>
        public static IEC104DataType GetDataType(TypeId typeId)
        {
            return (IEC104DataType)(int)typeId;
        }
        
        /// <summary>
        /// Get data type from string (TagType)
        /// </summary>
        public static IEC104DataType? ParseDataType(string tagType)
        {
            if (string.IsNullOrEmpty(tagType))
                return null;
                
            // Direct enum parsing
            if (Enum.TryParse<IEC104DataType>(tagType, true, out var dataType))
                return dataType;
                
            // Common aliases
            switch (tagType.ToLower())
            {
                case "sp":
                case "singlepoint":
                case "binary":
                    return IEC104DataType.SinglePoint;
                    
                case "dp":
                case "doublepoint":
                    return IEC104DataType.DoublePoint;
                    
                case "mv":
                case "measuredvalue":
                case "analog":
                case "float":
                    return IEC104DataType.MeasuredValueFloat;
                    
                case "it":
                case "counter":
                case "integratedtotals":
                    return IEC104DataType.IntegratedTotals;
                    
                case "sc":
                case "singlecommand":
                case "command":
                    return IEC104DataType.SingleCommand;
                    
                case "setpoint":
                case "setpointfloat":
                    return IEC104DataType.SetpointCommandFloat;
                    
                default:
                    return null;
            }
        }
    }
}