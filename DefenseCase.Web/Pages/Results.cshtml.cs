using DefenseCase.Web.Models;
using DefenseCase.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DefenseCase.Web.Pages;

public class ResultsModel(CaseStore cases) : PageModel
{
    public CaseWork? Work { get; set; }
    public IActionResult OnGet()
    {
        Response.Headers.CacheControl = "no-store";
        Work = cases.Get(HttpContext.Session.GetString("caseId"));
        return Work == null ? RedirectToPage("NewCase") : Page();
    }
    public IActionResult OnGetDownload()
    {
        var work = cases.Get(HttpContext.Session.GetString("caseId"));
        if (work?.Motion == null) return RedirectToPage("Results");
        string lastName = work.Case.DefendantName.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "Draft";
        string safeName = new(lastName.Where(char.IsLetterOrDigit).Take(60).ToArray());
        Response.Headers.CacheControl = "no-store";
        return File(work.Motion, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "DefenseCase_Motion_" + safeName + ".docx");
    }
}
