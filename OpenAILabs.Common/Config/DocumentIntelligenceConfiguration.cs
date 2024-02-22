using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenAILabs.Common.Config
{
    public class DocumentIntelligenceConfiguration
    {
        public string Endpoint { get; set; }
        public string AccountKey { get; set; }
        public DocumentIntelligenceAuthenticationTypeEnum AuthenticationType { get; set; }

    }

    public enum DocumentIntelligenceAuthenticationTypeEnum
    {
        AccountKey,
        AAD,
        Unknown = -1
    }

}
