using System;
using System.Collections.Generic;
using IEC104.Constants;
using IEC104.Exceptions;
using IEC104.Configuration;

namespace IEC104.Mapping
{
    /// <summary>
    /// Mapping between ATDriver TagAddress and IEC104 IOA (Information Object Address)
    /// </summary>
    public static class AddressMapper
    {
        #region PUBLIC METHODS

        /// <summary>
        /// Parse TagAddress to get IOA
        /// Format: "1001" or "1001:qualifier" or "1001:qualifier:select"
        /// </summary>
        public static int ParseIOA(string tagAddress)
        {
            if (string.IsNullOrEmpty(tagAddress))
                throw new ConfigurationException(IEC104ErrorCode.InvalidTagAddress, "TagAddress", tagAddress);

            try
            {
                var parts = tagAddress.Split(':');

                if (!int.TryParse(parts[0], out int ioa))
                    throw new ConfigurationException(IEC104ErrorCode.InvalidTagAddress, "IOA", parts[0]);

                if (ioa < IEC104Constants.MinIOA || ioa > IEC104Constants.MaxIOA)
                    throw new ConfigurationException(IEC104ErrorCode.InvalidIOA, "IOA", ioa.ToString());

                return ioa;
            }
            catch (ConfigurationException)
            {
                throw; // Re-throw configuration exceptions
            }
            catch (Exception)
            {
                throw new ConfigurationException(IEC104ErrorCode.InvalidTagAddress, "TagAddress", tagAddress);
            }
        }

        /// <summary>
        /// Parse TagAddress to get command qualifier
        /// </summary>
        public static byte ParseCommandQualifier(string tagAddress)
        {
            if (string.IsNullOrEmpty(tagAddress))
                return IEC104Constants.DefaultCommandQualifier;

            try
            {
                var parts = tagAddress.Split(':');

                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                {
                    if (byte.TryParse(parts[1], out byte qualifier))
                        return qualifier;
                }

                return IEC104Constants.DefaultCommandQualifier;
            }
            catch
            {
                return IEC104Constants.DefaultCommandQualifier;
            }
        }

        /// <summary>
        /// Parse TagAddress to get command select flag
        /// </summary>
        public static bool ParseCommandSelect(string tagAddress)
        {
            if (string.IsNullOrEmpty(tagAddress))
                return IEC104Constants.DefaultCommandSelect;

            try
            {
                var parts = tagAddress.Split(':');

                if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
                {
                    if (bool.TryParse(parts[2], out bool select))
                        return select;
                }

                return IEC104Constants.DefaultCommandSelect;
            }
            catch
            {
                return IEC104Constants.DefaultCommandSelect;
            }
        }

        /// <summary>
        /// Create TagAddress string from IOA
        /// </summary>
        public static string CreateTagAddress(int ioa)
        {
            ValidateIOA(ioa);
            return ioa.ToString();
        }

        /// <summary>
        /// Create TagAddress string from IOA with qualifier
        /// </summary>
        public static string CreateTagAddress(int ioa, byte qualifier)
        {
            ValidateIOA(ioa);
            return $"{ioa}:{qualifier}";
        }

        /// <summary>
        /// Create TagAddress string from IOA with qualifier and select
        /// </summary>
        public static string CreateTagAddress(int ioa, byte qualifier, bool select)
        {
            ValidateIOA(ioa);
            return $"{ioa}:{qualifier}:{select}";
        }

        /// <summary>
        /// Validate IOA range
        /// </summary>
        public static void ValidateIOA(int ioa)
        {
            if (ioa < IEC104Constants.MinIOA || ioa > IEC104Constants.MaxIOA)
                throw new ConfigurationException(IEC104ErrorCode.InvalidIOA, "IOA", ioa.ToString());
        }

        /// <summary>
        /// Check if IOA is valid
        /// </summary>
        public static bool IsValidIOA(int ioa)
        {
            return ioa >= IEC104Constants.MinIOA && ioa <= IEC104Constants.MaxIOA;
        }

        /// <summary>
        /// Check if TagAddress format is valid
        /// </summary>
        public static bool IsValidTagAddress(string tagAddress)
        {
            try
            {
                ParseIOA(tagAddress);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parse complete TagAddress information
        /// </summary>
        public static (int ioa, byte qualifier, bool select) ParseCompleteAddress(string tagAddress)
        {
            var ioa = ParseIOA(tagAddress);
            var qualifier = ParseCommandQualifier(tagAddress);
            var select = ParseCommandSelect(tagAddress);

            return (ioa, qualifier, select);
        }

        /// <summary>
        /// Get address information for debugging
        /// </summary>
        public static string GetAddressInfo(string tagAddress)
        {
            try
            {
                var (ioa, qualifier, select) = ParseCompleteAddress(tagAddress);
                return $"TagAddress: {tagAddress} → IOA: {ioa}, Qualifier: {qualifier}, Select: {select}";
            }
            catch (Exception ex)
            {
                return $"TagAddress: {tagAddress} → Error: {ex.Message}";
            }
        }

        #endregion
    }
}