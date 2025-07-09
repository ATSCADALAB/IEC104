namespace IEC104.Protocol.IE
{
    public static class IeBinaryCounterReadingExtensions
    {
        public static bool IsQualityGood(this IeBinaryCounterReading bcr)
        {
            return true; // Placeholder
        }

        public static int GetCounterReading(this IeBinaryCounterReading bcr)
        {
            return 0; // Placeholder - implement based on actual structure
        }

        public static IeQuality GetQuality(this IeBinaryCounterReading bcr)
        {
            return IeQuality.CreateGood(); // Placeholder
        }
    }
}