using IEC104.Protocol.IE.Base;
using System.IO;

namespace IEC104.Protocol.IE
{
    public class IeBinaryStateInformation : InformationElement
    {
        private uint value;
        private IeQuality quality;

        public IeBinaryStateInformation(uint value, IeQuality quality)
        {
            this.value = value;
            this.quality = quality;
        }

        public IeBinaryStateInformation(uint value) : this(value, IeQuality.CreateGood())
        {
        }

        public IeBinaryStateInformation(BinaryReader reader)
        {
            // Read 4 bytes for 32-bit bitstring
            value = reader.ReadUInt32();

            // Read quality
            quality = new IeQuality(reader);
        }

        public uint GetValue()
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
            return $"0x{value:X8}";
        }
    }
}