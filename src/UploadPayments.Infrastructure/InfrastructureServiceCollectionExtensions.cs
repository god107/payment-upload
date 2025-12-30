using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UploadPayments.Infrastructure.Persistence;

namespace UploadPayments.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddUploadPaymentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("UploadPaymentsDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'UploadPaymentsDb'.");
        }

        services.AddDbContextPool<UploadPaymentsDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
