using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TollBooth.Options;
using TollBooth.Models;
using Log = TollBooth.DatabaseMethodsLogging;

namespace TollBooth;

public class DatabaseMethods
{
    private readonly ILogger _log;
    private Container _container;

    public DatabaseMethods(ILogger<DatabaseMethods> log, CosmosClient client, IOptionsMonitor<DatabaseOptions> options)
    {
        options.Configure(opt => _container = client.GetContainer(opt.DatabaseId, opt.ContainerId));
        _log = log;
    }

    /// <summary>
    /// Retrieves all license plate records (documents) that have not yet been exported.
    /// </summary>
    /// <returns></returns>
    public async IAsyncEnumerable<LicensePlateDataDocument> GetLicensePlatesToExport([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Log.RetreivingFiles(_log);
        int exportedCount = 0;

        const string queryText = "Replace me with the query text";
        var container = _container;
        using var feed = container.GetItemQueryIterator<LicensePlateDataDocument>(queryText);
        while (feed.HasMoreResults)
        {
            var next = await feed.ReadNextAsync(cancellationToken);
            foreach (var current in next)
            {
                Log.ExportingFile(_log, current.FileName);
                exportedCount++;
                yield return current;
            }
        }
        Log.CompletedExport(_log, exportedCount);

        // using (_client = new DocumentClient(new Uri(_endpointUrl), _authorizationKey))
        // {
        //     // MaxItemCount value tells the document query to retrieve 100 documents at a time until all are returned.
        //     // TODO 5: Retrieve a List of LicensePlateDataDocument objects from the collectionLink where the exported value is false.
        //     // COMPLETE: licensePlates = _client.CreateDocumentQuery ...
        //     // TODO 6: Remove the line below.
        // licensePlates = new List<LicensePlateDataDocument>();
        // }

        // exportedCount = licensePlates.Count();
        // _log.LogInformation($"{exportedCount} license plates found that are ready for export");
        // return licensePlates;
    }

    /// <summary>
    /// Updates license plate records (documents) as exported. Call after successfully
    /// exporting the passed in license plates.
    /// In a production environment, it would be best to create a stored procedure that
    /// bulk updates the set of documents, vastly reducing the number of transactions.
    /// </summary>
    /// <param name="licensePlates"></param>
    /// <returns></returns>
    public async Task MarkLicensePlatesAsExported(IEnumerable<LicensePlateDataDocument> licensePlates, CancellationToken cancellationToken)
    {
        Log.BatchUpdateStarted(_log);

        var container = _container;
        var batch = container.CreateTransactionalBatch(new PartitionKey("fileName"));

        var licensePlateCount = 0;
        foreach (var licensePlate in licensePlates)
        {
            licensePlate.Exported = true;
            batch.UpsertItem(licensePlate);
            licensePlateCount++;
            Log.LicensePlateAdded(_log, licensePlate.FileName);
        }
        await batch.ExecuteAsync(cancellationToken);
        Log.BatchUpdateCompleted(_log, licensePlateCount);
    }
}

/// <summary>
/// This uses compile-time logging source generation <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator"/>
/// </summary>
internal static partial class DatabaseMethodsLogging
{
    [LoggerMessage(-887298395, LogLevel.Debug, "Retrieving license plates to export", EventName = "Database:Export:Starting")]
    internal static partial void RetreivingFiles(ILogger logger);
    [LoggerMessage(1764703243, LogLevel.Debug, "Exporting license plate from file {fileName}", EventName = "Database:Export:Next")]
    internal static partial void ExportingFile(ILogger logger, string fileName);
    [LoggerMessage(-1075309354, LogLevel.Information, "Completed export. {count} license plates exported.", EventName = "Database:Export:Completed")]
    internal static partial void CompletedExport(ILogger logger, int count);
    [LoggerMessage(-941637419, LogLevel.Information, "Updating license plate documents exported values to true.", EventName = "Database:LicensePlateBatch:Started")]
    internal static partial void BatchUpdateStarted(ILogger logger);
    [LoggerMessage(420943086, LogLevel.Debug, "Staging license plate from file {fileName} for batch update.", EventName = "Database:LicensePlate:Added")]
    internal static partial void LicensePlateAdded(ILogger logger, string fileName);
    [LoggerMessage(-573471406, LogLevel.Information, "Completed batch update of {licensePlateCount} license plates.", EventName = "Database:LicensePlateBatch:Updated")]
    internal static partial void BatchUpdateCompleted(ILogger logger, int licensePlateCount);
}