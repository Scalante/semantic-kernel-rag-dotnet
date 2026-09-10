namespace AiPortfolio.Application.DTOs;

public sealed record ChatRequest(string Question, string? ConversationId);

public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<string> SourcesUsed,
    string ConversationId);

public sealed record IngestDocumentRequest(string Title, string Content, string Source);

public sealed record ClassifyTicketRequest(string Description);

public sealed record ClassifyTicketResponse(string Category, float Confidence);
