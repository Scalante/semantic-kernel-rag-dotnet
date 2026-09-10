using System.Security.Cryptography;
using System.Text;
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
///    La clave (Id) se deriva de la "fuente" del documento, así que reingresar el
///    mismo archivo SOBRESCRIBE la fila en vez de duplicarla (ingesta idempotente).
///  - SearchRelevantAsync: genera el embedding de la pregunta, hace una búsqueda por
///    similitud de coseno contra los vectores ya almacenados, descarta los resultados
///    por debajo de RagOptions.MinRelevanceScore y deja un único resultado por
///    documento (por si quedaran duplicados de datos previos).
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
            // Id determinista a partir de la fuente (o el título si no hay fuente):
            // el mismo documento siempre cae en la misma fila y UpsertAsync la
            // reemplaza en lugar de insertar una copia.
            Id = DeterministicId(request.Source, request.Title),
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
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Pedimos algunos resultados de más para poder colapsar duplicados (mismo
        // documento indexado varias veces) sin quedarnos cortos frente a topK.
        var fetch = Math.Max(topK * 3, topK + 5);

        // Búsqueda vectorial por similitud de coseno (definida en el atributo
        // [VectorStoreVector] del record) contra los documentos ya indexados.
        // result.Score = similitud de coseno (más alto = más parecido).
        await foreach (var result in collection.SearchAsync(queryEmbedding.Vector, top: fetch, cancellationToken: ct))
        {
            // Descarta el "ruido" por debajo del umbral configurado.
            if (result.Score is double score && score < options.MinRelevanceScore)
                continue;

            // Un solo resultado por documento. Como vienen ordenados por score
            // descendente, nos quedamos con la mejor coincidencia de cada uno.
            var key = !string.IsNullOrWhiteSpace(result.Record.Source)
                ? result.Record.Source
                : result.Record.Title;
            if (!seen.Add(key))
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

            if (results.Count == topK)
                break;
        }

        return results;
    }

    /// <summary>
    /// Deriva un Guid estable a partir de un texto (MD5 → 16 bytes = un Guid).
    /// No es criptográfico; solo necesitamos que la misma entrada dé siempre el
    /// mismo Id para que la ingesta sea idempotente.
    /// </summary>
    private static Guid DeterministicId(string? source, string? title)
    {
        var seed = !string.IsNullOrWhiteSpace(source) ? source! : (title ?? string.Empty);
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(seed.Trim().ToLowerInvariant()));
        return new Guid(hash);
    }
}
