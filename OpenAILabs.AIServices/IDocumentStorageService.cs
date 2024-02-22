using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAILabs.AIServices
{
    public interface IDocumentStorageService
    {
        Task<string> GetDocumentStringAsync(string blobName);
        Task<BinaryData> GetDocumentBytesAsync(string blobName);
        Task<List<String>> GetDocumentsAsync();
        Task StoreDocumentAsync(string documentName, string documentData);
        Task StoreDocumentAsync(string documentName, BinaryData documentData);
    }

    public interface IDocumentStorageSourceService : IDocumentStorageService
    {
    }

    public interface IDocumentStorageDestinationService : IDocumentStorageService
    {
    }
}
