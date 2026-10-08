using System.Text.Json.Nodes;

namespace DefenseCase.Web.Services;

// All fields are required in the AI response, even when their value is empty.
public static class ResponseSchemas
{
    static JsonObject Text() => new() { ["type"] = "string" };
    static JsonObject Flag() => new() { ["type"] = "boolean" };
    static JsonObject List(JsonObject item) => new() { ["type"] = "array", ["items"] = item };
    static JsonObject Object(Dictionary<string, JsonObject> fields) => new()
    {
        ["type"] = "object", ["additionalProperties"] = false,
        ["properties"] = new JsonObject(fields.Select(x => KeyValuePair.Create<string, JsonNode?>(x.Key, x.Value))),
        ["required"] = new JsonArray(fields.Keys.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray())
    };
    public static JsonObject Discovery() => Object(new()
    {
        ["DocumentsReviewed"] = List(Text()), ["Summary"] = Text(), ["ProceduralFindings"] = Text(),
        ["Checklist"] = List(Object(new()
        {
            ["Id"] = Text(), ["ItemName"] = Text(), ["StatutoryBasis"] = Text(),
            ["Status"] = new() { ["type"] = "string", ["enum"] = new JsonArray("PRESENT", "MISSING", "NOT_APPLICABLE", "UNDETERMINED") },
            ["ReasonExpected"] = Text(), ["EvidenceFound"] = Text(), ["SourceFile"] = Text(),
            ["PageNumber"] = new() { ["type"] = new JsonArray("integer", "null") }, ["LegalSignificance"] = Text()
        }))
    });
    public static JsonObject Legal() => Object(new()
    {
        ["PreliminaryTimelinessArgument"] = Text(), ["PointOneHeading"] = Text(), ["PointOneParagraphs"] = List(Text()),
        ["PointTwoHeading"] = Text(), ["PointTwoParagraphs"] = List(Text()), ["RequestedRelief"] = Text(),
        ["AuthoritiesUsed"] = List(Text()), ["SupportsInvalidation"] = Flag(), ["SupportsDismissal"] = Flag()
    });
}
