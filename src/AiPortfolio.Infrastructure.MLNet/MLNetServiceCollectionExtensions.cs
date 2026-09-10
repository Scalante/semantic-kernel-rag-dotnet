using AiPortfolio.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AiPortfolio.Infrastructure.MLNet;

/// <summary>
/// Registro en el contenedor de DI del clasificador de ML.NET. Se llama desde
/// AiPortfolio.Api/Program.cs: builder.Services.AddMLNetInfrastructure();
/// </summary>
public static class MLNetServiceCollectionExtensions
{
    public static IServiceCollection AddMLNetInfrastructure(this IServiceCollection services)
    {
        // Singleton: el entrenamiento (perezoso) solo debe ocurrir una vez por
        // ciclo de vida de la aplicación.
        services.AddSingleton<ITicketClassifierService>(_ => new TicketClassifierService());
        return services;
    }
}
