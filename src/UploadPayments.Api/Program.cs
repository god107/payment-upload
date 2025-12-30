using UploadPayments.Infrastructure;
using UploadPayments.Api.HostedServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddUploadPaymentsInfrastructure(builder.Configuration);
builder.Services.AddHostedService<DatabaseBootstrapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
