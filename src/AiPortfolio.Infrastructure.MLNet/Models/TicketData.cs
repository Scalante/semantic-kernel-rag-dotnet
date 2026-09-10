using Microsoft.ML.Data;

namespace AiPortfolio.Infrastructure.MLNet.Models;

/// <summary>
/// Fila de entrenamiento/entrada para el clasificador de tickets de soporte.
/// Mapea 1:1 con las columnas del CSV en Data/tickets-training.csv.
/// </summary>
public sealed class TicketData
{
    [LoadColumn(0)]
    public string Description { get; set; } = string.Empty;

    [LoadColumn(1)]
    public string Category { get; set; } = string.Empty;
}
