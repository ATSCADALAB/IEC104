using IEC104.Constants;
using IEC104.Protocol.IE.Base;

namespace IEC104.Mapping
{
    /// <summary>
    /// Mapping between IEC104 Quality indicators and ATDriver status strings
    /// </summary>
    public static class QualityMapper
    {
        #region CONSTANTS

        public const string GoodQuality = "Good";
        public const string BadQuality = "Bad";
        public const string UncertainQuality = "Uncertain";

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Convert IEC104 quality to ATDriver status string
        /// </summary>
        public static string ConvertToStatusString(IeAbstractQuality quality)
        {
            if (quality == null)
                return BadQuality;

            // Check for invalid flag
            if (quality.IsInvalid())
                return BadQuality;

            // Check for blocked flag
            if (quality.IsBlocked())
                return UncertainQuality;

            // Check for substituted flag
            if (quality.IsSubstituted())
                return UncertainQuality;

            // Check for not topical flag
            if (quality.IsNotTopical())
                return UncertainQuality;

            // If none of the bad flags are set, consider it good
            return GoodQuality;
        }

        /// <summary>
        /// Convert IEC104 quality to ATDriver status string (overload for any IE with quality)
        /// </summary>
        public static string ConvertToStatusString(InformationElement ie)
        {
            if (ie == null)
                return BadQuality;

            // Try to cast to quality-bearing IE types
            if (ie is IeSinglePointWithQuality sp)
                return ConvertToStatusString(sp.GetQuality());
            if (ie is IeDoublePointWithQuality dp)
                return ConvertToStatusString(dp.GetQuality());
            if (ie is IeShortFloat sf)
                return ConvertToStatusString(sf.GetQuality());
            if (ie is IeNormalizedValue nv)
                return ConvertToStatusString(nv.GetQuality());
            if (ie is IeScaledValue sv)
                return ConvertToStatusString(sv.GetQuality());
            if (ie is IeBinaryCounterReading bcr)
                return ConvertToStatusString(bcr.GetQuality());

            // For IE types without quality, assume good
            return GoodQuality;
        }

        /// <summary>
        /// Check if quality indicates good value
        /// </summary>
        public static bool IsGoodQuality(IeAbstractQuality quality)
        {
            return ConvertToStatusString(quality) == GoodQuality;
        }

        /// <summary>
        /// Check if quality indicates good value (overload for IE)
        /// </summary>
        public static bool IsGoodQuality(InformationElement ie)
        {
            return ConvertToStatusString(ie) == GoodQuality;
        }

        /// <summary>
        /// Check if quality indicates bad value
        /// </summary>
        public static bool IsBadQuality(IeAbstractQuality quality)
        {
            return ConvertToStatusString(quality) == BadQuality;
        }

        /// <summary>
        /// Check if quality indicates bad value (overload for IE)
        /// </summary>
        public static bool IsBadQuality(InformationElement ie)
        {
            return ConvertToStatusString(ie) == BadQuality;
        }

        /// <summary>
        /// Check if quality indicates uncertain value
        /// </summary>
        public static bool IsUncertainQuality(IeAbstractQuality quality)
        {
            return ConvertToStatusString(quality) == UncertainQuality;
        }

        /// <summary>
        /// Check if quality indicates uncertain value (overload for IE)
        /// </summary>
        public static bool IsUncertainQuality(InformationElement ie)
        {
            return ConvertToStatusString(ie) == UncertainQuality;
        }

        /// <summary>
        /// Get detailed quality information for debugging
        /// </summary>
        public static string GetQualityDetails(IeAbstractQuality quality)
        {
            if (quality == null)
                return "Quality: null";

            var details = new System.Text.StringBuilder();
            details.Append($"Quality: {ConvertToStatusString(quality)}");

            if (quality.IsInvalid())
                details.Append(" [Invalid]");
            if (quality.IsBlocked())
                details.Append(" [Blocked]");
            if (quality.IsSubstituted())
                details.Append(" [Substituted]");
            if (quality.IsNotTopical())
                details.Append(" [Not Topical]");

            return details.ToString();
        }

        /// <summary>
        /// Get detailed quality information for debugging (overload for IE)
        /// </summary>
        public static string GetQualityDetails(InformationElement ie)
        {
            if (ie == null)
                return "Quality: null (IE is null)";

            // Try to get quality from IE
            if (ie is IeSinglePointWithQuality sp)
                return GetQualityDetails(sp.GetQuality());
            if (ie is IeDoublePointWithQuality dp)
                return GetQualityDetails(dp.GetQuality());
            if (ie is IeShortFloat sf)
                return GetQualityDetails(sf.GetQuality());
            if (ie is IeNormalizedValue nv)
                return GetQualityDetails(nv.GetQuality());
            if (ie is IeScaledValue sv)
                return GetQualityDetails(sv.GetQuality());
            if (ie is IeBinaryCounterReading bcr)
                return GetQualityDetails(bcr.GetQuality());

            return $"Quality: {GoodQuality} (IE type: {ie.GetType().Name} - no quality info)";
        }

        /// <summary>
        /// Create quality flags summary for monitoring
        /// </summary>
        public static string GetQualitySummary(IeAbstractQuality quality)
        {
            if (quality == null)
                return "NULL";

            var flags = new System.Collections.Generic.List<string>();

            if (quality.IsInvalid())
                flags.Add("IV");
            if (quality.IsBlocked())
                flags.Add("BL");
            if (quality.IsSubstituted())
                flags.Add("SB");
            if (quality.IsNotTopical())
                flags.Add("NT");

            return flags.Count > 0 ? string.Join(",", flags) : "OK";
        }

        /// <summary>
        /// Convert quality to numeric code for legacy systems
        /// 0 = Good, 1 = Uncertain, 2 = Bad
        /// </summary>
        public static int ConvertToNumericQuality(IeAbstractQuality quality)
        {
            var status = ConvertToStatusString(quality);
            switch (status)
            {
                case GoodQuality:
                    return 0;
                case UncertainQuality:
                    return 1;
                case BadQuality:
                default:
                    return 2;
            }
        }

        /// <summary>
        /// Convert quality to numeric code for legacy systems (overload for IE)
        /// </summary>
        public static int ConvertToNumericQuality(InformationElement ie)
        {
            var status = ConvertToStatusString(ie);
            switch (status)
            {
                case GoodQuality:
                    return 0;
                case UncertainQuality:
                    return 1;
                case BadQuality:
                default:
                    return 2;
            }
        }

        /// <summary>
        /// Check if value should be used based on quality
        /// </summary>
        public static bool ShouldUseValue(IeAbstractQuality quality)
        {
            // Use value if quality is Good or Uncertain, but not Bad
            return !IsBadQuality(quality);
        }

        /// <summary>
        /// Check if value should be used based on quality (overload for IE)
        /// </summary>
        public static bool ShouldUseValue(InformationElement ie)
        {
            return !IsBadQuality(ie);
        }

        #endregion
    }
}