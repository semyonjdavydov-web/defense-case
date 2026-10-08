using System.Text.Json;
using DefenseCase.Web.Models;
using DefenseCase.Web.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

// These tests use fictional facts and canned responses. They never call Azure AI.
int passed = 0;
int failed = 0;
string webFolder = args.Length > 0 ? Path.GetFullPath(args[0])
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DefenseCase.Web"));
var authorities = new AuthorityData(new TestEnvironment(webFolder));
using var http = new HttpClient();
var client = new AzureResponsesClient(http, new ConfigurationBuilder().Build());
var discoveryService = new DiscoveryAnalysisService(client, authorities);
var legalService = new LegalArgumentService(client, authorities);
var documentService = new DocumentGenerationService();
string bayCitation = authorities.Citations.Single(x => x.StartsWith("People v Bay,"));

Test("Completed structured response is parsed", () =>
{
    string response = CompletedResponse("{\"Name\":\"Fictional response\"}");
    Check(AzureResponsesClient.Parse<TestResponse>(response).Name == "Fictional response", "Parsed text did not match.");
});
Test("Malformed response JSON is rejected", () => Reject(() => AzureResponsesClient.Parse<TestResponse>("not JSON")));
Test("Missing response status is rejected", () => Reject(() => AzureResponsesClient.Parse<TestResponse>("{\"output\":[]}")));
Test("Incomplete response is rejected", () => Reject(() => AzureResponsesClient.Parse<TestResponse>("{\"status\":\"incomplete\",\"output\":[]}")));
Test("Missing output is rejected", () => Reject(() => AzureResponsesClient.Parse<TestResponse>("{\"status\":\"completed\"}")));
Test("Malformed structured payload is rejected", () => Reject(() => AzureResponsesClient.Parse<TestResponse>(CompletedResponse("not JSON"))));
Test("Missing message is rejected", () => Reject(() => AzureResponsesClient.Parse<TestResponse>("{\"status\":\"completed\",\"output\":[]}")));
Test("AI refusal is rejected", () => Reject(() => AzureResponsesClient.Parse<TestResponse>(
    "{\"status\":\"completed\",\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"refusal\",\"refusal\":\"Refused\"}]}]}")));

Test("90-day classification is recognized", () => Check(LegalArgumentService.FixedReadinessLimit("Misdemeanor90") == 90, "Wrong limit."));
Test("60-day classification is recognized", () => Check(LegalArgumentService.FixedReadinessLimit("Misdemeanor60") == 60, "Wrong limit."));
Test("30-day classification is recognized", () => Check(LegalArgumentService.FixedReadinessLimit("Violation") == 30, "Wrong limit."));
Test("Felony has no invented fixed-day limit", () => Check(LegalArgumentService.FixedReadinessLimit("Felony") == null, "Felony was converted to days."));

Test("Complete canonical discovery response is accepted", () => discoveryService.Validate(Discovery(), ["fictional-packet.pdf"]));
Test("Unknown checklist ID is rejected", () =>
{
    var result = Discovery();
    result.Checklist[0].Id = "invented-item";
    Reject(() => discoveryService.Validate(result, ["fictional-packet.pdf"]));
});
Test("Altered statutory basis is rejected", () =>
{
    var result = Discovery();
    result.Checklist[0].StatutoryBasis = "CPL 999.99";
    Reject(() => discoveryService.Validate(result, ["fictional-packet.pdf"]));
});
Test("Missing checklist entry is rejected", () =>
{
    var result = Discovery();
    result.Checklist.RemoveAt(0);
    Reject(() => discoveryService.Validate(result, ["fictional-packet.pdf"]));
});
Test("Duplicate checklist ID is rejected", () =>
{
    var result = Discovery();
    result.Checklist[0].Id = result.Checklist[1].Id;
    Reject(() => discoveryService.Validate(result, ["fictional-packet.pdf"]));
});
Test("Unreviewed upload is rejected", () =>
{
    var result = Discovery();
    Reject(() => discoveryService.Validate(result, ["fictional-packet.pdf", "second-fictional-packet.pdf"]));
});
Test("Unsupported source filename is rejected", () =>
{
    var result = Discovery();
    result.MissingItems[0].SourceFile = "unprovided-packet.pdf";
    Reject(() => discoveryService.Validate(result, ["fictional-packet.pdf"]));
});
Test("Missing evidence is rejected", () =>
{
    var result = Discovery();
    result.MissingItems[0].EvidenceFound = "";
    Reject(() => discoveryService.Validate(result, ["fictional-packet.pdf"]));
});
Test("Invalid discovery classification is rejected", () =>
{
    var result = Discovery();
    result.Checklist[0].Status = (DiscoveryStatus)999;
    Reject(() => discoveryService.Validate(result, ["fictional-packet.pdf"]));
});

