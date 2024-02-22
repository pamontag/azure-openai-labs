using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAILabs.Common.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAILabs.AIServices.Impl
{
    public class StorageAccountDocumentStorageService : IDocumentStorageSourceService, IDocumentStorageDestinationService
    {
        private BlobServiceClient _blobServiceClient;
        private BlobContainerClient _blobContainerClient;
        private StorageAccountConfiguration _configuration;
        private Task _initializationTask;
        private readonly ILogger _logger;
        private const int SEGMENT_SIZE = 100;

        public StorageAccountDocumentStorageService(ILogger<StorageAccountDocumentStorageService> logger, StorageAccountConfiguration configuration)
        {
            _configuration = configuration;
            _logger = logger;
            _blobServiceClient = GetBlobServiceClient();
            _initializationTask = GetorCreateContainerAsync(_configuration.ContainerName);
        }

        private BlobServiceClient GetBlobServiceClient()
        {
            ArgumentNullException.ThrowIfNull(_configuration.AuthenticationType, nameof(_configuration.AuthenticationType));

            switch (_configuration.AuthenticationType)
            {
                case StorageAccountAuthenticationTypeEnum.AccountKey:
                    return new BlobServiceClient(new Uri($"https://{_configuration.AccountName}.blob.core.windows.net"), new Azure.Storage.StorageSharedKeyCredential(_configuration.AccountName, _configuration.AccountKey));
                case StorageAccountAuthenticationTypeEnum.ConnectionString:
                    return new BlobServiceClient(_configuration.ConnectionString);
                case StorageAccountAuthenticationTypeEnum.SAS:
                    return new BlobServiceClient(new Uri($"https://{_configuration.AccountName}.blob.core.windows.net?{_configuration.SASToken}"));
                case StorageAccountAuthenticationTypeEnum.AAD:
                    return new BlobServiceClient(new Uri($"https://{_configuration.AccountName}.blob.core.windows.net"), new Azure.Identity.DefaultAzureCredential());
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task GetorCreateContainerAsync(string containerName)
        {

            _blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            if (await _blobContainerClient.ExistsAsync())
            {
                _logger.LogInformation("Container exist: {0}", containerName); 
            } 
            else
            {
                _blobContainerClient = await _blobServiceClient.CreateBlobContainerAsync(containerName);
                if (await _blobContainerClient.ExistsAsync())
                {
                    _logger.LogInformation("Created container {0}", containerName);                     
                }
            }
        }

        public async Task StoreDocumentAsync(string documentName , string documentData)
        {
            await StoreDocumentAsync(documentName, BinaryData.FromString(documentData));
        }

        public async Task StoreDocumentAsync(string documentName, BinaryData documentData)
        {
            try
            {
                if (await _blobContainerClient.GetBlobClient(documentName).ExistsAsync())
                {
                    _logger.LogWarning("Blob already exists: {0}", documentName);
                } else 
                    await _blobContainerClient.UploadBlobAsync(documentName, documentData);
            } 
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error storing document");
                throw new Exception("Error storing document", ex);
            }

        }

        public async Task<string> GetDocumentStringAsync(string blobName)
        {
            BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
            BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();
            return downloadResult.Content.ToString();
        }

        public async Task<BinaryData> GetDocumentBytesAsync(string blobName)
        {
            BlobClient blobClient = _blobContainerClient.GetBlobClient(blobName);
            byte[] result = null;
            using(var stream = new MemoryStream())
            {
                await blobClient.DownloadToAsync(stream);
                result = stream.ToArray();
            }
            return BinaryData.FromBytes(result);
        }

        public async Task<List<String>> GetDocumentsAsync()
        {
            List<string> documents = new List<string>();

            var resultSegment = _blobContainerClient.GetBlobsAsync()
            .AsPages(default, SEGMENT_SIZE);

            // Enumerate the blobs returned for each page.
            await foreach (Page<BlobItem> blobPage in resultSegment)
            {
                foreach (BlobItem blobItem in blobPage.Values)
                {
                    documents.Add(blobItem.Name);
                    _logger.LogInformation("Blob name: {0}", blobItem.Name);
                }
            }

            return documents;
        }

    }
}
