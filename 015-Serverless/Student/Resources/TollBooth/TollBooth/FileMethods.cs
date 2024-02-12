using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using TollBooth.Models;
using Log = TollBooth.FileMethodsLogging;

namespace TollBooth;

public class FileMethods
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly string _containerName = Environment.GetEnvironmentVariable("exportCsvContainerName");
    private readonly string _blobStorageConnection = Environment.GetEnvironmentVariable("blobStorageConnection");
    private readonly ILogger _log;

    public FileMethods(BlobContainerClient blobContainerClient, ILogger<FileMethods> log)
    {
        _log = log;
        _blobContainerClient = blobContainerClient;
    }

    public async Task<bool> GenerateAndSaveCsv(IEnumerable<LicensePlateDataDocument> licensePlates, CancellationToken cancellationToken)
    {
        Log.Starting(_log);
        string blobName = $"{DateTime.UtcNow:s}.csv";

        using var stream = new MemoryStream();
        using var textWriter = new StreamWriter(stream);
        using var csv = new CsvWriter(textWriter, new CsvConfiguration(CultureInfo.CurrentCulture)
        {
            Delimiter = ","
        });
        csv.WriteRecords(licensePlates.Select(ToLicensePlateData));
        await textWriter.FlushAsync();

        Log.BeginningUpload(_log, blobName);
        try
        {
            // Retrieve reference to a blob.
            //await container.CreateIfNotExistsAsync(); // todo do this in the startup
            var blobClient = _blobContainerClient.GetBlobClient(blobName);

            // Upload blob.
            stream.Seek(0, SeekOrigin.Begin);
            // TODO 7: Asyncronously upload the blob from the memory stream.
            // COMPLETE: await blob...;
            Log.CompletedUpload(_log, blobName);
        }
        catch (Exception e)
        {
            Log.CouldNotUpload(_log, e);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Used for mapping from a LicensePlateDataDocument object to a LicensePlateData object.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    private static LicensePlateData ToLicensePlateData(LicensePlateDataDocument source)
    {
        return new LicensePlateData
        {
            FileName = source.FileName,
            LicensePlateText = source.LicensePlateText,
            TimeStamp = source.Timestamp
        };
    }
}

/// <summary>
/// This uses compile-time logging source generation <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator"/>
/// </summary>
internal static partial class FileMethodsLogging
{
    [LoggerMessage(1754212027, LogLevel.Information, "Generating CSV file.", EventName = "FileMethods:Starting")]
    internal static partial void Starting(ILogger log);
    [LoggerMessage(-74519488, LogLevel.Debug, "Beginning file upload: {blobName}.", EventName = "FileMethods:BeginningUpload")]
    internal static partial void BeginningUpload(ILogger log, string blobName);
    [LoggerMessage(369768087, LogLevel.Debug, "Completed file upload: {blobName}.", EventName = "FileMethods:CompletedUpload")]
    internal static partial void CompletedUpload(ILogger log, string blobName);
    [LoggerMessage(-1568592544, LogLevel.Error, "Could not upload CSV file.", EventName = "FileMethods:Error")]
    internal static partial void CouldNotUpload(ILogger log, Exception e);
}