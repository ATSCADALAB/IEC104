using IEC104.Protocol.IE.Base;
using System.IO;

namespace IEC104.Protocol.IE
{
    public class IeScaledValue : InformationElement
    {
        private short value;
        private IeQuality quality;

        public IeScaledValue(short value, IeQuality quality)
        {
            this.value = value;
            this.quality = quality;
        }

        public IeScaledValue(short value) : this(value, IeQuality.CreateGood())
        {
        }

        public IeScaledValue(BinaryReader reader)
        {
            // Read 2 bytes for scaled value
            value = reader.ReadInt16();

            // Read quality
            quality = new IeQuality(reader);
        }

        public short GetScaledValue()
        {
            return value;
        }

        public IeQuality GetQuality()
        {
            return quality;
        }

        public bool IsQualityGood()
        {
            return quality != null && !quality.IsInvalid();
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}