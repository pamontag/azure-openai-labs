using Microsoft.Extensions.Hosting;
using OpenAILabs.AIServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAIConsoleTest
{
    public class IngestDataIntoIndexConsole : IHostedService
    {
        readonly IEmbeddingService _embeddingService;
        readonly IHostApplicationLifetime _lifeTime;
        readonly IDocumentStorageSourceService _sourceDocumentStorageService;
        readonly IDocumentStorageDestinationService _destinationDocumentStorageService;
        readonly IDocumentManagementService _documentManagementService;
        readonly IMemoryStoreService _memoryStoreService;

        public IngestDataIntoIndexConsole(IEmbeddingService embeddingService,
            IDocumentStorageSourceService sourceDocumentStorageService,
            IDocumentStorageDestinationService destinationDocumentStorageService,
            IDocumentManagementService documentManagementService,
            IMemoryStoreService memoryStoreService,
            IHostApplicationLifetime lifeTime)
        {
            ArgumentNullException.ThrowIfNull(lifeTime, nameof(lifeTime));
            ArgumentNullException.ThrowIfNull(embeddingService, nameof(embeddingService));
            ArgumentNullException.ThrowIfNull(sourceDocumentStorageService, nameof(embeddingService));
            ArgumentNullException.ThrowIfNull(destinationDocumentStorageService, nameof(embeddingService));
            ArgumentNullException.ThrowIfNull(documentManagementService, nameof(embeddingService));
            ArgumentNullException.ThrowIfNull(memoryStoreService, nameof(memoryStoreService));
            _embeddingService = embeddingService;
            _sourceDocumentStorageService = sourceDocumentStorageService;
            _destinationDocumentStorageService = destinationDocumentStorageService;
            _documentManagementService = documentManagementService;
            _memoryStoreService = memoryStoreService;
            _lifeTime = lifeTime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(() => this.ExecuteAsync(cancellationToken), cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {

                Console.WriteLine("Starting Ingest Data Into Index Test");
                var documents = await _sourceDocumentStorageService.GetDocumentsAsync();
                foreach (var document in documents)
                {
                    Console.WriteLine($"Processing document: {document}");
                    Console.WriteLine($"Getting document bytes for: {document}");
                    var documentBytes = await _sourceDocumentStorageService.GetDocumentBytesAsync(document);
                    Console.WriteLine($"Splitting document: {document}");
                    var splitDocuments = _documentManagementService.SplitPDFDocument(documentBytes, Path.GetFileNameWithoutExtension(document));
                    foreach (var splitDocument in splitDocuments)
                    {
                        Console.WriteLine($"Analyzing document: {splitDocument.Key}");
                        var sentences = await _documentManagementService.AnalyzeDocumentAsync(document, splitDocument.Value);
                        Console.WriteLine($"Storing document: {splitDocument.Key}");
                        await _destinationDocumentStorageService.StoreDocumentAsync(splitDocument.Key, splitDocument.Value);
                        foreach (var sentence in sentences)
                        {
                            if (!sentence.Contains("UNKNOWN"))
                            {

                                Dictionary<string, object> metadata = new()
                                {
                                    { "title", Path.GetFileNameWithoutExtension(document) },
                                    { "filename", splitDocument.Key }
                                };
                                var result = await _memoryStoreService.StoreMemoryAsync(sentence, metadata);
                            }
                        }
                    }
                }

                Console.WriteLine("End");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                _lifeTime.StopApplication();
            }

        }
    }
}
