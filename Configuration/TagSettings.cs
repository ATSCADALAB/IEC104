using System;
using IEC104.Common;
using IEC104.Constants;
using IEC104.Exceptions;
using IEC104.Protocol.Enum;

namespace IEC104.Configuration
{
    /// <summary>
    /// Tag configuration settings
    /// </summary>
    public class TagSettings
    {
        #region PROPERTIES

        /// <summary>
        /// Information Object Address
        /// </summary>
        public int IOA { get; set; }

        /// <summary>
        /// IEC104 Data Type
        /// </summary>
        public IEC104DataType DataType { get; set; }

        /// <summary>
        /// Protocol Type ID
        /// </summary>
        public TypeId TypeId { get; set; }

        /// <summary>
        /// Access rights
        /// </summary>
        public string AccessRight { get; set; }

        /// <summary>
        /// Tag name
        /// </summary>
        public string TagName { get; set; }

        /// <summary>
        /// Tag description
        /// </summary>
        public string TagDescription { get; set; }

        /// <summary>
        /// Original TagAddress string
        /// </summary>
        public string TagAddress { get; set; }

        /// <summary>
        /// Original TagType string
        /// </summary>
        public string TagType { get; set; }

        /// <summary>
        /// Command qualifier for write operations
        /// </summary>
        public byte CommandQualifier { get; set; }

        /// <summary>
        /// Command select flag for write operations
        /// </summary>
        public bool CommandSelect { get; set; }

        /// <summary>
        /// Value scaling factor for analog values
        /// </summary>
        public double ScalingFactor { get; set; }

        /// <summary>
        /// Value offset for analog values
        /// </summary>
        public double Offset { get; set; }

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Default constructor
        /// </summary>
        public TagSettings()
        {
            SetDefaults();
        }

        /// <summary>
        /// Constructor with tag address and type
        /// </summary>
        public TagSettings(string tagAddress, string tagType)
        {
            SetDefaults();
            TagAddress = tagAddress;
            TagType = tagType;
            Parse(tagAddress, tagType);
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Set default values
        /// </summary>
        private void SetDefaults()
        {
            IOA = DefaultValues.DefaultIOA;
            DataType = IEC104DataType.SinglePoint;
            TypeId = TypeId.M_SP_NA_1;
            AccessRight = DefaultValues.DefaultAccessRight;
            CommandQualifier = DefaultValues.DefaultCommandQualifier;
            CommandSelect = DefaultValues.DefaultCommandSelect;
            ScalingFactor = 1.0;
            Offset = 0.0;
        }

        /// <summary>
        /// Parse tag address and type
        /// </summary>
        public void Parse(string tagAddress, string tagType)
        {
            ParseTagAddress(tagAddress);
            ParseTagType(tagType);
        }

        /// <summary>
        /// Parse tag address to get IOA
        /// Format: "1001" or "1001:qualifier" or "1001:qualifier:select"
        /// </summary>
        private void ParseTagAddress(string tagAddress)
        {
            if (string.IsNullOrEmpty(tagAddress))
                throw new ConfigurationException(IEC104ErrorCode.InvalidTagAddress, "TagAddress", tagAddress);

            try
            {
                var parts = tagAddress.Split(':');

                // Parse IOA
                if (!int.TryParse(parts[0], out int ioa) || ioa < IEC104Constants.MinIOA || ioa > IEC104Constants.MaxIOA)
                    throw new ConfigurationException(IEC104ErrorCode.InvalidTagAddress, "IOA", parts[0]);
                IOA = ioa;

                // Parse command qualifier
                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                {
                    if (byte.TryParse(parts[1], out byte qualifier))
                        CommandQualifier = qualifier;
                }

                // Parse command select flag
                if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
                {
                    if (bool.TryParse(parts[2], out bool select))
                        CommandSelect = select;
                }
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
        /// Parse tag type to get DataType and TypeId
        /// </summary>
        private void ParseTagType(string tagType)
        {
            if (string.IsNullOrEmpty(tagType))
                throw new ConfigurationException(IEC104ErrorCode.InvalidTagType, "TagType", tagType);

            var dataType = IEC104DataTypeHelper.ParseDataType(tagType);
            if (!dataType.HasValue)
                throw new ConfigurationException(IEC104ErrorCode.InvalidTagType, "TagType", tagType);

            DataType = dataType.Value;
            TypeId = IEC104DataTypeHelper.GetTypeId(DataType);
        }

        /// <summary>
        /// Validate tag settings
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = null;

            if (IOA < IEC104Constants.MinIOA || IOA > IEC104Constants.MaxIOA)
            {
                errorMessage = $"IOA must be between {IEC104Constants.MinIOA} and {IEC104Constants.MaxIOA}";
                return false;
            }

            if (!Enum.IsDefined(typeof(IEC104DataType), DataType))
            {
                errorMessage = "Invalid data type";
                return false;
            }

            if (!Enum.IsDefined(typeof(TypeId), TypeId))
            {
                errorMessage = "Invalid Type ID";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if tag is readable
        /// </summary>
        public bool IsReadable()
        {
            return IEC104DataTypeHelper.IsReadable(DataType);
        }

        /// <summary>
        /// Check if tag is writable
        /// </summary>
        public bool IsWritable()
        {
            return IEC104DataTypeHelper.IsWritable(DataType);
        }

        /// <summary>
        /// Check if tag has timestamp
        /// </summary>
        public bool HasTimestamp()
        {
            return IEC104DataTypeHelper.HasTimestamp(DataType);
        }

        #endregion

        #region STATIC METHODS

        /// <summary>
        /// Create TagSettings from tag address and type
        /// </summary>
        public static TagSettings Create(string tagAddress, string tagType)
        {
            return new TagSettings(tagAddress, tagType);
        }

        /// <summary>
        /// Validate tag address and type without creating instance
        /// </summary>
        public static bool ValidateTag(string tagAddress, string tagType, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                var settings = new TagSettings(tagAddress, tagType);
                return settings.IsValid(out errorMessage);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        #endregion

        #region OVERRIDES

        public override string ToString()
        {
            return $"IOA {IOA} ({DataType})";
        }

        public override bool Equals(object obj)
        {
            if (obj is TagSettings other)
            {
                return IOA == other.IOA && DataType == other.DataType;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return $"{IOA}:{DataType}".GetHashCode();
        }

        #endregion
    }
}
