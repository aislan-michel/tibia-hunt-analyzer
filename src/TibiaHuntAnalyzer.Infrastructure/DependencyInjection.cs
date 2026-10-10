using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TibiaHuntAnalyzer.Infrastructure.Persistence;

namespace TibiaHuntAnalyzer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<HuntAnalyzerDbContext>(options =>
            options.UseSqlite($"Data Source={ApplicationDataPaths.GetDatabasePath()}"));

        return services;
    }
}
