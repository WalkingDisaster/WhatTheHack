using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TollBooth.Options;
using TollBooth.Clients;
using Microsoft.Extensions.Azure;
using System;

[assembly: FunctionsStartup(typeof(TollBooth.Startup))]

namespace TollBooth;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        // Options
        builder.Services
            .AddOptions<EventGridTopicOptions>()
            .Configure<IConfiguration>((opt, cfg) =>
            {
                cfg.GetSection(EventGridTopicOptions.Section)
                    .Bind(opt);
            })
            .ValidateOnStart();
        builder.Services
            .AddOptions<DatabaseOptions>()
            .Configure<IConfiguration>((opt, cfg) =>
            {
                cfg.GetSection(DatabaseOptions.Section)
                    .Bind(opt);
            })
            .ValidateOnStart();

        // HTTP
        builder.Services.AddHttpClient();
        builder.Services
            .AddHttpClient<EventGridEventClient>((sp, client) =>
            {
                var options = sp.GetService<IOptions<EventGridTopicOptions>>().Value;
                client.DefaultRequestHeaders.Add("aeg-sas-key", options.AccessKey);
                client.BaseAddress = new Uri(options.AccountEndpoint);
            });

        // Cosmos DB
        builder.Services.AddSingleton<TokenCredential, DefaultAzureCredential>();
        builder.Services.AddSingleton(() => new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        });
        builder.Services.AddSingleton(svc => new CosmosClient(
            svc.GetService<IOptions<DatabaseOptions>>().Value.AccountEndpoint,
            svc.GetService<TokenCredential>(),
            svc.GetService<CosmosClientOptions>()));

        // Utility Services
        builder.Services.AddSingleton<DatabaseMethods>();
        builder.Services.AddSingleton<FileMethods>();
        builder.Services.AddSingleton<SendToEventGrid>();
    }
}