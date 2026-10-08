using DefenseCase.Web.Models;
using DefenseCase.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DefenseCase.Web.Pages;

[RequestSizeLimit(55_000_000)]
public class UploadModel(CaseStore cases, CaseProcessor processor) : PageModel
{
    [BindProperty] public List<IFormFile> Documents { get; set; } = [];
    public CaseWork? Work { get; set; }
    public IActionResult OnGet()
    {
        Work = cases.Get(HttpContext.Session.GetString("caseId"));
        return Work == null ? RedirectToPage("NewCase") : Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        Work = cases.Get(HttpContext.Session.GetString("caseId"));
        if (Work == null) return RedirectToPage("NewCase");
        if (Work.Running) return RedirectToPage("Results");
        if (Documents.Count == 0 || Documents.Sum(x => x.Length) >= 50_000_000)
            ModelState.AddModelError("Documents", "Select at least one PDF or DOCX. Combined size must be below 50 MB.");
        if (Documents.Select(x => Path.GetFileName(x.FileName)).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Documents.Count)
            ModelState.AddModelError("Documents", "Each uploaded file must have a distinct filename.");
        var files = new List<UploadedDocument>();
        if (ModelState.IsValid)
        {
            foreach (var file in Documents)
            {
                string name = Path.GetFileName(file.FileName);
                string extension = Path.GetExtension(name).ToLowerInvariant();
                if (file.Length == 0 || (extension != ".pdf" && extension != ".docx"))
                { ModelState.AddModelError("Documents", "Only nonempty PDF and DOCX files are accepted."); break; }
                using var memory = new MemoryStream();
                await file.CopyToAsync(memory);
                byte[] bytes = memory.ToArray();
                try
                {
                    if (extension == ".pdf" && (bytes.Length < 5 || System.Text.Encoding.ASCII.GetString(bytes, 0, 5) != "%PDF-"))
                        throw new InvalidDataException();
                    if (extension == ".docx") DiscoveryAnalysisService.ExtractDocx(bytes);
                }
                catch
                { ModelState.AddModelError("Documents", "One file is invalid, encrypted, empty or too large to read. Upload readable PDF or DOCX documents."); break; }
                files.Add(new(name, extension == ".pdf" ? "application/pdf" : "application/vnd.openxmlformats-officedocument.wordprocessingml.document", bytes));
            }
        }
        if (!ModelState.IsValid) return Page();
        if (!processor.Start(Work, files))
        { ModelState.AddModelError("Documents", "The demo is busy. Please try again shortly."); return Page(); }
        return RedirectToPage("Results");
    }
}
