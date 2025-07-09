namespace IEC104.Protocol.IE
{
    public static class IeSinglePointWithQualityExtensions
    {
        public static bool IsQualityGood(this IeSinglePointWithQuality sp)
        {
            // Assume we have access to quality field or method
            // This will be implemented based on actual IeSinglePointWithQuality structure
            return true; // Placeholder
        }

        public static bool IsOn(this IeSinglePointWithQuality sp)
        {
            // Implement based on actual structure
            return true; // Placeholder
        }

        public static IeQuality GetQuality(this IeSinglePointWithQuality sp)
        {
            // Return quality object
            return IeQuality.CreateGood(); // Placeholder
        }
    }
}