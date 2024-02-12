using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TollBooth.Clients;
using TollBooth.Models;

namespace TollBooth
{
    public class SendToEventGrid
    {
        private readonly EventGridEventClient _client;
        private readonly ILogger _log;

        public SendToEventGrid(EventGridEventClient client, ILogger<SendToEventGrid> log)
        {
            _log = log;
            _client = client;
        }

        public async Task SendLicensePlateData(LicensePlateData data, CancellationToken cancellationToken)
        {
            // TODO 3: Remove the line below
            await Task.CompletedTask;

            // Will send to one of two routes, depending on success.
            // Event listeners will filter and act on events they need to
            // process (save to database, move to manual checkup queue, etc.)
            if (data.LicensePlateFound)
            {
                // TODO 3: Modify send method to include the proper eventType name value for saving plate data.
                // COMPLETE: await Send(...);
            }
            else
            {
                // TODO 4: Modify send method to include the proper eventType name value for queuing plate for manual review.
                // COMPLETE: await Send(...);
            }
        }

        private async Task Send(string eventType, string subject, LicensePlateData data, CancellationToken cancellationToken)
        {
            _log.LogInformation($"Sending license plate data to the {eventType} Event Grid type");
            var result = await _client.SendEventAsync(subject, eventType, data, cancellationToken);
            _log.LogInformation($"Sent the following to the Event Grid topic: {result}");
        }
    }
}
