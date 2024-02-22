using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAILabs.Common.Config
{
    public class MemoryConfiguration
    {       

        public MemoryStoreTypeEnum MemoryStoreType { get; set; }    
        public string CollectionName { get; set; }
        public int Limit { get; set; }
        public float MinRelevanceScore { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
    }

    public enum MemoryStoreTypeEnum
    {
        Unknown = -1,
        Volatile,
        AzureAISearch,
        CosmosDB,
        Postgres
    }
}
