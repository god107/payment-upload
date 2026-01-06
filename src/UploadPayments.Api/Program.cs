using UploadPayments.Infrastructure;
using UploadPayments.Api.HostedServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.Run();
