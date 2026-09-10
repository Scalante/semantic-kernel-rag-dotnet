using Microsoft.Extensions.AI;
using OllamaSharp;
using OpenAI;

namespace AiPortfolio.Infrastructure.Rag;

/// <summary>
/// Construye el generador de embeddings usando la abstracción agnóstica
/// IEmbeddingGenerator&lt;string, Embedding&lt;float&gt;&gt; de Microsoft.Extensions.AI.
///
/// Igual que con AiPortfolio.Infrastructure.AI.KernelFactory, la idea es que el
/// resto del sistema (PostgresRagService) dependa solo de la interfaz, nunca del
/// SDK concreto — así se puede cambiar de proveedor de embeddings (Ollama local
/// u OpenAI) sin tocar el código de negocio.
///
/// IMPORTANTE: la dimensión del vector NO es agnóstica. Cada modelo produce
/// vectores de un tamaño fijo (nomic-embed-text = 768, text-embedding-3-small
/// = 1536). Si cambias de modelo debes actualizar la dimensión en
/// <see cref="Records.KnowledgeDocumentRecord.Embedding"/> y recrear la colección.
///
/// Referencia oficial:
/// https://learn.microsoft.com/en-us/dotnet/ai/conceptual/embeddings
/// https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-vector-search-app
/// </summary>
public static class EmbeddingGeneratorFactory
{
    public static IEmbeddingGenerator<string, Embedding<float>> Create(RagOptions options) =>
        options.EmbeddingProvider switch
        {
            // Ollama local (por defecto): sin costo, sin API key.
            // OllamaApiClient implementa IEmbeddingGenerator<string, Embedding<float>>
            // de Microsoft.Extensions.AI de forma nativa.
            // Requiere: ollama pull nomic-embed-text
            EmbeddingProvider.Ollama => new OllamaApiClient(
                new Uri(options.EmbeddingEndpoint), options.EmbeddingModelId),

            // OpenAI: cliente oficial (paquete "OpenAI") envuelto como IEmbeddingGenerator
            // mediante el adaptador de Microsoft.Extensions.AI.OpenAI.
            EmbeddingProvider.OpenAi => new OpenAIClient(options.EmbeddingApiKey)
                .GetEmbeddingClient(options.EmbeddingModelId) // ej. "text-embedding-3-small"
                .AsIEmbeddingGenerator(),

            _ => throw new ArgumentOutOfRangeException(
                nameof(options), options.EmbeddingProvider, "Proveedor de embeddings no soportado.")
        };
}

/// <summary>
/// Proveedor de embeddings para el módulo RAG.
/// </summary>
public enum EmbeddingProvider
{
    /// <summary>Ollama corriendo local (http://localhost:11434). Sin costo, sin API key.</summary>
    Ollama,

    /// <summary>API de OpenAI. Requiere una API key real.</summary>
    OpenAi
}

/// <summary>
/// Configuración para la infraestructura de RAG: cadena de conexión a Postgres
/// (con pgvector habilitado) y el proveedor de embeddings.
/// Se enlaza desde appsettings.json / variables de entorno en AiPortfolio.Api.
/// </summary>
public sealed class RagOptions
{
    public required string ConnectionString { get; init; }

    /// <summary>Ollama (por defecto) u OpenAi.</summary>
    public EmbeddingProvider EmbeddingProvider { get; init; } = EmbeddingProvider.Ollama;

    /// <summary>
    /// Modelo de embeddings. Ollama: "nomic-embed-text" (768 dim).
    /// OpenAI: "text-embedding-3-small" (1536 dim).
    /// </summary>
    public string EmbeddingModelId { get; init; } = "nomic-embed-text";

    /// <summary>Endpoint de Ollama (solo si EmbeddingProvider = Ollama).</summary>
    public string EmbeddingEndpoint { get; init; } = "http://localhost:11434";

    /// <summary>API key de OpenAI (solo si EmbeddingProvider = OpenAi).</summary>
    public string EmbeddingApiKey { get; init; } = string.Empty;

    public string CollectionName { get; init; } = "knowledge_documents";

    /// <summary>
    /// Umbral mínimo de similitud de coseno (0..1) para considerar un documento
    /// relevante. Los resultados de la búsqueda vectorial por debajo de este
    /// valor se descartan antes de pasarlos al LLM.
    ///
    /// Calibrado para nomic-embed-text, cuyos scores quedan comprimidos: un
    /// documento que responde la pregunta ronda 0.70+, mientras que el "ruido"
    /// se queda en ~0.50-0.55. 0.65 separa bien ambos. Con text-embedding-3-small
    /// de OpenAI los scores se reparten más y este umbral puede bajarse.
    /// </summary>
    public double MinRelevanceScore { get; init; } = 0.65;
}
