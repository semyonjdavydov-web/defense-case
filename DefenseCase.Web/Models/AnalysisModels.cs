using System.Text.Json.Serialization;

namespace DefenseCase.Web.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DiscoveryStatus { PRESENT, MISSING, NOT_APPLICABLE, UNDETERMINED }

public class ChecklistItem
{
    public string Id { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string StatutoryBasis { get; set; } = "";
    public string Applicability { get; set; } = "";
}

public class DiscoveryChecklistResult
{
    public string Id { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string StatutoryBasis { get; set; } = "";
    public DiscoveryStatus Status { get; set; }
    public string ReasonExpected { get; set; } = "";
    public string EvidenceFound { get; set; } = "";
    public string SourceFile { get; set; } = "";
    public int? PageNumber { get; set; }
    public string LegalSignificance { get; set; } = "";
}

public class DiscoveryAnalysisResult
{
    public List<string> DocumentsReviewed { get; set; } = [];
    public List<DiscoveryChecklistResult> Checklist { get; set; } = [];
    public string Summary { get; set; } = "";
    public string ProceduralFindings { get; set; } = "";
    [JsonIgnore]
    public List<DiscoveryChecklistResult> MissingItems => Checklist.Where(x => x.Status == DiscoveryStatus.MISSING).ToList();
}

public class LegalArgumentResult
{
    public string PreliminaryTimelinessArgument { get; set; } = "";
    public string PointOneHeading { get; set; } = "";
    public List<string> PointOneParagraphs { get; set; } = [];
    public string PointTwoHeading { get; set; } = "";
    public List<string> PointTwoParagraphs { get; set; } = [];
    public string RequestedRelief { get; set; } = "";
    public List<string> AuthoritiesUsed { get; set; } = [];
    public bool SupportsInvalidation { get; set; }
    public bool SupportsDismissal { get; set; }
}

public record UploadedDocument(string Name, string ContentType, byte[] Bytes);

public class CaseWork
{
    public CaseInformation Case { get; set; } = new();
    public string Status { get; set; } = "Awaiting upload";
    public string? Error { get; set; }
    public bool Running { get; set; }
    public DiscoveryAnalysisResult? Discovery { get; set; }
    public LegalArgumentResult? Argument { get; set; }
    public byte[]? Motion { get; set; }
}
