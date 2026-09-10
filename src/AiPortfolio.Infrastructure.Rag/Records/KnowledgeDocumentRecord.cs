using Microsoft.Extensions.VectorData;

namespace AiPortfolio.Infrastructure.Rag.Records;

/// <summary>
/// Modelo de registro para el almacén vectorial (Postgres + pgvector).
///
/// Este NO es el mismo tipo que <see cref="AiPortfolio.Domain.Entities.KnowledgeDocument"/>:
/// esa es la entidad de dominio que usa el resto de la aplicación; esta clase es el
/// "shape" concreto que Microsoft.Extensions.VectorData necesita para mapear filas de
/// Postgres, usando los atributos [VectorStoreKey]/[VectorStoreData]/[VectorStoreVector].
///
/// Referencia oficial:
/// https://learn.microsoft.com/en-us/dotnet/ai/conceptual/vector-databases
/// https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/
/// </summary>
public sealed class KnowledgeDocumentRecord
{
    [VectorStoreKey]
    public Guid Id { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public string Title { get; set; } = string.Empty;

    [VectorStoreData]
    public string Content { get; set; } = string.Empty;

    [VectorStoreData(IsIndexed = true)]
    public string Source { get; set; } = string.Empty;

    [VectorStoreData]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Vector de embeddings. La dimensión (768) corresponde al modelo
    /// "nomic-embed-text" de Ollama (el proveedor por defecto).
    /// Si cambias de modelo de embeddings hay que actualizar este número
    /// (text-embedding-3-small de OpenAI = 1536) y recrear la colección.
    /// </summary>
    [VectorStoreVector(768, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
