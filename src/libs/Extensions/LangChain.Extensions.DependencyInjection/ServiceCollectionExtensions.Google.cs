using LangChain.Providers;
using LangChain.Providers.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LangChain.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IServiceCollection AddGoogle(
        this IServiceCollection services)
    {
        services = services ?? throw new ArgumentNullException(nameof(services));

        _ = services
            .AddOptions<GenerativeAiConfiguration>()
            .BindConfiguration(configSectionPath: "Google");
        _ = services
            .AddHttpClient<GenerativeModel>();
        _ = services
            .AddScoped<GenerativeModel>(static services => new GenerativeModel(
                configuration: services.GetRequiredService<IOptions<GenerativeAiConfiguration>>().Value,
                httpClient: services.GetRequiredService<HttpClient>()));

        return services;
    }
}