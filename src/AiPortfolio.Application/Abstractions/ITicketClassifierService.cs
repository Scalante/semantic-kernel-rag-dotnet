using AiPortfolio.Application.DTOs;

namespace AiPortfolio.Application.Abstractions;

/// <summary>
/// Clasificador de tickets de soporte basado en ML.NET (no LLM):
/// demuestra el uso de Machine Learning "clásico" dentro de .NET,
/// tal como lo pide la vacante junto a las capacidades de IA generativa.
/// Implementada en Infrastructure.MLNet.
/// </summary>
public interface ITicketClassifierService
{
    ClassifyTicketResponse Classify(ClassifyTicketRequest request);
}
