namespace AiPortfolio.Domain.Entities;

/// <summary>
/// Entidad de ejemplo para el módulo de ML.NET: un ticket de soporte que se
/// clasifica automáticamente por categoría (Bug, Pregunta, Solicitud de mejora).
/// </summary>
public sealed class SupportTicket
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Description { get; init; }
    public string? PredictedCategory { get; set; }
    public float Confidence { get; set; }
}
