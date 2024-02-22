using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using OpenAILabs.AIServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAILabs.Common.Model;
using Humanizer;
using Azure.Search.Documents.Models;

namespace OpenAIConsoleTest
{
    public class ChatCompletitionTestConsole : IHostedService
    {
        readonly IOpenAIChatCompletitionService _chatCompletitionService;
        readonly IHostApplicationLifetime _lifeTime;
        readonly IMemoryStoreService _memoryStoreService;

        public ChatCompletitionTestConsole(IOpenAIChatCompletitionService chatCompletitionService, IMemoryStoreService memoryStoreService, IHostApplicationLifetime lifeTime)
        {
            ArgumentNullException.ThrowIfNull(lifeTime, nameof(lifeTime));
            ArgumentNullException.ThrowIfNull(chatCompletitionService, nameof(chatCompletitionService));

            _chatCompletitionService = chatCompletitionService;
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
            Console.WriteLine("Start Chat Completition Test");

            ChatSession chatSession = new ChatSession();
            List<ChatHistoryItem> chatHistory = new List<ChatHistoryItem>();
            // Create chat history

            chatHistory.Add(new ChatHistoryItem("""
Sei un amichevole aiutante che conosce tutti i regolamenti dei giochi da tavolo più famosi. Rispondi nella maniera più precisa possibile. Dovrai attenerti solo alle conoscenze
che ti vengono fornite input. Se non conosci la risposta chiedi all'utente di essere più preciso nella richiesta oppure semplicemente risponderai di non sapere la risposta. 
""", RoleEnum.System));

            chatSession.ChatHistory = chatHistory;

            // Get chat completion service

            try
            {

                // Start the conversation
                while (true)
                {
                    // Get user input
                    Console.Write("User > ");
                    string userInput = Console.ReadLine();
                    if (string.IsNullOrEmpty(userInput) || userInput == "exit")
                    {
                        break;
                    }

                    var resultSearch = await _memoryStoreService.SearchMemoryAsync(userInput);
                    chatSession.InputMessage = userInput;
                    chatSession.ChatHistory.Add(new ChatHistoryItem(userInput + " " + String.Join(" ", resultSearch), RoleEnum.User));


                    // Get the response from the AI
                    var result = await _chatCompletitionService.ChatAsync(chatSession);

                    // Print the results
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Assistant > " + result);
                    Console.ResetColor();

                    // Add the message from the agent to the chat history
                    chatHistory.Add(new ChatHistoryItem(result.OutputMessage, RoleEnum.Assistant));
                }

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
