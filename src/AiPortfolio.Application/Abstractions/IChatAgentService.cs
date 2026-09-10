using AiPortfolio.Application.DTOs;

namespace AiPortfolio.Application.Abstractions;

/// <summary>
/// Orquesta el agente de IA: recibe una pregunta, decide si necesita
/// recuperar contexto vía RAG, invoca funciones (function calling) si aplica,
/// y devuelve una respuesta generada por el LLM configurado.
/// Implementada en Infrastructure.AI usando Semantic Kernel.
/// </summary>
public interface IChatAgentService
{
    Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken ct = default);
}
