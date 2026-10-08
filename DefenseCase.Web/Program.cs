using DefenseCase.Web.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(45);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 52_428_800);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 55_000_000);
builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = 55_000_000);
builder.Services.AddSingleton<AuthorityData>();
builder.Services.AddSingleton<CaseStore>();
builder.Services.AddSingleton<CaseProcessor>();
builder.Services.AddHttpClient<AzureResponsesClient>(client => client.Timeout = TimeSpan.FromMinutes(10));
builder.Services.AddScoped<DiscoveryAnalysisService>();
builder.Services.AddScoped<LegalArgumentService>();
builder.Services.AddSingleton<DocumentGenerationService>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();
app.Run();
