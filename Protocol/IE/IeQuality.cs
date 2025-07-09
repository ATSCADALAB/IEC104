using IEC104.Protocol.IE.Base;
using System.IO;

namespace IEC104.Protocol.IE
{
    public class IeQuality : IeAbstractQuality
    {
        public IeQuality(bool invalid, bool notTopical, bool substituted, bool blocked)
            : base(invalid, notTopical, substituted, blocked)
        {
        }

        public IeQuality(BinaryReader reader) : base(reader)
        {
        }

        public IeQuality(byte qualityByte) : base(qualityByte)
        {
        }

        public static IeQuality CreateGood()
        {
            return new IeQuality(false, false, false, false);
        }

        public static IeQuality CreateBad()
        {
            return new IeQuality(true, false, false, false);
        }
    }
}
