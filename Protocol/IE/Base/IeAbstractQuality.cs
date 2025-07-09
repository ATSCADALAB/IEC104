using System.IO;

namespace IEC104.Protocol.IE.Base
{
    public abstract class IeAbstractQuality : InformationElement
    {
        protected bool invalid;
        protected bool notTopical;
        protected bool substituted;
        protected bool blocked;

        protected IeAbstractQuality(bool invalid, bool notTopical, bool substituted, bool blocked)
        {
            this.invalid = invalid;
            this.notTopical = notTopical;
            this.substituted = substituted;
            this.blocked = blocked;
        }

        protected IeAbstractQuality(BinaryReader reader)
        {
            byte qualityByte = reader.ReadByte();
            ParseQualityByte(qualityByte);
        }

        protected IeAbstractQuality(byte qualityByte)
        {
            ParseQualityByte(qualityByte);
        }

        private void ParseQualityByte(byte qualityByte)
        {
            invalid = (qualityByte & 0x80) != 0;      // Bit 7
            notTopical = (qualityByte & 0x40) != 0;   // Bit 6
            substituted = (qualityByte & 0x20) != 0;  // Bit 5
            blocked = (qualityByte & 0x10) != 0;      // Bit 4
        }

        public bool IsInvalid()
        {
            return invalid;
        }

        public bool IsNotTopical()
        {
            return notTopical;
        }

        public bool IsSubstituted()
        {
            return substituted;
        }

        public bool IsBlocked()
        {
            return blocked;
        }

        public byte GetQualityByte()
        {
            byte result = 0;
            if (invalid) result |= 0x80;
            if (notTopical) result |= 0x40;
            if (substituted) result |= 0x20;
            if (blocked) result |= 0x10;
            return result;
        }

        public override string ToString()
        {
            var flags = new System.Collections.Generic.List<string>();
            if (invalid) flags.Add("IV");
            if (notTopical) flags.Add("NT");
            if (substituted) flags.Add("SB");
            if (blocked) flags.Add("BL");
            return flags.Count > 0 ? string.Join(",", flags) : "OK";
        }
    }
}