using DefenseCase.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace DefenseCase.Web.Services;

// Short-lived state for this one-instance demo. Nothing is saved in a database.
public class CaseStore(IMemoryCache cache)
{
    public string Create(CaseInformation information)
    {
        string id = Guid.NewGuid().ToString("N");
        cache.Set(id, new CaseWork { Case = information }, TimeSpan.FromMinutes(45));
        return id;
    }
    public CaseWork? Get(string? id) => id == null ? null : cache.Get<CaseWork>(id);
}

// Keep long AI calls off the upload request so App Service can return promptly.
public class CaseProcessor(IServiceScopeFactory scopes, IHostApplicationLifetime lifetime, ILogger<CaseProcessor> logger)
{
    private readonly SemaphoreSlim queue = new(1, 1);
    private int pending;

    public bool Start(CaseWork work, List<UploadedDocument> files)
    {
        if (Interlocked.Increment(ref pending) > 3)
        {
            Interlocked.Decrement(ref pending);
            return false;
        }
        lock (work)
        {
            if (work.Running) { Interlocked.Decrement(ref pending); return false; }
            work.Running = true;
            work.Error = null;
            work.Motion = null;
            work.Discovery = null;
            work.Argument = null;
            work.Status = "Waiting to analyze discovery";
        }
        _ = Task.Run(async () =>
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping);
            timeout.CancelAfter(TimeSpan.FromMinutes(15));
            bool entered = false;
            try
            {
                await queue.WaitAsync(timeout.Token);
                entered = true;
                using var scope = scopes.CreateScope();
                work.Status = "Analyzing the uploaded discovery";
                var discovery = await scope.ServiceProvider.GetRequiredService<DiscoveryAnalysisService>().AnalyzeAsync(work.Case, files, timeout.Token);
                work.Discovery = discovery;
                work.Status = "Drafting the case-specific legal argument";
                var argument = await scope.ServiceProvider.GetRequiredService<LegalArgumentService>().DraftAsync(work.Case, discovery, timeout.Token);
                work.Argument = argument;
                work.Status = "Generating the Word motion";
                work.Motion = scope.ServiceProvider.GetRequiredService<DocumentGenerationService>().Generate(work.Case, discovery, argument);
                work.Status = "MOTION READY";
            }
            catch (Exception error)
            {
                // Do not log client documents, case facts, API keys or raw model output.
                logger.LogWarning("Motion generation stopped: {ErrorType}", error.GetType().Name);
                work.Error = error is InvalidOperationException ? error.Message : error is OperationCanceledException
                    ? "Processing timed out or the app restarted. Please retry with a smaller packet."
                    : "The packet could not be processed. Check that the files are readable, unencrypted PDF or DOCX documents and retry.";
                work.Status = "Processing stopped";
            }
            finally
            {
                files.Clear();
                work.Running = false;
                if (entered) queue.Release();
                Interlocked.Decrement(ref pending);
            }
        });
        return true;
    }
}
