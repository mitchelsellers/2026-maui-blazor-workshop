using Microsoft.Extensions.Logging;

namespace FieldOps.Mobile.Services;

public sealed class WorkshopDiagnostics(ILogger<WorkshopDiagnostics> logger)
{
    public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await action();
            logger.LogInformation("Operation {Operation} completed in {ElapsedMs} ms", operation, timer.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Operation {Operation} failed after {ElapsedMs} ms", operation, timer.ElapsedMilliseconds);
            throw;
        }
    }
}
