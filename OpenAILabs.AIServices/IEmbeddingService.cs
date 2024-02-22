using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAILabs.AIServices
{
    public interface IEmbeddingService
    {
        float[] GetEmbeddings(string text);

        Task<float[]> GetEmbeddingsAsync(string text);
    }
}
