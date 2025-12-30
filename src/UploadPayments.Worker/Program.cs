using UploadPayments.Infrastructure;
using UploadPayments.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddUploadPaymentsInfrastructure(builder.Configuration);
builder.Services.Configure<ValidationWorkerOptions>(builder.Configuration.GetSection("ValidationWorker"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
