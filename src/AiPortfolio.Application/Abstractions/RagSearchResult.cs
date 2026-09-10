using AiPortfolio.Domain.Entities;

namespace AiPortfolio.Application.Abstractions;

/// <summary>
/// Un documento recuperado por la búsqueda vectorial junto con su puntaje de
/// similitud. El puntaje es la similitud de coseno devuelta por pgvector
/// (rango ~0..1; más alto = más parecido a la consulta) y sirve para descartar
/// resultados poco relevantes antes de pasarlos al LLM.
/// </summary>
/// <param name="Document">El documento de la base de conocimiento.</param>
/// <param name="Score">
/// Similitud de coseno con la consulta. <c>null</c> si el almacén no la reporta.
/// </param>
public sealed record RagSearchResult(KnowledgeDocument Document, double? Score);
