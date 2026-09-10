using AiPortfolio.Application.Abstractions;
using AiPortfolio.Application.DTOs;

namespace AiPortfolio.Infrastructure.Rag;

/// <summary>
/// Utilidad de demo: lee los documentos de ejemplo en Data/sample-documents/
/// (políticas ficticias de una empresa) y los ingesta en el almacén vectorial,
/// para poder probar el flujo de RAG de punta a punta sin depender de datos reales.
///
/// Se expone como endpoint POST /api/rag/seed en AiPortfolio.Api para poder
/// ejecutarlo a demanda durante una demo o entrevista técnica.
/// </summary>
public static class SampleKnowledgeSeeder
{
    public static async Task<int> SeedAsync(IRagService ragService, string? contentRootPath = null, CancellationToken ct = default)
    {
        var baseDir = contentRootPath ?? AppContext.BaseDirectory;
        var sampleDir = Path.Combine(baseDir, "Data", "sample-documents");

        if (!Directory.Exists(sampleDir))
            return 0;

        var files = Directory.GetFiles(sampleDir, "*.txt");
        var ingested = 0;

        foreach (var file in files)
        {
            var title = Path.GetFileNameWithoutExtension(file).Replace('-', ' ');
            var content = await File.ReadAllTextAsync(file, ct);

            await ragService.IngestAsync(
                new IngestDocumentRequest(Title: title, Content: content, Source: Path.GetFileName(file)),
                ct);

            ingested++;
        }

        return ingested;
    }
}
