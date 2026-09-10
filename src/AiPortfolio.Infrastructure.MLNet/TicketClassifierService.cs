using AiPortfolio.Application.Abstractions;
using AiPortfolio.Application.DTOs;
using AiPortfolio.Infrastructure.MLNet.Models;
using Microsoft.ML;

namespace AiPortfolio.Infrastructure.MLNet;

/// <summary>
/// Implementación de ITicketClassifierService. Entrena el modelo una sola vez
/// (de forma perezosa/lazy) a partir de Data/tickets-training.csv y reutiliza el
/// PredictionEngine para clasificar tickets nuevos.
///
/// Nota de diseño: en un escenario productivo el entrenamiento se separaría del
/// servicio de predicción (un job/pipeline de entrenamiento que guarda un
/// modelo.zip con mlContext.Model.Save, y el servicio solo lo carga con
/// mlContext.Model.Load). Para este proyecto de portafolio se mantiene todo en
/// un solo servicio para que sea fácil de leer y de explicar en la entrevista.
/// </summary>
public sealed class TicketClassifierService : ITicketClassifierService, IDisposable
{
    private readonly MLContext _mlContext = new(seed: 0);
    private readonly Lazy<PredictionEngine<TicketData, TicketPrediction>> _predictionEngine;

    public TicketClassifierService(string? trainingCsvPath = null)
    {
        var csvPath = trainingCsvPath
            ?? Path.Combine(AppContext.BaseDirectory, "Data", "tickets-training.csv");

        _predictionEngine = new Lazy<PredictionEngine<TicketData, TicketPrediction>>(() =>
        {
            var model = TicketClassifierTrainer.Train(_mlContext, csvPath);
            return _mlContext.Model.CreatePredictionEngine<TicketData, TicketPrediction>(model);
        });
    }

    public ClassifyTicketResponse Classify(ClassifyTicketRequest request)
    {
        var input = new TicketData { Description = request.Description };
        var prediction = _predictionEngine.Value.Predict(input);

        var confidence = prediction.Score.Length > 0 ? prediction.Score.Max() : 0f;

        return new ClassifyTicketResponse(prediction.PredictedCategory, confidence);
    }

    public void Dispose()
    {
        if (_predictionEngine.IsValueCreated)
            _predictionEngine.Value.Dispose();
    }
}
