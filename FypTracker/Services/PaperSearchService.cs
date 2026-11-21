using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FypTracker.Models;

namespace FypTracker.Services
{
    // Search ka jawab: papers ki list, ya (agar masla ho) ek friendly paigham
    public class PaperSearchResult
    {
        public List<PaperItemViewModel> Papers { get; set; } = new();
        public string? Error { get; set; }
    }

    // Crossref (free research-paper API) se idea ke related papers lata hai.
    public class PaperSearchService
    {
        private const int MaxPapers = 5;
        private const int KeywordCount = 6;

        private const string ErrorText =
            "Research papers could not be loaded right now. Your similarity result below is still correct.";

        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<PaperSearchService> _logger;

        // HttpClient Program.cs se milta hai (Dependency Injection)
        public PaperSearchService(HttpClient http, IConfiguration config, ILogger<PaperSearchService> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        public async Task<PaperSearchResult> SearchAsync(string title, string description)
        {
            var result = new PaperSearchResult();

            // Title do dafa likha hai, taake uske alfaz keywords mein zyada wazan paayein
            var keywords = TextCleaner.TopKeywords($"{title} {title} {description}", KeywordCount);

            if (keywords.Count == 0)
            {
                return result;
            }

            try
            {
                using var response = await _http.GetAsync(BuildUrl(keywords));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Crossref returned status {Status}", (int)response.StatusCode);
                    result.Error = ErrorText;
                    return result;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var json = await JsonDocument.ParseAsync(stream);

                result.Papers = ReadPapers(json.RootElement);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                // Internet band, timeout, ya ajeeb JSON: app crash nahi hoti
                _logger.LogWarning(ex, "Crossref search failed");
                result.Error = ErrorText;
            }

            return result;
        }

        // ---------- helpers ----------

        private string BuildUrl(List<string> keywords)
        {
            var query = Uri.EscapeDataString(string.Join(' ', keywords));

            // select: sirf zaroori fields mangte hain, jawab chhota aur tez
            var url = $"works?query.bibliographic={query}&filter=type:journal-article&rows={MaxPapers}&select=DOI,title,author,issued,URL";

            var mailto = _config["Crossref:Mailto"];
            if (!string.IsNullOrWhiteSpace(mailto))
            {
                url += "&mailto=" + Uri.EscapeDataString(mailto.Trim());
            }

            return url;
        }

        // Crossref ka JSON: { "message": { "items": [ {...}, {...} ] } }
        private static List<PaperItemViewModel> ReadPapers(JsonElement root)
        {
            var papers = new List<PaperItemViewModel>();

            if (!root.TryGetProperty("message", out var message) ||
                !message.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return papers;
            }

            for (int i = 0; i < items.GetArrayLength(); i++)
            {
                var item = items[i];

                var doi = ReadString(item, "DOI");
                var title = CleanText(ReadFirstOfArray(item, "title"));

                // DOI ya title na ho to paper kaam ka nahi
                if (doi.Length == 0 || title.Length == 0)
                {
                    continue;
                }

                var url = ReadString(item, "URL");

                papers.Add(new PaperItemViewModel
                {
                    Doi = doi,
                    Title = Cut(title, 500),
                    Authors = ReadAuthors(item),
                    Year = ReadYear(item),
                    Url = url.Length > 0 ? url : "https://doi.org/" + doi
                });
            }

            return papers;
        }

        private static string ReadString(JsonElement item, string name)
        {
            if (item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        // Crossref title ek list hoti hai: ["Paper ka naam"]
        private static string ReadFirstOfArray(JsonElement item, string name)
        {
            if (item.TryGetProperty(name, out var array) &&
                array.ValueKind == JsonValueKind.Array &&
                array.GetArrayLength() > 0 &&
                array[0].ValueKind == JsonValueKind.String)
            {
                return array[0].GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        // Pehle 3 authors, baqi ke liye "et al."
        private static string ReadAuthors(JsonElement item)
        {
            if (!item.TryGetProperty("author", out var authors) || authors.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            var names = new List<string>();
            int shown = Math.Min(3, authors.GetArrayLength());

            for (int i = 0; i < shown; i++)
            {
                var given = ReadString(authors[i], "given");
                var family = ReadString(authors[i], "family");
                var organisation = ReadString(authors[i], "name");   // kuch authors ek idara hote hain

                var fullName = $"{given} {family}".Trim();
                names.Add(CleanText(fullName.Length > 0 ? fullName : organisation));
            }

            names.RemoveAll(name => name.Length == 0);

            var text = string.Join(", ", names);
            return authors.GetArrayLength() > 3 ? text + " et al." : text;
        }

        // "issued": { "date-parts": [[2021, 5, 14]] }  -> 2021
        private static int? ReadYear(JsonElement item)
        {
            if (item.TryGetProperty("issued", out var issued) &&
                issued.TryGetProperty("date-parts", out var parts) &&
                parts.ValueKind == JsonValueKind.Array &&
                parts.GetArrayLength() > 0 &&
                parts[0].ValueKind == JsonValueKind.Array &&
                parts[0].GetArrayLength() > 0 &&
                parts[0][0].ValueKind == JsonValueKind.Number &&
                parts[0][0].TryGetInt32(out var year))
            {
                return year;
            }

            return null;
        }

        // Bahar se aaye text ko saaf karta hai:
        // 1. <i>, <sub> jaise tags hata do
        // 2. &amp; ko & mein badlo (HtmlDecode)
        // 3. faltu spaces hata do
        private static string CleanText(string value)
        {
            var withoutTags = Regex.Replace(value, "<[^>]+>", string.Empty);
            var decoded = WebUtility.HtmlDecode(withoutTags);
            return Regex.Replace(decoded, @"\s+", " ").Trim();
        }

        private static string Cut(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}