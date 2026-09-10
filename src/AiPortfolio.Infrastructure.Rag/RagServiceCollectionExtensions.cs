using AiPortfolio.Application.Abstractions;
using AiPortfolio.Infrastructure.Rag.Records;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace AiPortfolio.Infrastructure.Rag;

/// <summary>
/// Punto único de registro en el contenedor de DI para todo lo relacionado a RAG.
/// Se llama una sola vez desde AiPortfolio.Api/Program.cs:
///
///     builder.Services.AddRagInfrastructure(new RagOptions
///     {
///         ConnectionString  = builder.Configuration.GetConnectionString("Postgres")!,
///         EmbeddingProvider = EmbeddingProvider.Ollama
///     });
/// </summary>
public static class RagServiceCollectionExtensions
{
    public static IServiceCollection AddRagInfrastructure(this IServiceCollection services, RagOptions options)
    {
        services.AddSingleton(options);

        // Generador de embeddings agnóstico (Ollama u OpenAI, ver EmbeddingGeneratorFactory).
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
            _ => EmbeddingGeneratorFactory.Create(options));

        // Conector oficial de Semantic Kernel para Postgres + pgvector.
        // Este helper registra PostgresCollection<Guid, KnowledgeDocumentRecord> COMO
        // VectorStoreCollection<Guid, KnowledgeDocumentRecord> (que es el tipo que pide
        // el constructor de PostgresRagService) y crea internamente el NpgsqlDataSource
        // con el mapeo de tipos de pgvector a partir del connection string.
        services.AddPostgresCollection<Guid, KnowledgeDocumentRecord>(
            name: options.CollectionName,
            connectionString: options.ConnectionString);

        services.AddScoped<IRagService, PostgresRagService>();

        return services;
    }
}
