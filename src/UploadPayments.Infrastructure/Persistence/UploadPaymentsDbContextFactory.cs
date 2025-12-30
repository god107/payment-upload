using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UploadPayments.Infrastructure.Persistence;

public sealed class UploadPaymentsDbContextFactory : IDesignTimeDbContextFactory<UploadPaymentsDbContext>
{
    public UploadPaymentsDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("UPLOADPAYMENTS_CONNECTIONSTRING")
            ?? "Host=localhost;Port=5432;Database=upload_payments;Username=upload_payments;Password=upload_payments";

        var optionsBuilder = new DbContextOptionsBuilder<UploadPaymentsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new UploadPaymentsDbContext(optionsBuilder.Options);
    }
}
