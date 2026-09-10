using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace AiPortfolio.Infrastructure.AI;

/// <summary>
/// Construye el Kernel de Semantic Kernel de forma AGNÓSTICA al proveedor de LLM.
///
/// La idea clave (pedida explícitamente en la vacante) es que el mismo Kernel
/// pueda hablar con OpenAI, Anthropic, Ollama, etc. sin cambiar el código de
/// negocio — solo cambia qué IChatClient se registra aquí.
///
/// Referencia oficial:
/// https://learn.microsoft.com/en-us/semantic-kernel/get-started/quick-start-guide
/// https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai
/// </summary>
public static class KernelFactory
{
    public static Kernel Create(AiProviderOptions options)
    {
        var builder = Kernel.CreateBuilder();

        // Microsoft.Extensions.AI expone IChatClient como abstracción común.
        // Semantic Kernel sabe "adaptar" cualquier IChatClient registrado.
        // Aquí decidimos QUÉ proveedor concreto se conecta según configuración,
        // sin que el resto de la aplicación (plugins, agente, endpoints) lo sepa.
        switch (options.Provider)
        {
            case AiProvider.OpenAi:
                builder.AddOpenAIChatCompletion(
                    modelId: options.ModelId, // ej. "gpt-4o-mini"
                    apiKey: options.ApiKey!);
                break;

            case AiProvider.AzureOpenAi:
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: options.ModelId,
                    endpoint: options.Endpoint!,
                    apiKey: options.ApiKey!);
                break;

            case AiProvider.Ollama:
                // Ollama corre local (ej. llama3, mistral) — útil para desarrollar
                // sin costo de API mientras pruebas el resto del flujo.
                // Conector nativo: Microsoft.SemanticKernel.Connectors.Ollama (alpha).
#pragma warning disable SKEXP0070 // El conector de Ollama está marcado como experimental por el equipo de SK.
                builder.AddOllamaChatCompletion(
                    modelId: options.ModelId, // ej. "llama3"
                    endpoint: new Uri(options.Endpoint ?? "http://localhost:11434"));
#pragma warning restore SKEXP0070
                break;

            case AiProvider.Anthropic:
                // Anthropic no tiene conector nativo aún en Semantic Kernel;
                // se integra implementando IChatClient de Microsoft.Extensions.AI
                // y registrándolo con builder.Services.AddChatClient(...).
                // Ver: https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai
                throw new NotSupportedException(
                    "Anthropic se conecta implementando IChatClient manualmente. " +
                    "Ver AnthropicChatClient.cs (pendiente) para el patrón a seguir.");

            default:
                throw new ArgumentOutOfRangeException(nameof(options.Provider));
        }

        return builder.Build();
    }
}

public enum AiProvider
{
    OpenAi,
    AzureOpenAi,
    Ollama,
    Anthropic
}

public sealed class AiProviderOptions
{
    public AiProvider Provider { get; init; } = AiProvider.Ollama;
    public string ModelId { get; init; } = "llama3";
    public string? ApiKey { get; init; }
    public string? Endpoint { get; init; }
}
