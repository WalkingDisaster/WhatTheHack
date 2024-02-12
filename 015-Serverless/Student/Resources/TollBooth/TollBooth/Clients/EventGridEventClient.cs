using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TollBooth.Models;

namespace TollBooth.Clients;

public class EventGridEventClient
{
    private readonly HttpClient _client;
    private readonly ILogger _log;

    public EventGridEventClient(HttpClient client, ILogger<EventGridEventClient> log) =>
    (_client, _log) = (client, log);

    public async Task<Event<T>> SendEventAsync<T>(string subject, string eventType, T eventData, CancellationToken cancellationToken) where T : class
    {
        EventGridEventClientLogging.SendingMessage(_log, subject, eventType);
        var theEvent = new Event<T>
        {
            Data = eventData,
            EventTime = DateTime.UtcNow,
            EventType = eventType,
            Id = Guid.NewGuid().ToString(),
            Subject = subject
        };
        var result = await _client.PostAsJsonAsync(string.Empty, new[] { theEvent });
        result.EnsureSuccessStatusCode();
        EventGridEventClientLogging.SentMessage(_log, subject, eventType);
        return theEvent;
    }
}

internal static partial class EventGridEventClientLogging
{
    [LoggerMessage(932388740, LogLevel.Debug, "Sending message with subject {subject} for event type {eventType}", EventName = "Event:Sending")]
    public static partial void SendingMessage(ILogger logger, string subject, string eventType);
    [LoggerMessage(621525703, LogLevel.Information, "Sent message with subject {subject} for event type {eventType}", EventName = "Event:Sent")]
    public static partial void SentMessage(ILogger logger, string subject, string eventType);
}