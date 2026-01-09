using System.Net.Http.Json;
using UploadPayments.Contracts;

namespace UploadPayments.BlazorUI.Services;

public sealed class PaymentUploadsApiClient(IHttpClientFactory httpClientFactory)
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("PaymentUploadsApi");

    public async Task<List<UploadListItemDto>?> GetUploadsAsync(int limit = 50)
    {
        using var client = CreateClient();
        return await client.GetFromJsonAsync<List<UploadListItemDto>>($"/api/payment-uploads?limit={limit}");
    }

    public async Task<UploadAcceptedDto?> UploadFileAsync(IFormFile file)
    {
        using var client = CreateClient();
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(file.OpenReadStream());
        
        content.Add(fileContent, "File", file.FileName);
        
        var response = await client.PostAsync("/api/payment-uploads", content);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<UploadAcceptedDto>();
    }

    public async Task<UploadStatusDto?> GetUploadStatusAsync(Guid uploadId, Guid token)
    {
        using var client = CreateClient();
        return await client.GetFromJsonAsync<UploadStatusDto>($"/api/payment-uploads/{uploadId}/status?token={token}");
    }

    public async Task<UploadErrorsPageDto?> GetErrorsAsync(Guid uploadId, Guid token, int? cursorRow = null, int limit = 100)
    {
        using var client = CreateClient();
        var url = $"/api/payment-uploads/{uploadId}/errors?token={token}&limit={limit}";
        if (cursorRow.HasValue)
            url += $"&cursorRow={cursorRow}";
        
        return await client.GetFromJsonAsync<UploadErrorsPageDto>(url);
    }

    public async Task<UploadErrorsPageDto?> GetUploadErrorsAsync(Guid uploadId, Guid token, int? cursorRow = null, int limit = 100)
        => await GetErrorsAsync(uploadId, token, cursorRow, limit);

    public async Task<PaymentInstructionsPageDto?> GetInstructionsAsync(Guid uploadId, Guid token, int? cursorRow = null, int limit = 100)
    {
        using var client = CreateClient();
        var url = $"/api/payment-uploads/{uploadId}/instructions?token={token}&limit={limit}";
        if (cursorRow.HasValue)
            url += $"&cursorRow={cursorRow}";
        
        return await client.GetFromJsonAsync<PaymentInstructionsPageDto>(url);
    }

    public async Task<PaymentInstructionsPageDto?> GetPaymentInstructionsAsync(Guid uploadId, Guid token, int? cursorRow = null, int limit = 100)
        => await GetInstructionsAsync(uploadId, token, cursorRow, limit);
}
