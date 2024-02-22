using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAILabs.AIServices
{
    public interface IDocumentManagementService
    {
        Dictionary<string, BinaryData> SplitPDFDocument(BinaryData document, string documentname);

        Task<List<string>> AnalyzeDocumentAsync(string documentName, BinaryData document);
    }
}
