using UploadPayments.BlazorUI.Components;
using UploadPayments.BlazorUI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure HttpClient for API communication
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8080";
builder.Services.AddHttpClient("PaymentUploadsApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add API service clients
builder.Services.AddScoped<PaymentUploadsApiClient>();
builder.Services.AddScoped<ValidationRulesApiClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
