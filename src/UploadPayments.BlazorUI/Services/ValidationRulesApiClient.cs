using System.Net.Http.Json;
using UploadPayments.Contracts;

namespace UploadPayments.BlazorUI.Services;

public sealed class ValidationRulesApiClient(IHttpClientFactory httpClientFactory)
{
    private HttpClient CreateClient() => httpClientFactory.CreateClient("PaymentUploadsApi");

    public async Task<ValidationRulesPageDto?> GetRulesAsync(int offset = 0, int limit = 50, bool? enabled = null)
    {
        using var client = CreateClient();
        var url = $"/api/validation-rules?offset={offset}&limit={limit}";
        if (enabled.HasValue)
            url += $"&enabled={enabled.Value}";
        
        return await client.GetFromJsonAsync<ValidationRulesPageDto>(url);
    }

    public async Task<ValidationRuleDto?> GetRuleAsync(Guid id)
    {
        using var client = CreateClient();
        return await client.GetFromJsonAsync<ValidationRuleDto>($"/api/validation-rules/{id}");
    }

    public async Task<ValidationRuleDto?> CreateRuleAsync(CreateValidationRuleDto dto)
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/validation-rules", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ValidationRuleDto>();
    }

    public async Task UpdateRuleAsync(Guid id, UpdateValidationRuleDto dto)
    {
        using var client = CreateClient();
        var response = await client.PutAsJsonAsync($"/api/validation-rules/{id}", dto);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteRuleAsync(Guid id)
    {
        using var client = CreateClient();
        var response = await client.DeleteAsync($"/api/validation-rules/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task ToggleRuleAsync(Guid id)
    {
        using var client = CreateClient();
        var response = await client.PostAsync($"/api/validation-rules/{id}/toggle", null);
        response.EnsureSuccessStatusCode();
    }
}
