using System.ComponentModel;
using AiPortfolio.Application.Abstractions;
using Microsoft.SemanticKernel;

namespace AiPortfolio.Infrastructure.AI.Plugins;

/// <summary>
/// Plugin de Semantic Kernel: expone una función nativa de C# que el LLM
/// puede decidir invocar por su cuenta (function calling).
///
/// El [KernelFunction] + [Description] son los que Semantic Kernel usa para
/// que el modelo "entienda" cuándo y cómo llamar a este método.
///
/// Además de devolver el contexto al modelo, el plugin va anotando en
/// <see cref="RetrievedSources"/> qué documentos concretos se recuperaron,
/// para que <c>ChatAgentService</c> pueda poblar <c>ChatResponse.SourcesUsed</c>
/// con datos reales en vez de intentar adivinarlos del texto de la respuesta.
///
/// Referencia oficial:
/// https://learn.microsoft.com/en-us/semantic-kernel/concepts/plugins/
/// </summary>
public sealed class KnowledgeBasePlugin(IRagService ragService)
{
    private readonly List<string> _retrievedSources = [];

    /// <summary>
    /// Documentos (nombre de archivo o título) devueltos por
    /// <c>search_company_knowledge</c> durante la conversación actual, sin repetir
    /// y en el orden en que se usaron. La instancia del plugin es por-request
    /// (el Kernel se registra Scoped), así que esta lista no se comparte entre
    /// peticiones.
    /// </summary>
    public IReadOnlyList<string> RetrievedSources => _retrievedSources;

    [KernelFunction("search_company_knowledge")]
    [Description("Busca información relevante en la base de conocimiento de la empresa " +
                 "para responder preguntas sobre documentos, políticas o procesos internos.")]
    public async Task<string> SearchAsync(
        [Description("La pregunta o tema a buscar")] string query)
    {
        // SearchRelevantAsync ya viene filtrado por umbral de similitud, así que
        // aquí solo llegan documentos realmente relevantes (o ninguno).
        var results = await ragService.SearchRelevantAsync(query, topK: 3);

        if (results.Count == 0)
            return "No se encontró información relevante en la base de conocimiento.";

        foreach (var r in results)
        {
            // Preferimos el nombre de archivo (más preciso para citar); si el
            // documento se ingestó sin 'source', caemos al título.
            var label = string.IsNullOrWhiteSpace(r.Document.Source)
                ? r.Document.Title
                : r.Document.Source;

            if (!_retrievedSources.Contains(label))
                _retrievedSources.Add(label);
        }

        // Se concatena el contenido recuperado; esto es lo que luego el LLM
        // usa como contexto (grounding) para responder sin "alucinar".
        return string.Join(
            "\n---\n",
            results.Select(r => $"[Fuente: {r.Document.Title}]\n{r.Document.Content}"));
    }
}
