using AiPortfolio.Application.Abstractions;
using AiPortfolio.Application.DTOs;
using AiPortfolio.Infrastructure.AI;
using AiPortfolio.Infrastructure.MLNet;
using AiPortfolio.Infrastructure.Rag;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuración: proveedor de LLM (agnóstico, ver KernelFactory) + Postgres/pgvector
// ---------------------------------------------------------------------------
var aiProviderOptions = new AiProviderOptions
{
    Provider = Enum.Parse<AiProvider>(builder.Configuration["Ai:Provider"] ?? "Ollama"),
    ModelId = builder.Configuration["Ai:ModelId"] ?? "llama3",
    ApiKey = builder.Configuration["Ai:ApiKey"],
    Endpoint = builder.Configuration["Ai:Endpoint"]
};

var ragOptions = new RagOptions
{
    ConnectionString = builder.Configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=55432;Database=aiportfolio;Username=postgres;Password=postgres",
    EmbeddingProvider = Enum.Parse<EmbeddingProvider>(
        builder.Configuration["Rag:EmbeddingProvider"] ?? "Ollama"),
    EmbeddingModelId = builder.Configuration["Rag:EmbeddingModelId"] ?? "nomic-embed-text",
    EmbeddingEndpoint = builder.Configuration["Rag:EmbeddingEndpoint"] ?? "http://localhost:11434",
    EmbeddingApiKey = builder.Configuration["OpenAI:ApiKey"] ?? string.Empty,
    MinRelevanceScore = double.TryParse(
        builder.Configuration["Rag:MinRelevanceScore"],
        System.Globalization.CultureInfo.InvariantCulture, out var minScore) ? minScore : 0.5
};

// ---------------------------------------------------------------------------
// Inyección de dependencias
// ---------------------------------------------------------------------------

// Kernel de Semantic Kernel: se registra Scoped (una instancia por request) para
// que cada ChatAgentService pueda registrar su plugin de RAG sin chocar con
// otras solicitudes concurrentes sobre la misma instancia de Kernel.
builder.Services.AddScoped<Kernel>(_ => KernelFactory.Create(aiProviderOptions));
builder.Services.AddScoped<IChatAgentService, ChatAgentService>();

builder.Services.AddRagInfrastructure(ragOptions);
builder.Services.AddMLNetInfrastructure();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AiPortfolio API",
        Version = "v1",
        Description = "Proyecto de portafolio: Microsoft.Extensions.AI + Semantic Kernel + RAG (pgvector) + ML.NET"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Panel de pruebas estático (wwwroot/index.html). Sirve la SPA en "/" y deja
// la API en "/api/*" y Swagger en "/swagger".
app.UseDefaultFiles();
app.UseStaticFiles();

// ---------------------------------------------------------------------------
// Endpoints
// ---------------------------------------------------------------------------

var api = app.MapGroup("/api");

// --- Estado: qué proveedor/modelo está activo (lo consume el panel de pruebas) ---
api.MapGet("/health", (RagOptions rag) => Results.Ok(new
    {
        status = "ok",
        llm = new { provider = aiProviderOptions.Provider.ToString(), model = aiProviderOptions.ModelId },
        embeddings = new
        {
            provider = rag.EmbeddingProvider.ToString(),
            model = rag.EmbeddingModelId,
            minRelevanceScore = rag.MinRelevanceScore
        }
    }))
    .WithName("Health")
    .WithSummary("Estado de la API y configuración activa de LLM/embeddings.");

// --- Semantic Kernel + Microsoft.Extensions.AI + RAG (agente conversacional) ---
api.MapPost("/chat", async (ChatRequest request, IChatAgentService chatAgent, CancellationToken ct) =>
    {
        var response = await chatAgent.AskAsync(request, ct);
        return Results.Ok(response);
    })
    .WithName("Chat")
    .WithSummary("Pregunta al agente de IA (usa RAG vía function calling cuando aplica).");

// --- RAG: ingestión de documentos propios ---
api.MapPost("/rag/documents", async (IngestDocumentRequest request, IRagService ragService, CancellationToken ct) =>
    {
        await ragService.IngestAsync(request, ct);
        return Results.Created();
    })
    .WithName("IngestDocument")
    .WithSummary("Genera el embedding de un documento y lo guarda en Postgres (pgvector).");

// --- RAG: carga rápida de los documentos de ejemplo, para poder hacer una demo ---
api.MapPost("/rag/seed", async (IRagService ragService, CancellationToken ct) =>
    {
        var count = await SampleKnowledgeSeeder.SeedAsync(ragService, ct: ct);
        return Results.Ok(new { DocumentosIngestados = count });
    })
    .WithName("SeedSampleDocuments")
    .WithSummary("Ingesta los documentos de ejemplo (políticas ficticias de empresa) para poder probar RAG.");

api.MapPost("/rag/search", async (string query, int topK, IRagService ragService, CancellationToken ct) =>
    {
        var results = await ragService.SearchRelevantAsync(query, topK == 0 ? 3 : topK, ct);
        return Results.Ok(results.Select(r => new
        {
            r.Document.Title,
            r.Document.Content,
            r.Document.Source,
            r.Score
        }));
    })
    .WithName("SearchKnowledge")
    .WithSummary("Búsqueda por similitud vectorial directa (sin pasar por el LLM), con el score de cada resultado. Útil para depurar el RAG y calibrar Rag:MinRelevanceScore.");

// --- ML.NET: clasificación clásica (no generativa) ---
api.MapPost("/tickets/classify", (ClassifyTicketRequest request, ITicketClassifierService classifier) =>
    {
        var result = classifier.Classify(request);
        return Results.Ok(result);
    })
    .WithName("ClassifyTicket")
    .WithSummary("Clasifica un ticket de soporte (Bug / Pregunta / Mejora) usando un modelo de ML.NET.");

app.Run();
