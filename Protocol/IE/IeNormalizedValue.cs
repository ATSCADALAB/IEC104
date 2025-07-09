using IEC104.Protocol.IE.Base;
using System.IO;

namespace IEC104.Protocol.IE
{
    public class IeNormalizedValue : InformationElement
    {
        private float value;
        private IeQuality quality;

        public IeNormalizedValue(float value, IeQuality quality)
        {
            this.value = value;
            this.quality = quality;
        }

        public IeNormalizedValue(float value) : this(value, IeQuality.CreateGood())
        {
        }

        public IeNormalizedValue(BinaryReader reader)
        {
            // Read 2 bytes for normalized value (-1.0 to +1.0 mapped to -32768 to +32767)
            short shortValue = reader.ReadInt16();
            value = shortValue / 32768.0f;

            // Read quality
            quality = new IeQuality(reader);
        }

        public float GetNormalizedValue()
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
            return $"{value:F6}";
        }
    }
}