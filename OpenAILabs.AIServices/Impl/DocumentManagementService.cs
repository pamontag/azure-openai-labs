using PdfSharp.Pdf.IO;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using OpenAILabs.Common.Config;
using Azure.AI.DocumentIntelligence;
using Azure;
using System.Net;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using SharpToken;
using System.ComponentModel;

namespace OpenAILabs.AIServices.Impl
{
    public class DocumentManagementService : IDocumentManagementService
    {
        private readonly ILogger _logger;
        private DocumentIntelligenceConfiguration _configuration;
        private DocumentIntelligenceClient _documentIntelligenceClient;
        private GptEncoding _encoding;
        private IOpenAIChatCompletitionService _textGenerationService;
        private const int TOKEN_LIMIT = 256;
        private const string ENCODING_NAME = "cl100k_base";
        public DocumentManagementService(
            ILogger<DocumentManagementService> logger, 
            IOptions<DocumentIntelligenceConfiguration> configuration, 
            IOpenAIChatCompletitionService textGenerationService)
        {
            _logger = logger;
            _configuration = configuration.Value;
            _documentIntelligenceClient = GetDocumentIntelligenceClient();
            _encoding = GptEncoding.GetEncoding(ENCODING_NAME);
            _textGenerationService = textGenerationService;
        }
        private DocumentIntelligenceClient GetDocumentIntelligenceClient()
        {
            ArgumentNullException.ThrowIfNull(_configuration.AuthenticationType, nameof(_configuration.AuthenticationType));

            switch (_configuration.AuthenticationType)
            {
                case DocumentIntelligenceAuthenticationTypeEnum.AccountKey:
                    return new DocumentIntelligenceClient(new Uri(_configuration.Endpoint), new AzureKeyCredential(_configuration.AccountKey));
                case DocumentIntelligenceAuthenticationTypeEnum.AAD:
                    return new DocumentIntelligenceClient(new Uri(_configuration.Endpoint), new Azure.Identity.DefaultAzureCredential());
                default:
                    throw new NotImplementedException();
            }
        }

        public async Task<List<string>> AnalyzeDocumentAsync(string documentName, BinaryData document)
        {

            try
            {
                var content = new AnalyzeDocumentContent()
                {
                    Base64Source = document
                };

                Operation<AnalyzeResult> operation = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", content);
                AnalyzeResult result = operation.Value;

                documentName = Path.GetFileNameWithoutExtension(documentName);
                documentName = RеmovеSpеcialCharactеrs(documentName);

                _logger.LogInformation("Analyze paragraphs...");

                var sentences = new List<string>();
                string cleanSentence = string.Empty;    
                string sentenceTotal = string.Empty;
                int tokenCount = 0;

                for (int i = 0; i < result.Paragraphs.Count; i++)
                {
                    

                    DocumentParagraph paragraph = result.Paragraphs[i];

                    _logger.LogDebug($"Paragraph {i}:");
                    _logger.LogDebug($"  Content: {paragraph.Content}");

                    if (paragraph.Role != null)
                    {
                        _logger.LogDebug($"  Role: {paragraph.Role}");
                    }
                    var sentence = paragraph.Content;
                    sentence = sentence.TrimEnd('\r', '\n');
                    sentence = RеmovеSpеcialCharactеrs(sentence);
                    int token = CountToken(sentence);   
                    if(tokenCount +  token > TOKEN_LIMIT) 
                    {
                        _logger.LogInformation($"Sentence: {sentenceTotal}");
                        cleanSentence = await AskGptForCleanDataAsync(sentenceTotal, documentName);
                        _logger.LogInformation($"Clean Sentence: {cleanSentence}");
                        sentences.Add(cleanSentence);

                        sentenceTotal = sentence;
                        tokenCount = token;
                    } 
                    else
                    {
                        tokenCount += token;
                        sentenceTotal += " " + sentence;
                    }


                }

                _logger.LogInformation($"Sentence: {sentenceTotal}");
                cleanSentence = await AskGptForCleanDataAsync(sentenceTotal, documentName);
                _logger.LogInformation($"Clean Sentence: {cleanSentence}");
                sentences.Add(cleanSentence);

                return sentences;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing PDF document");
                throw new Exception("Error splitting PDF document", ex);
            }

        }

        public Dictionary<string, BinaryData> SplitPDFDocument(BinaryData document, string documentname)
        {

            try
            {
                Dictionary<string, BinaryData> pages = new Dictionary<string, BinaryData>();
                // Open the file
                PdfDocument inputDocument = PdfReader.Open(document.ToStream(), PdfDocumentOpenMode.Import);

                for (int idx = 0; idx < inputDocument.PageCount; idx++)
                {
                    // Create new document
                    PdfDocument outputDocument = new PdfDocument();
                    outputDocument.Version = inputDocument.Version;
                    outputDocument.Info.Title =
                      String.Format("Page {0} of {1}", idx + 1, inputDocument.Info.Title);
                    outputDocument.Info.Creator = inputDocument.Info.Creator;

                    // Add the page and save it
                    outputDocument.AddPage(inputDocument.Pages[idx]);
                    using (var stream = new MemoryStream())
                    {
                        outputDocument.Save(stream);
                        pages.Add($"{documentname}_{idx + 1}.pdf", new BinaryData(stream.ToArray()));
                    }
                }
                return pages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error splitting PDF document");
                throw new Exception("Error splitting PDF document", ex);
            }
        }

        private async Task<string> AskGptForCleanDataAsync(string input, string documentName)
        {
            return await _textGenerationService.CleanTextAsync(input, documentName);
        }

        private int CountToken(string input)
        {
            return _encoding.Encode(input).Count();
        }

        private string RеmovеSpеcialCharactеrs(string input)
        {
            // Usе a rеgular еxprеssion to rеplacе non-alphanumеric charactеrs with an еmpty string
            return Regex.Replace(input, @"[^0-9a-zA-Z -\+\.]", "");
        }
    }
}
