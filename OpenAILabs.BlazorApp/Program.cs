using Microsoft.Extensions.Options;
using OpenAILabs.AIServices.Impl;
using OpenAILabs.AIServices;
using OpenAILabs.Common.Config;

var builder = WebApplication.CreateBuilder(args);

builder.RegisterConfiguration();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.RegisterServices();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();

static class ProgramExtensions
{
    public static void RegisterConfiguration(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<AzureOpenAIConfiguration>("chatcompletition")
            .Bind(builder.Configuration.GetSection("OpenAIChatCompletitionConfig"));
        builder.Services.AddOptions<AzureOpenAIConfiguration>("embedding")
                    .Bind(builder.Configuration.GetSection("OpenAIEmbeddingConfig"));
        builder.Services.AddOptions<AzureOpenAIConfiguration>("textgeneration")
                    .Bind(builder.Configuration.GetSection("OpenAITextGenerationConfig"));
        builder.Services.AddOptions<MemoryConfiguration>()
            .Bind(builder.Configuration.GetSection("MemoryConfig"));
    }

    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
        services.AddTransient<IMemoryStoreService, AzureOpenAISemanticKernelMemoryStoreService>();
        services.AddTransient<IOpenAIChatCompletitionService, AzureOpenAISemanticKernelChatCompletitionService>();
        services.AddSingleton<IConversationHistoryService, MemoryConversationHistoryService>();
    }
}
