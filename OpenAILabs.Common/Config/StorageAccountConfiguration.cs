using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAILabs.Common.Config
{
    public class StorageAccountConfiguration
    {
        public string AccountName { get; set; }
        public string AccountKey { get; set; }
        public string SASToken { get; set; }

        public string ConnectionString { get; set; }

        public string ContainerName { get; set; }

        public StorageAccountAuthenticationTypeEnum AuthenticationType { get; set; }

    }

    public enum StorageAccountAuthenticationTypeEnum
    {
        AccountKey,
        ConnectionString,
        SAS,
        AAD,
        Unknown = -1
    }
}
