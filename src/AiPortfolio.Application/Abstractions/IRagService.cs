using AiPortfolio.Application.DTOs;

namespace AiPortfolio.Application.Abstractions;

/// <summary>
/// Encapsula la parte de Retrieval-Augmented Generation: generar embeddings,
/// guardarlos en el almacén vectorial (pgvector) y buscar los fragmentos
/// más relevantes para una pregunta dada.
/// Implementada en Infrastructure.Rag.
/// </summary>
public interface IRagService
{
    Task IngestAsync(IngestDocumentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Devuelve hasta <paramref name="topK"/> documentos ordenados por similitud
    /// de coseno con <paramref name="query"/>, ya filtrados por el umbral mínimo
    /// de relevancia configurado (RagOptions.MinRelevanceScore). Puede devolver
    /// una lista vacía si ningún documento supera el umbral.
    /// </summary>
    Task<IReadOnlyList<RagSearchResult>> SearchRelevantAsync(
        string query, int topK = 3, CancellationToken ct = default);
}
