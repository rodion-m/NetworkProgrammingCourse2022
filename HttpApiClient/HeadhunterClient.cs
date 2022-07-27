using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HttpApiClient;

public class HeadhunterClient
{
    private readonly HttpClient _httpClient;

    public HeadhunterClient()
    {
        _httpClient = new HttpClient()
        {
            DefaultRequestHeaders =
            {
                UserAgent = { ProductInfoHeaderValue.Parse("MySuperApp") }
            }
        };
    }

    public async Task<EmployersResponse> GetEmployersAsync(string? text)
    {
        var result = await _httpClient.GetFromJsonAsync<EmployersResponse>(
            $"https://api.hh.ru/employers?text={text}")
            .ConfigureAwait(false);
        return result!;
    }
}