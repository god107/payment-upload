using UploadPayments.Infrastructure;
using UploadPayments.Worker;
using UploadPayments.Worker.Services;
using UploadPayments.Contracts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddUploadPaymentsInfrastructure(builder.Configuration);
builder.Services.Configure<ValidationWorkerOptions>(builder.Configuration.GetSection("ValidationWorker"));

// Configure HttpClient for notification service to call API
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://api:8080";
builder.Services.AddHttpClient<IPaymentUploadNotificationService, HttpPaymentUploadNotificationService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
