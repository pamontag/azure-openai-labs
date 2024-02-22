using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using OpenAILabs.Common.Model;

namespace OpenAILabs.AIServices.Impl
{
    public class CosmosDBConversationHistoryService : IConversationHistoryService
    {
        private readonly Container _container;

        public CosmosDBConversationHistoryService(string endpoint, string key, string databaseName, string containerName)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(databaseName);
            ArgumentNullException.ThrowIfNullOrEmpty(containerName);
            ArgumentNullException.ThrowIfNullOrEmpty(endpoint);
            ArgumentNullException.ThrowIfNullOrEmpty(key);

            CosmosSerializationOptions options = new()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            };

            CosmosClient client = new CosmosClientBuilder(endpoint, key)
                .WithSerializerOptions(options)
                .Build();

            Database? database = client?.GetDatabase(databaseName);
            Container? container = database?.GetContainer(containerName);

            _container = container ??
                throw new ArgumentException("Unable to connect to existing Azure Cosmos DB container or database.");
        }

        /// <summary>
        /// Creates a new chat session.
        /// </summary>
        /// <param name="session">Chat session item to create.</param>
        /// <returns>Newly created chat session item.</returns>
        public async Task<ChatSession> InsertSessionAsync(ChatSession session)
        {
            PartitionKey partitionKey = new(session.SessionId);
            return await _container.CreateItemAsync<ChatSession>(
                item: session,
                partitionKey: partitionKey
            );
        }

        /// <summary>
        /// Creates a new chat message.
        /// </summary>
        /// <param name="message">Chat message item to create.</param>
        /// <returns>Newly created chat message item.</returns>
        public async Task<ChatHistoryItem> InsertMessageAsync(ChatHistoryItem message)
        {
            PartitionKey partitionKey = new(message.SessionId);
            ChatHistoryItem newMessage = message with { TimeStamp = DateTime.UtcNow };
            return await _container.CreateItemAsync<ChatHistoryItem>(
                item: message,
                partitionKey: partitionKey
            );
        }

        /// <summary>
        /// Gets a list of all current chat sessions.
        /// </summary>
        /// <returns>List of distinct chat session items.</returns>
        public async Task<List<ChatSession>> GetSessionsAsync()
        {
            QueryDefinition query = new QueryDefinition("SELECT DISTINCT * FROM c WHERE c.type = @type")
            .WithParameter("@type", nameof(ChatSession));
            FeedIterator<ChatSession> response = _container.GetItemQueryIterator<ChatSession>(query);

            List<ChatSession> output = new();
            while (response.HasMoreResults)
            {
                FeedResponse<ChatSession> results = await response.ReadNextAsync();
                output.AddRange(results);
            }
            return output;
        }

        /// <summary>
        /// Gets a list of all current chat messages for a specified session identifier.
        /// </summary>
        /// <param name="sessionId">Chat session identifier used to filter messsages.</param>
        /// <returns>List of chat message items for the specified session.</returns>
        public async Task<List<ChatHistoryItem>> GetSessionMessagesAsync(string sessionId)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.sessionId = @sessionId AND c.type = @type")
                .WithParameter("@sessionId", sessionId)
                .WithParameter("@type", nameof(ChatHistoryItem));

            FeedIterator<ChatHistoryItem> results = _container.GetItemQueryIterator<ChatHistoryItem>(query);

            List<ChatHistoryItem> output = new();
            while (results.HasMoreResults)
            {
                FeedResponse<ChatHistoryItem> response = await results.ReadNextAsync();
                output.AddRange(response);
            }
            return output;
        }

        /// <summary>
        /// Updates an existing chat session.
        /// </summary>
        /// <param name="session">Chat session item to update.</param>
        /// <returns>Revised created chat session item.</returns>
        public async Task<ChatSession> UpdateSessionAsync(ChatSession session)
        {
            PartitionKey partitionKey = new(session.SessionId);
            return await _container.ReplaceItemAsync(
                item: session,
                id: session.Id,
                partitionKey: partitionKey
            );
        }

        /// <summary>
        /// Batch create or update chat messages and session.
        /// </summary>
        /// <param name="messages">Chat message and session items to create or replace.</param>
        public async Task UpsertSessionBatchAsync(params dynamic[] messages)
        {
            if (messages.Select(m => m.SessionId).Distinct().Count() > 1)
            {
                throw new ArgumentException("All items must have the same partition key.");
            }

            PartitionKey partitionKey = new(messages.First().SessionId);
            TransactionalBatch batch = _container.CreateTransactionalBatch(partitionKey);
            foreach (var message in messages)
            {
                batch.UpsertItem(
                    item: message
                );
            }
            await batch.ExecuteAsync();
        }

        /// <summary>
        /// Batch deletes an existing chat session and all related messages.
        /// </summary>
        /// <param name="sessionId">Chat session identifier used to flag messages and sessions for deletion.</param>
        public async Task DeleteSessionAndMessagesAsync(string sessionId)
        {
            PartitionKey partitionKey = new(sessionId);

            QueryDefinition query = new QueryDefinition("SELECT VALUE c.id FROM c WHERE c.sessionId = @sessionId")
                    .WithParameter("@sessionId", sessionId);

            FeedIterator<string> response = _container.GetItemQueryIterator<string>(query);

            TransactionalBatch batch = _container.CreateTransactionalBatch(partitionKey);
            while (response.HasMoreResults)
            {
                FeedResponse<string> results = await response.ReadNextAsync();
                foreach (var itemId in results)
                {
                    batch.DeleteItem(
                        id: itemId
                    );
                }
            }
            await batch.ExecuteAsync();
        }

    }
}
