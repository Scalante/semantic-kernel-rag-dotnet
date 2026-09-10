using AiPortfolio.Application.DTOs;
using AiPortfolio.Infrastructure.MLNet;
using Xunit;

namespace AiPortfolio.Tests;

/// <summary>
/// Pruebas del clasificador de ML.NET. Entrena contra el mismo CSV de ejemplo
/// (Data/tickets-training.csv, copiado al output del proyecto MLNet) y verifica
/// que casos claros se clasifiquen en la categoría esperada.
///
/// Nota: al ser un modelo estadístico entrenado con pocos datos, estas pruebas
/// usan ejemplos muy claros para evitar falsos negativos por ambigüedad real.
/// </summary>
public sealed class TicketClassifierServiceTests
{
    private static string TrainingCsvPath =>
        Path.Combine(AppContext.BaseDirectory, "tickets-training.csv");

    [Fact]
    public void Classify_DescripcionDeError_DevuelveCategoriaBug()
    {
        var sut = new TicketClassifierService(TrainingCsvPath);

        var result = sut.Classify(new ClassifyTicketRequest(
            "La aplicación muestra un error 500 al guardar el formulario"));

        Assert.Equal("Bug", result.Category);
        Assert.True(result.Confidence > 0);
    }

    [Fact]
    public void Classify_Pregunta_DevuelveCategoriaPregunta()
    {
        var sut = new TicketClassifierService(TrainingCsvPath);

        var result = sut.Classify(new ClassifyTicketRequest(
            "¿Cómo puedo cambiar mi contraseña desde la aplicación?"));

        Assert.Equal("Pregunta", result.Category);
    }

    [Fact]
    public void Classify_SolicitudDeMejora_DevuelveCategoriaMejora()
    {
        var sut = new TicketClassifierService(TrainingCsvPath);

        var result = sut.Classify(new ClassifyTicketRequest(
            "Sería genial poder tener un modo oscuro en la aplicación"));

        Assert.Equal("Mejora", result.Category);
    }
}
