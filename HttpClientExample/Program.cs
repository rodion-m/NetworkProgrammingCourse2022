using System.Text;
using System.Text.RegularExpressions;

Console.OutputEncoding = Encoding.UTF8;

var url = Console.ReadLine();
if (string.IsNullOrWhiteSpace(url))
    url = "https://proglib.io/vacancies/all";

var pattern = "<h2 class=\"preview-card__title\" itemprop=\"title\">(.+)<\\/h2>";
Regex regex = new Regex(pattern);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
using HttpClient client = new HttpClient();
using HttpResponseMessage response = await client.GetAsync(url);
string htmlBody = await response.Content.ReadAsStringAsync();
MatchCollection matches = regex.Matches(htmlBody);
Console.WriteLine($"Кол-во совпадений: {matches.Count}");
foreach (Match match in matches)
{
    //match.Value == match.Groups[0] == $&
    //match.Groups[1] == $1
    Console.WriteLine(match.Groups[1].Value);
}


Console.WriteLine("Code: " + (int) response.StatusCode);