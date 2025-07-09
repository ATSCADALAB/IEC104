namespace IEC104.Protocol.IE
{
    public static class IeDoublePointWithQualityExtensions
    {
        public static bool IsQualityGood(this IeDoublePointWithQuality dp)
        {
            return true; // Placeholder
        }

        public static byte GetDoublePointState(this IeDoublePointWithQuality dp)
        {
            return 1; // Placeholder - implement based on actual structure
        }

        public static IeQuality GetQuality(this IeDoublePointWithQuality dp)
        {
            return IeQuality.CreateGood(); // Placeholder
        }
    }
}