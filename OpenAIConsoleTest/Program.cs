using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenAIConsoleTest;
using OpenAILabs.AIServices;
using OpenAILabs.AIServices.Impl;
using OpenAILabs.Common.Config;
using Microsoft.SemanticKernel.Memory;
// check https://github.com/microsoft/AzureDataRetrievalAugmentedGenerationSamples/blob/main/C%23/CosmosDB-NoSQL_CognitiveSearch_SemanticKernel/appsettings.json
// check https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/KernelSyntaxExamples/Example14_SemanticMemory.cs
namespace OpenAiConsole
{
    internal class Program
    {
        private static readonly CancellationTokenSource cts = new();

        static async Task Main(string[] args)
        {
            var builder =
                 Host.CreateDefaultBuilder(args)
                     .ConfigureAppConfiguration(
                         (context, config) =>
                         {
                             config
                                 .AddJsonFile("appsettings.json")
                                 .AddUserSecrets<Program>(true)
                                 .AddEnvironmentVariables();
                         })
                     .ConfigureLogging(
                         (context, config) =>
                         {
                             var appinsight = context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

                             config
                                .ClearProviders()
                                .AddConfiguration(context.Configuration.GetSection("Logging"))
                                .AddConsole();

                             if (!string.IsNullOrEmpty(appinsight))
                             {
                                 // Add OpenTelemetry as a logging provider
                                 config.AddOpenTelemetry(options =>
                                 {
                                     options.AddAzureMonitorLogExporter(options => options.ConnectionString = appinsight);
                                     // Format log messages. This is default to false.
                                     options.IncludeFormattedMessage = true;
                                 });

                             }
                         })
                     .ConfigureServices(
                         (context, services) =>
                         {
                             //logging
                             services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));

                             services.Configure<AzureOpenAIConfiguration>("embedding",context.Configuration.GetSection("OpenAIEmbeddingConfig"));
                             services.Configure<AzureOpenAIConfiguration>("textgeneration",context.Configuration.GetSection("OpenAITextGenerationConfig"));
                             services.Configure<AzureOpenAIConfiguration>("chatcompletition", context.Configuration.GetSection("OpenAIChatCompletitionConfig"));
                             services.Configure<MemoryConfiguration>(context.Configuration.GetSection("MemoryConfig"));
                             services.Configure<StorageAccountConfiguration>("source", context.Configuration.GetSection("StorageAccountSourceConfig"));
                             services.Configure<StorageAccountConfiguration>("destination", context.Configuration.GetSection("StorageAccountDestinationConfig"));
                             services.Configure<DocumentIntelligenceConfiguration>(context.Configuration.GetSection("DocumentIntelligenceConfig"));
                             services.AddTransient<IEmbeddingService, AzureOpenAIEmbeddingService>();
                             services.AddTransient<IMemoryStoreService, AzureOpenAISemanticKernelMemoryStoreService>();
                             services.AddTransient<IDocumentManagementService, DocumentManagementService>();
                             services.AddTransient<IDocumentStorageSourceService, StorageAccountDocumentStorageService>(service =>
                                { 
                                  return new StorageAccountDocumentStorageService(service.GetRequiredService<ILogger<StorageAccountDocumentStorageService>>(), service.GetRequiredService<IOptionsSnapshot<StorageAccountConfiguration>>().Get("source"));
                                }
                             );
                             services.AddTransient<IDocumentStorageDestinationService, StorageAccountDocumentStorageService>(service =>
                             {
                                 return new StorageAccountDocumentStorageService(service.GetRequiredService<ILogger<StorageAccountDocumentStorageService>>(), service.GetRequiredService<IOptionsSnapshot<StorageAccountConfiguration>>().Get("destination"));
                             }
                             );
                             services.AddTransient<IOpenAIChatCompletitionService, AzureOpenAISemanticKernelChatCompletitionService>();
                             services.AddSingleton<IConversationHistoryService, MemoryConversationHistoryService>();
                             //console register

                             services.AddHostedService<ChatCompletitionTestConsole>();
                             //services.AddHostedService<EmbeddingTestConsole>();

                             //services.AddHostedService<IngestDataIntoIndexConsole>();


                         })
                     .UseConsoleLifetime();
            var host = builder.Build();

            var configService = host.Services.GetRequiredService<IConfiguration>();

            Console.CancelKeyPress += (sender, e) =>
            {
                // Segnala il token di annullamento
                cts.Cancel();

                // Impedisci la chiusura dell'applicazione
                e.Cancel = true;
            };

            await host.RunAsync().ConfigureAwait(false);

        }
    }
}
                      
