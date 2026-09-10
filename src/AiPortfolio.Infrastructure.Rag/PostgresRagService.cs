using AiPortfolio.Application.Abstractions;
using AiPortfolio.Application.DTOs;
using AiPortfolio.Domain.Entities;
using AiPortfolio.Infrastructure.Rag.Records;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace AiPortfolio.Infrastructure.Rag;

/// <summary>
/// Implementación de IRagService sobre Postgres + pgvector, usando el conector
/// oficial de Semantic Kernel (Microsoft.SemanticKernel.Connectors.PgVector) a
/// través de la abstracción Microsoft.Extensions.VectorData.
///
/// Flujo:
///  - IngestAsync: genera el embedding del contenido y lo guarda como fila en Postgres.
///  - SearchRelevantAsync: genera el embedding de la pregunta, hace una búsqueda por
///    similitud de coseno contra los vectores ya almacenados (top-K) y descarta los
///    resultados por debajo de RagOptions.MinRelevanceScore.
///
/// Referencia oficial:
/// https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/postgres-connector
/// https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-vector-search-app
/// </summary>
public sealed class PostgresRagService(
    VectorStoreCollection<Guid, KnowledgeDocumentRecord> collection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    RagOptions options) : IRagService
{
    public async Task IngestAsync(IngestDocumentRequest request, CancellationToken ct = default)
    {
        // Nos asegura que la tabla/colección exista antes de escribir (idempotente).
        await collection.EnsureCollectionExistsAsync(ct);

        var embedding = await embeddingGenerator.GenerateAsync(request.Content, cancellationToken: ct);

        var record = new KnowledgeDocumentRecord
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            Source = request.Source,
            CreatedAt = DateTimeOffset.UtcNow,
            Embedding = embedding.Vector
        };

        await collection.UpsertAsync(record, ct);
    }

    public async Task<IReadOnlyList<RagSearchResult>> SearchRelevantAsync(
        string query, int topK = 3, CancellationToken ct = default)
    {
        await collection.EnsureCollectionExistsAsync(ct);

        var queryEmbedding = await embeddingGenerator.GenerateAsync(query, cancellationToken: ct);

        var results = new List<RagSearchResult>();

        // Búsqueda vectorial por similitud de coseno (definida en el atributo
        // [VectorStoreVector] del record) contra los documentos ya indexados.
        // El almacén devuelve result.Score = similitud de coseno (más alto = más
        // parecido). Descartamos los que no lleguen al umbral mínimo configurado
        // para no pasar "ruido" al LLM ni citarlos como fuente.
        await foreach (var result in collection.SearchAsync(queryEmbedding.Vector, top: topK, cancellationToken: ct))
        {
            if (result.Score is double score && score < options.MinRelevanceScore)
                continue;

            var document = new KnowledgeDocument
            {
                Id = result.Record.Id,
                Title = result.Record.Title,
                Content = result.Record.Content,
                Source = result.Record.Source,
                CreatedAt = result.Record.CreatedAt,
                Embedding = result.Record.Embedding
            };

            results.Add(new RagSearchResult(document, result.Score));
        }

        return results;
    }
}
