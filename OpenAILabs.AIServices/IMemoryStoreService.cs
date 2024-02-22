using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAILabs.AIServices
{
    public interface IMemoryStoreService
    {
        Task<string> StoreMemoryAsync(string text, Dictionary<string, object> metadata);

        Task<List<string>> SearchMemoryAsync(string query);
    }
}
