using System.Net.Http.Headers;
using System.Net.Http.Json;
using HttpApiClient;

var httpClient = new HttpClient()
{
    DefaultRequestHeaders =
    {
        UserAgent = { ProductInfoHeaderValue.Parse("MySuperApp") }
    }
};

string? text = Console.ReadLine();

EmployersResponse? result = await httpClient.GetFromJsonAsync<EmployersResponse>(
    $"https://api.hh.ru/employers?text={text}");

IEnumerable<string> enumerable = result.Items.Select(item => item.Name);

Console.WriteLine(string.Join(Environment.NewLine, enumerable));