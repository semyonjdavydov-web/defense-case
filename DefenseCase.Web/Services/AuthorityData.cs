using System.Text.Json;
using DefenseCase.Web.Models;

namespace DefenseCase.Web.Services;

// A small, closed authority set. No web search is performed at runtime.
public class AuthorityData
{
    public string AuthorityJson { get; }
    public List<ChecklistItem> Checklist { get; }
    public HashSet<string> Citations { get; } = new(StringComparer.Ordinal);

    public AuthorityData(IWebHostEnvironment environment)
    {
        string folder = Path.Combine(environment.ContentRootPath, "LegalAuthorities");
        AuthorityJson = File.ReadAllText(Path.Combine(folder, "NYArticle245Authorities.json"));
        Checklist = JsonSerializer.Deserialize<List<ChecklistItem>>(File.ReadAllText(Path.Combine(folder, "DiscoveryChecklist.json")))!;
        using var data = JsonDocument.Parse(AuthorityJson);
        foreach (var section in new[] { "Statutes", "Cases" })
            foreach (var item in data.RootElement.GetProperty(section).EnumerateArray())
                Citations.Add(item.GetProperty("Citation").GetString()!);
    }
}
