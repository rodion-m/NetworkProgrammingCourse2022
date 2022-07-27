using System.Text.Json.Serialization;
#pragma warning disable CS8618

namespace HttpApiClient;

public class EmployersResponse
{
    [JsonPropertyName("items")] public Item[] Items { get; set; }

    [JsonPropertyName("found")] public long Found { get; set; }

    [JsonPropertyName("pages")] public long Pages { get; set; }

    [JsonPropertyName("per_page")] public long PerPage { get; set; }

    [JsonPropertyName("page")] public long Page { get; set; }
}

public class Item
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("url")] public Uri Url { get; set; }

    [JsonPropertyName("alternate_url")] public Uri AlternateUrl { get; set; }

    [JsonPropertyName("logo_urls")] public LogoUrls LogoUrls { get; set; }

    [JsonPropertyName("vacancies_url")] public Uri VacanciesUrl { get; set; }

    [JsonPropertyName("open_vacancies")] public long OpenVacancies { get; set; }
}

public class LogoUrls
{
    [JsonPropertyName("90")] public Uri The90 { get; set; }

    [JsonPropertyName("240")] public Uri The240 { get; set; }

    [JsonPropertyName("original")] public Uri Original { get; set; }
}