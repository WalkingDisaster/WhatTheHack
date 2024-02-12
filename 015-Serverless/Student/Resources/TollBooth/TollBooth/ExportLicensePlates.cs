using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Log = TollBooth.ExportLicensePlatesLogging;

namespace TollBooth;

public static class ExportLicensePlates
{
    [FunctionName("ExportLicensePlates")]
    public static async Task<HttpResponseMessage> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
        ILogger log,
        DatabaseMethods databaseMethods,
        FileMethods fileMethods,
        CancellationToken cancellationToken)
    {
        Log.Started(log);

        var licensePlates = databaseMethods.GetLicensePlatesToExport(cancellationToken);
        var hasPlates = await licensePlates.AnyAsync();
        if (!hasPlates)
        {
            Log.NoPlates(log);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        var plates = await licensePlates.ToArrayAsync();
        Log.ExportingPlates(log, plates.Length);
        var uploaded = await fileMethods.GenerateAndSaveCsv(plates, cancellationToken);
        if (uploaded)
        {
            await databaseMethods.MarkLicensePlatesAsExported(plates, cancellationToken);
            Log.UpdatedExportFlag(log);
        }
        else
        {
            Log.NotUploaded(log);
        }

        Log.ExportCompleted(log, plates.Length);

        return req.CreateResponse(HttpStatusCode.OK, $"Exported {plates.Length} license plates");
    }
}

/// <summary>
/// This uses compile-time logging source generation <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator"/>
/// </summary>
internal static partial class ExportLicensePlatesLogging
{
    [LoggerMessage(889936917, LogLevel.Information, "Finding license plate data to export.", EventName = "ExportLicensePlates:Started")]
    internal static partial void Started(ILogger logger);
    [LoggerMessage(1410786351, LogLevel.Warning, "No license plates to export.", EventName = "ExportLicensePlates:NoPlates")]
    internal static partial void NoPlates(ILogger logger);
    [LoggerMessage(421010592, LogLevel.Information, "Exporting {plateCount} license plates.", EventName = "ExportLicensePlates:Exported")]
    internal static partial void ExportingPlates(ILogger logger, int plateCount);
    [LoggerMessage(-1889503968, LogLevel.Information, "Finished updating the license plates.", EventName = "ExportLicensePlates:DatabaseUpdated")]
    internal static partial void UpdatedExportFlag(ILogger logger);
    [LoggerMessage(502963595, LogLevel.Debug, "Export file could not be uploaded. Skipping database update that marks the documents as exported.", EventName = "ExportLicensePlates:Skipped")]
    internal static partial void NotUploaded(ILogger logger);
    [LoggerMessage(-1856450250, LogLevel.Information, "Exported {plateCount} license plates.", EventName = "ExportLicensePlates:Completed")]
    internal static partial void ExportCompleted(ILogger logger, int plateCount);
}