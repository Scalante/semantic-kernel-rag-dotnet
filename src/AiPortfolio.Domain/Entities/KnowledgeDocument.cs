namespace AiPortfolio.Domain.Entities;

/// <summary>
/// Representa un fragmento de documento empresarial que se indexa para RAG.
/// El campo Embedding se llena en la capa de infraestructura (Infrastructure.Rag)
/// usando un modelo de embeddings antes de guardarlo en Postgres con pgvector.
/// </summary>
public sealed class KnowledgeDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Title { get; init; }
    public required string Content { get; init; }
    public string Source { get; init; } = "manual-upload";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Vector de embeddings (768 dimensiones con nomic-embed-text, el modelo por
    /// defecto; 1536 con text-embedding-3-small de OpenAI). Se mapea a una columna
    /// "vector" en Postgres vía el conector de pgvector.
    /// </summary>
    public ReadOnlyMemory<float> Embedding { get; set; }
}
