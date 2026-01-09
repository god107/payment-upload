using UploadPayments.Infrastructure;
using UploadPayments.Api.HostedServices;
using UploadPayments.Api.Hubs;
using UploadPayments.Api.Services;
using UploadPayments.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR for real-time upload notifications
builder.Services.AddSignalR();
builder.Services.AddSingleton<IPaymentUploadNotificationService, PaymentUploadNotificationService>();

builder.Services.AddUploadPaymentsInfrastructure(builder.Configuration);
builder.Services.AddHostedService<DatabaseBootstrapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<PaymentUploadHub>("/hubs/payment-uploads");

app.Run();
