using AiPortfolio.Infrastructure.MLNet.Models;
using Microsoft.ML;

namespace AiPortfolio.Infrastructure.MLNet;

/// <summary>
/// Entrena el pipeline de clasificación multiclase de ML.NET (Machine Learning
/// "clásico", NO generativo) que la vacante pide junto a las capacidades de IA
/// con LLMs. Clasifica tickets de soporte en: Bug, Pregunta o Mejora.
///
/// Pipeline: FeaturizeText (convierte el texto en vector de features TF-IDF-like)
/// + MapValueToKey (la etiqueta de texto a un "key" numérico) + SdcaMaximumEntropy
/// (entrenador multiclase) + MapKeyToValue (vuelve a texto legible al final).
///
/// Referencia oficial:
/// https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/github-issue-classification
/// https://learn.microsoft.com/en-us/dotnet/api/microsoft.ml.trainers.sdcamaximumentropymulticlasstrainer
/// </summary>
public static class TicketClassifierTrainer
{
    public static ITransformer Train(MLContext mlContext, string csvPath)
    {
        IDataView trainingData = mlContext.Data.LoadFromTextFile<TicketData>(
            csvPath, hasHeader: true, separatorChar: ',');

        var pipeline = mlContext.Transforms.Conversion
            .MapValueToKey(inputColumnName: nameof(TicketData.Category), outputColumnName: "Label")
            .Append(mlContext.Transforms.Text.FeaturizeText(
                inputColumnName: nameof(TicketData.Description),
                outputColumnName: "Features"))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label",
                featureColumnName: "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue(
                inputColumnName: "PredictedLabel",
                outputColumnName: "PredictedLabel"));

        return pipeline.Fit(trainingData);
    }

    /// <summary>
    /// Entrena y evalúa con validación cruzada (5 folds) — útil para mostrar en la
    /// entrevista que el modelo se valida, no solo se entrena y se usa a ciegas.
    /// </summary>
    public static double EvaluateMacroAccuracy(MLContext mlContext, string csvPath)
    {
        IDataView data = mlContext.Data.LoadFromTextFile<TicketData>(
            csvPath, hasHeader: true, separatorChar: ',');

        var pipeline = mlContext.Transforms.Conversion
            .MapValueToKey(inputColumnName: nameof(TicketData.Category), outputColumnName: "Label")
            .Append(mlContext.Transforms.Text.FeaturizeText(
                inputColumnName: nameof(TicketData.Description),
                outputColumnName: "Features"))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                labelColumnName: "Label",
                featureColumnName: "Features"));

        var crossValidationResults = mlContext.MulticlassClassification
            .CrossValidate(data, pipeline, numberOfFolds: 5, labelColumnName: "Label");

        return crossValidationResults.Average(r => r.Metrics.MacroAccuracy);
    }
}
