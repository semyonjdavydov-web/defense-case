namespace MotionGenerator
{
    // Store the case-specific text without changing the Word motion's formatting.
    public class MotionContent
    {
        public string MotionTitle { get; set; } = string.Empty;
        public string OpeningRequest { get; set; } = string.Empty;
        public List<string> BackgroundParagraphs { get; set; } = new List<string>();
        public string MissingDiscoveryIntroduction { get; set; } = string.Empty;
        public string PreliminaryArgument { get; set; } = string.Empty;
        public string PointOneHeading { get; set; } = string.Empty;
        public List<string> PointOneParagraphs { get; set; } = new List<string>();
        public string PointTwoHeading { get; set; } = string.Empty;
        public List<string> PointTwoParagraphs { get; set; } = new List<string>();
        public string RequestedRelief { get; set; } = string.Empty;
        public DateTime MotionDate { get; set; } = DateTime.Today;
    }
}
