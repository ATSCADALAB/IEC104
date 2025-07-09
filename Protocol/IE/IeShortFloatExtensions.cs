namespace IEC104.Protocol.IE
{
    public static class IeShortFloatExtensions
    {
        public static bool IsQualityGood(this IeShortFloat sf)
        {
            return true; // Placeholder
        }

        public static float GetValue(this IeShortFloat sf)
        {
            return 0.0f; // Placeholder - implement based on actual structure
        }

        public static IeQuality GetQuality(this IeShortFloat sf)
        {
            return IeQuality.CreateGood(); // Placeholder
        }
    }
}