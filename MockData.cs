
namespace MotionGenerator
{
    // Create MockData class to store the discovery choices used by the program
    public static class MockData
    {
        // List of discovery items
        private static readonly string[] DiscoveryNames = new[]
        {
            "Police report",
            "Body-worn camera footage",
            "CAD records",
            "911 recordings",
            "Radio transmissions",
            "Witness statements",
            "Breathalyzer calibration records",
            "Breathalyzer maintenance records",
            "Operator certification",
            "Crash photographs",
            "Tow records"
        };

        // Create and return the list of discovery items
        public static List<DiscoveryItem> GetDiscoveryItems()
        {
            return DiscoveryNames
                .Select(n => new DiscoveryItem { Name = n })
                .ToList();
        }
    }
}