Test("Exact canonical authority is accepted", () => legalService.Validate(Argument(), Case(), Discovery()));
Test("Unknown authority in authority list is rejected", () =>
{
    var result = Argument();
    result.AuthoritiesUsed.Add("People v Invented, 999 NY3d 999 (2026)");
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Unknown case embedded in prose is rejected", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("People v Invented, 999 NY3d 999 (2026) controls.");
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Noncanonical case abbreviation is rejected", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("People v. Bay supports this request.");
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Known case omitted from authority list is rejected", () =>
{
    var result = Argument();
    result.AuthoritiesUsed.Clear();
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Unknown statute in prose is rejected", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("CPL 999.99 authorizes relief.");
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Known statute omitted from authority list is rejected", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("CPL 245.50(5) requires a totality assessment.");
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Known statutory subsections allow ordinary case and spacing", () =>
{
    var result = Argument();
    result.AuthoritiesUsed.Add("CPL 245.50(5)");
    result.PointOneParagraphs.Add("cpl245.50(4)(c)(ii) and Criminal Procedure Law § 245 . 50(6) apply in this fictional test.");
    legalService.Validate(result, Case(), Discovery());
});
Test("Statutory diagnostic identifies an unverified numeric section", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("CPL 999.99 authorizes relief.");
    try { legalService.Validate(result, Case(), Discovery()); throw new Exception("Expected rejection did not occur."); }
    catch (InvalidOperationException error)
    {
        Check(error.Message.Contains("unverified") && error.Message.Contains("CPL 999.99"), "Diagnostic did not identify the unverified section.");
        Check(!error.Message.Contains(Case().DefendantName), "Diagnostic included case information.");
    }
});
Test("Statutory diagnostic identifies a missing authority-list entry", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("CPL 245.50(5) requires a totality assessment.");
    try { legalService.Validate(result, Case(), Discovery()); throw new Exception("Expected rejection did not occur."); }
    catch (InvalidOperationException error)
    {
        Check(error.Message.Contains("CPL 245.50") && error.Message.Contains("did not list"), "Diagnostic did not distinguish an unlisted authority.");
    }
});
Test("One structured repair corrects a validation failure", () =>
{
    var first = Argument();
    first.PointOneParagraphs.Add("CPL 245.50(5) requires a totality assessment.");
    var corrected = Argument();
    corrected.PointOneParagraphs.Add("CPL 245.50(5) requires a totality assessment.");
    corrected.AuthoritiesUsed.Add("CPL 245.50(5)");
    using var handler = new CannedAiHandler([
        CompletedResponse(JsonSerializer.Serialize(first)), CompletedResponse(JsonSerializer.Serialize(corrected))]);
    using var fakeHttp = new HttpClient(handler);
    var service = new LegalArgumentService(new AzureResponsesClient(fakeHttp, TestAiConfiguration()), authorities);
    var result = service.DraftAsync(Case(), Discovery(), CancellationToken.None).GetAwaiter().GetResult();
    Check(handler.Requests.Count == 2, "Expected exactly one repair request.");
    Check(result.AuthoritiesUsed.Contains("CPL 245.50(5)"), "Returned draft was not repaired.");
    using var request = JsonDocument.Parse(handler.Requests[1]);
    var content = request.RootElement.GetProperty("input")[0].GetProperty("content");
    using var originalFacts = JsonDocument.Parse(content[0].GetProperty("text").GetString()!);
    Check(originalFacts.RootElement.GetProperty("CaseInformation").GetProperty("SpeedyTrial").GetProperty("ProsecutionChargeableDays").GetInt32() == 94,
        "Repair changed the supplied chargeable days.");
    Check(content[1].GetProperty("text").GetString() == authorities.AuthorityJson, "Repair did not preserve the closed authority set.");
    using var correction = JsonDocument.Parse(content[2].GetProperty("text").GetString()!);
    Check(correction.RootElement.GetProperty("ValidationError").GetString()!.Contains("CPL 245.50") &&
        correction.RootElement.TryGetProperty("RejectedDraft", out _), "Repair did not include the validation issue and rejected structure.");
    Check(request.RootElement.GetProperty("text").GetProperty("format").GetProperty("schema").GetProperty("properties")
        .GetProperty("AuthoritiesUsed").GetProperty("items").GetProperty("enum").GetArrayLength() == authorities.Citations.Count,
        "Repair lost the closed citation enum.");
});
Test("A second invalid draft fails without a third request", () =>
{
    var invalid = Argument();
    invalid.PointOneParagraphs.Add("CPL 999.99 authorizes relief.");
    using var handler = new CannedAiHandler([
        CompletedResponse(JsonSerializer.Serialize(invalid)), CompletedResponse(JsonSerializer.Serialize(invalid))]);
    using var fakeHttp = new HttpClient(handler);
    var service = new LegalArgumentService(new AzureResponsesClient(fakeHttp, TestAiConfiguration()), authorities);
    Reject(() => service.DraftAsync(Case(), Discovery(), CancellationToken.None).GetAwaiter().GetResult());
    Check(handler.Requests.Count == 2, "Invalid repair was accepted or more than one repair was attempted.");
});
Test("A valid first draft is not repaired", () =>
{
    using var handler = new CannedAiHandler([CompletedResponse(JsonSerializer.Serialize(Argument()))]);
    using var fakeHttp = new HttpClient(handler);
    var service = new LegalArgumentService(new AzureResponsesClient(fakeHttp, TestAiConfiguration()), authorities);
    service.DraftAsync(Case(), Discovery(), CancellationToken.None).GetAwaiter().GetResult();
    Check(handler.Requests.Count == 1, "A valid draft caused an unnecessary repair.");
});
Test("Malformed API output is not treated as a draft-validation repair", () =>
{
    using var handler = new CannedAiHandler(["not JSON"]);
    using var fakeHttp = new HttpClient(handler);
    var service = new LegalArgumentService(new AzureResponsesClient(fakeHttp, TestAiConfiguration()), authorities);
    Reject(() => service.DraftAsync(Case(), Discovery(), CancellationToken.None).GetAwaiter().GetResult());
    Check(handler.Requests.Count == 1, "A transport/parse failure triggered the draft-only repair.");
});
Test("Changed chargeable-day count is rejected", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("The People are chargeable with 95 days.");
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Changed adjectival chargeable-day count is rejected", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("There are 95 prosecution-chargeable days.");
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Changed days-are-chargeable assertion is rejected", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("95 days are chargeable to the People.");
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Changed chargeable-time-total assertion is rejected", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("Chargeable time totals 95 days.");
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Ordinary day-count phrasings accept counsel's unchanged figure", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("94 days are chargeable to the People. Chargeable time totals 94 days. The People are chargeable for a total of 94 days.");
    legalService.Validate(result, Case(), Discovery());
});
Test("Out-of-range chargeable-day assertion is rejected", () =>
{
    var result = Argument();
    result.PointOneParagraphs.Add("999999999999 days are chargeable to the People.");
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("90 days does not exceed the 90-day limit", () =>
{
    Reject(() => legalService.Validate(Argument(90), Case(90), Discovery()));
    legalService.Validate(Argument(90, false), Case(90), Discovery());
});
Test("91 days exceeds the 90-day limit", () => legalService.Validate(Argument(91), Case(91), Discovery()));
Test("Felony dismissal based on a fixed day count is rejected", () =>
{
    var caseInfo = Case(190);
    caseInfo.HighestChargeClassification = "Felony";
    Reject(() => legalService.Validate(Argument(190), caseInfo, Discovery()));
});
Test("Dismissal without invalidation is rejected", () =>
{
    var result = Argument();
    result.SupportsInvalidation = false;
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("Dismissal without supported missing discovery is rejected", () =>
    Reject(() => legalService.Validate(Argument(), Case(), Discovery(false))));
Test("Unsupported second argument point is rejected", () =>
{
    var result = Argument();
    result.SupportsDismissal = false;
    Reject(() => legalService.Validate(result, Case(), Discovery()));
});
Test("No-missing response can request nondismissal relief", () =>
{
    var result = Argument(94, false);
    result.SupportsInvalidation = false;
    legalService.Validate(result, Case(), Discovery(false));
});
Test("Retry clears previous analysis and argument before processing", () =>
{
    var work = new CaseWork { Case = Case(), Discovery = Discovery(), Argument = Argument(), Motion = [1], Error = "Previous error" };
    // A stopped host guarantees this test cannot send an AI request.
    var processor = new CaseProcessor(new TestScopeFactory(), new StoppedTestHost(), NullLogger<CaseProcessor>.Instance);
    Check(processor.Start(work, []), "Retry did not start.");
    Check(work.Discovery == null && work.Argument == null && work.Motion == null, "Previous upload results survived a retry.");
    Check(SpinWait.SpinUntil(() => !work.Running, TimeSpan.FromSeconds(5)), "Cancelled test job did not finish.");
    Check(work.Discovery == null && work.Argument == null, "Failed retry restored stale results.");
});

byte[] motion = documentService.Generate(Case(), Discovery(), Argument());
Test("Generated motion has valid OpenXML", () => WithDocument(motion, document =>
{
    var errors = new OpenXmlValidator().Validate(document).ToList();
    Check(errors.Count == 0, string.Join("; ", errors.Select(x => x.Description)));
}));
Test("Counsel's 94 days are not replaced by 10 calendar days", () => WithDocument(motion, document =>
{
    string text = document.MainDocumentPart!.Document.InnerText;
    Check(text.Contains("The People are chargeable with 94 days"), "Counsel's number was not preserved.");
    Check(!text.Contains("chargeable with 10 days") && !text.Contains("10 chargeable days"), "Calendar dates changed counsel's number.");
}));
Test("Motion sections are in the required order", () => WithDocument(motion, document =>
{
    string text = document.MainDocumentPart!.Document.InnerText;
    InOrder(text, "PLEASE TAKE NOTICE", "Defendant was arraigned", "The following expected discovery",
        "1. Body-worn camera footage", "The fictional conferral record", "POINT I", "Certification requires",
        "POINT II", "Counsel supplies 94 chargeable days", "For this fictional test, dismissal", "Dated:", "Respectfully submitted,");
}));
Test("Original page dimensions and margins are retained", () => WithDocument(motion, document =>
{
    var section = document.MainDocumentPart!.Document.Body!.Elements<SectionProperties>().Single();
    var page = section.GetFirstChild<PageSize>()!;
    var margin = section.GetFirstChild<PageMargin>()!;
    Check(page.Width!.Value == 12240 && page.Height!.Value == 15840, "Page is not Letter size.");
    Check(margin.Top!.Value == 1440 && margin.Bottom!.Value == 1440 && margin.Left!.Value == 1440 && margin.Right!.Value == 1440, "Margins changed.");
    Check(margin.Header!.Value == 708 && margin.Footer!.Value == 708 && margin.Gutter!.Value == 0, "Header/footer layout changed.");
}));
Test("Original font, size, spacing and numbering are retained", () => WithDocument(motion, document =>
{
    var body = document.MainDocumentPart!.Document.Body!;
    foreach (var run in body.Descendants<Run>())
    {
        var properties = run.RunProperties!;
        Check(properties.GetFirstChild<RunFonts>()!.Ascii!.Value == "Times New Roman", "Font changed.");
        Check(properties.GetFirstChild<FontSize>()!.Val!.Value == "24", "Font size changed.");
    }
    foreach (var paragraph in body.Elements<Paragraph>())
    {
        var spacing = paragraph.ParagraphProperties!.GetFirstChild<SpacingBetweenLines>()!;
        Check(spacing.Line!.Value == "264" && spacing.LineRule!.Value == LineSpacingRuleValues.Auto, "Line spacing changed.");
        Check(spacing.After!.Value == (paragraph.InnerText.StartsWith("1. ") ? "80" : "120"), "Paragraph spacing changed.");
    }
    var numbered = body.Elements<Paragraph>().Single(x => x.InnerText.StartsWith("1. "));
    var indent = numbered.ParagraphProperties!.GetFirstChild<Indentation>()!;
    Check(indent.Left!.Value == "720" && indent.Hanging!.Value == "360", "Numbered indentation changed.");
}));
Test("Caption and signature remain in their original positions", () => WithDocument(motion, document =>
{
    var paragraphs = document.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
    Check(paragraphs[0].InnerText == "FICTIONAL CRIMINAL COURT", "Court caption changed.");
    Check(paragraphs[0].ParagraphProperties!.Justification!.Val!.Value == JustificationValues.Center, "Court alignment changed.");
    Check(paragraphs[6].InnerText == "Defendant." && paragraphs[6].ParagraphProperties!.Justification!.Val!.Value == JustificationValues.Right, "Defendant caption changed.");
    Check(paragraphs[^2].InnerText == "Respectfully submitted," && paragraphs[^1].InnerText == "Jordan Fictional Counsel", "Signature changed.");
}));
Test("Legal headings stay with the following paragraphs", () => WithDocument(motion, document =>
{
    var headings = document.MainDocumentPart!.Document.Body!.Elements<Paragraph>().Where(x => x.InnerText.StartsWith("POINT ")).ToList();
    Check(headings.Count == 2 && headings.All(x => x.ParagraphProperties!.GetFirstChild<KeepNext>() != null), "Legal heading pagination was not preserved.");
}));
Test("No-missing motion does not automatically request dismissal", () =>
{
    var argument = Argument(94, false);
    argument.SupportsInvalidation = false;
    byte[] noMissing = documentService.Generate(Case(), Discovery(false), argument);
    WithDocument(noMissing, document =>
    {
        string text = document.MainDocumentPart!.Document.InnerText;
        Check(text.Contains("NOTICE OF MOTION FOR DISCOVERY RELIEF"), "Wrong motion title.");
        Check(text.Contains("does not establish a missing applicable discovery item"), "No-missing condition was not explained.");
        Check(!text.Contains("dismiss", StringComparison.OrdinalIgnoreCase) && !text.Contains("POINT II"), "Automatic dismissal text remained.");
    });
});
Test("DOCX extraction returns the generated text", () => Check(DiscoveryAnalysisService.ExtractDocx(motion).Contains("ALEX FICTIONAL TEST"), "DOCX extraction lost the caption."));
Test("Invalid DOCX is rejected", () =>
{
    bool rejected = false;
    try { DiscoveryAnalysisService.ExtractDocx([1, 2, 3, 4]); }
    catch (Exception error) when (error is InvalidDataException or OpenXmlPackageException or FileFormatException) { rejected = true; }
    Check(rejected, "Invalid DOCX was not rejected.");
});

Console.WriteLine($"\nResults: {passed} passed, {failed} failed. All data was fictional; no Azure requests were made.");
return failed == 0 ? 0 : 1;

void Test(string name, Action action)
{
    try { action(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.Message); }
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Reject(Action action)
{
    bool rejected = false;
    try { action(); }
    catch (InvalidOperationException) { rejected = true; }
    Check(rejected, "Expected rejection did not occur.");
}

static string CompletedResponse(string json) => JsonSerializer.Serialize(new
{
    status = "completed",
    output = new[] { new { type = "message", content = new[] { new { type = "output_text", text = json } } } }
});

static IConfiguration TestAiConfiguration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["AZURE_OPENAI_ENDPOINT"] = "https://unit-test.invalid",
    ["AZURE_OPENAI_API_KEY"] = "fake-unit-test-key",
    ["AZURE_OPENAI_DEPLOYMENT"] = "fake-unit-test-model"
}).Build();

static void WithDocument(byte[] bytes, Action<WordprocessingDocument> inspect)
{
    using var stream = new MemoryStream(bytes);
    using var document = WordprocessingDocument.Open(stream, false);
    inspect(document);
}

static void InOrder(string text, params string[] sections)
{
    int previous = -1;
    foreach (string section in sections)
    {
        int index = text.IndexOf(section, previous + 1, StringComparison.Ordinal);
        Check(index > previous, "Section missing or out of order: " + section);
        previous = index;
    }
}

static CaseInformation Case(int chargeableDays = 94) => new()
{
    DefendantName = "Alex Fictional Test", CaseNumber = "DEMO-TEST-001", Court = "Fictional Criminal Court",
    County = "Fictional County", AttorneyName = "Jordan Fictional Counsel", Charge = "Fictional misdemeanor",
    HighestChargeClassification = "Misdemeanor90", InstrumentType = "LocalCriminalCourt", MotionDate = new DateTime(2026, 2, 1),
    SpeedyTrial = new SpeedyTrialInformation
    {
        ArraignmentDate = new DateTime(2026, 1, 1), CertificateOfComplianceDate = new DateTime(2026, 1, 11),
        ProsecutionChargeableDays = chargeableDays
    }
};

DiscoveryAnalysisResult Discovery(bool missing = true) => new()
{
    DocumentsReviewed = ["fictional-packet.pdf"], Summary = "Fictional automated test packet.",
    ProceduralFindings = "Fictional test facts only, not a representation about an actual case.",
    Checklist = authorities.Checklist.Select(item => new DiscoveryChecklistResult
    {
        Id = item.Id, ItemName = item.ItemName, StatutoryBasis = item.StatutoryBasis,
        Status = missing && item.Id == "bwc" ? DiscoveryStatus.MISSING : DiscoveryStatus.UNDETERMINED,
        SourceFile = "fictional-packet.pdf", PageNumber = 1, EvidenceFound = "The fictional report identifies a recording.",
        ReasonExpected = "The fictional officer reported activating a camera.",
        LegalSignificance = "The referenced recording could document the fictional encounter."
    }).ToList()
};

LegalArgumentResult Argument(int days = 94, bool dismissal = true) => new()
{
    PreliminaryTimelinessArgument = "The fictional conferral record supplies the basis for this test argument.",
    PointOneHeading = "THE CERTIFICATION REQUIRES REVIEW",
    PointOneParagraphs = ["Certification requires a supported analysis under " + bayCitation + "."],
    PointTwoHeading = dismissal ? "THE READINESS PERIOD HAS EXPIRED" : "",
    PointTwoParagraphs = dismissal ? [$"Counsel supplies {days} chargeable days for this fictional test."] : [],
    RequestedRelief = dismissal ? "For this fictional test, dismissal is requested." : "The defense requests only the supported discovery relief.",
    AuthoritiesUsed = [bayCitation], SupportsInvalidation = true, SupportsDismissal = dismissal
};

public class TestResponse
{
    public string Name { get; set; } = "";
}

public class TestEnvironment(string root) : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "DefenseCase.Tests";
    public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = root;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

public class TestScopeFactory : IServiceScopeFactory
{
    public IServiceScope CreateScope() => throw new InvalidOperationException("The cancelled test must not create AI services.");
}

public class StoppedTestHost : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => new(true);
    public CancellationToken ApplicationStopped => new(true);
    public void StopApplication() { }
}

// This handler never contacts the network; it returns only the supplied fictional responses.
public class CannedAiHandler(IEnumerable<string> replies) : HttpMessageHandler
{
    private readonly Queue<string> remaining = new(replies);
    public List<string> Requests { get; } = [];
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        Requests.Add(await request.Content!.ReadAsStringAsync(token));
        if (remaining.Count == 0) throw new InvalidOperationException("Unexpected extra test request.");
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(remaining.Dequeue(), System.Text.Encoding.UTF8, "application/json")
        };
    }
}
