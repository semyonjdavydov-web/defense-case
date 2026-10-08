using DefenseCase.Web.Models;
using DefenseCase.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DefenseCase.Web.Pages;

public class NewCaseModel(CaseStore cases) : PageModel
{
    [BindProperty] public CaseInformation Input { get; set; } = new();
    public void OnGet() { }
    public IActionResult OnPost()
    {
        if (!ModelState.IsValid) return Page();
        string id = cases.Create(Input);
        HttpContext.Session.SetString("caseId", id);
        return RedirectToPage("Upload");
    }
}
