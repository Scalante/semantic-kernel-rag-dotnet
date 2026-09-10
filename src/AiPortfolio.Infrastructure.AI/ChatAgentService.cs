using AiPortfolio.Application.Abstractions;
using AiPortfolio.Application.DTOs;
using AiPortfolio.Infrastructure.AI.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AiPortfolio.Infrastructure.AI;

/// <summary>
/// Implementa el agente de IA de punta a punta:
///  1) Recibe la pregunta del usuario.
///  2) Le da al LLM acceso al plugin de RAG (function calling automático).
///  3) El LLM decide si necesita buscar en la base de conocimiento antes
///     de responder ("prompt chaining" implícito vía tool calling).
///  4) Devuelve la respuesta ya fundamentada en los documentos reales.
///
/// Referencia oficial (function calling automático):
/// https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/function-calling/
/// Referencia oficial (RAG con agentes):
/// https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-rag
/// </summary>
public sealed class ChatAgentService : IChatAgentService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly KnowledgeBasePlugin _knowledgeBasePlugin;

    public ChatAgentService(Kernel kernel, IRagService ragService)
    {
        _kernel = kernel;

        // Registramos el plugin de RAG como herramienta disponible para el modelo.
        // Guardamos la referencia para leer después qué documentos recuperó.
        _knowledgeBasePlugin = new KnowledgeBasePlugin(ragService);
        _kernel.Plugins.AddFromObject(_knowledgeBasePlugin, "KnowledgeBase");

        _chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken ct = default)
    {
        var history = new ChatHistory();
        history.AddSystemMessage(
            """
            Eres un asistente empresarial.

            Usa la función search_company_knowledge ÚNICAMENTE cuando la pregunta
            trate sobre información interna de la empresa: políticas, procesos,
            beneficios, onboarding, soporte de TI, etc. En ese caso responde SOLO
            con lo que devuelva la función y, si no encuentra nada, dilo
            explícitamente en vez de inventar (evita alucinaciones).

            Para preguntas de conocimiento general (matemáticas, definiciones,
            cultura general) NO llames a ninguna función: responde directamente.
            """);
        history.AddUserMessage(request.Question);

        // AutoInvokeKernelFunctions: el modelo decide por sí mismo si llama al plugin
        // (function calling) antes de generar la respuesta final.
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var result = await _chatCompletion.GetChatMessageContentAsync(
            history, settings, _kernel, ct);

        var conversationId = request.ConversationId ?? Guid.NewGuid().ToString();

        return new ChatResponse(
            Answer: result.Content ?? string.Empty,
            // Fuentes reales: los documentos que el plugin de RAG recuperó durante
            // esta petición (vacío si el modelo respondió sin consultar la base).
            SourcesUsed: _knowledgeBasePlugin.RetrievedSources.ToArray(),
            ConversationId: conversationId);
    }
}
