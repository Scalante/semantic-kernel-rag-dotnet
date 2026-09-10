using Microsoft.ML.Data;

namespace AiPortfolio.Infrastructure.MLNet.Models;

/// <summary>
/// Salida del modelo entrenado. "PredictedLabel" es el nombre que ML.NET usa
/// por convención para la columna de salida del pipeline de clasificación
/// multiclase (KeyToValueMappingEstimator al final del pipeline la convierte
/// de nuevo a texto legible).
/// </summary>
public sealed class TicketPrediction
{
    [ColumnName("PredictedLabel")]
    public string PredictedCategory { get; set; } = string.Empty;

    /// <summary>
    /// Un score (probabilidad relativa) por cada categoría posible, en el mismo
    /// orden en que el modelo las aprendió. Se usa para calcular la confianza
    /// de la predicción (el valor máximo del arreglo).
    /// </summary>
    public float[] Score { get; set; } = Array.Empty<float>();
}
