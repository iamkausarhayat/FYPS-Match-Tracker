using System.Text.Json;

namespace FypTracker.Services
{
    // DSpace repository se aaya hua ek item (project ya thesis), jaisa website ne bheja
    public record DSpaceItem(
        string Uuid,
        string Title,
        string Abstract,
        List<string> Authors,
        string DateIssued,
        List<string> Descriptions,   // dc.description: UMT mein yahan "Supervised by: ..." likha hota hai
        List<string> Subjects);

    // DSpace 7/8 ki public search API se baat karta hai (UMT, COMSATS Lahore, Superior teeno ek jaisi hain).
    // Sirf padhta hai: login ya key ki zaroorat nahi.
    public class DSpaceClient
    {
        private readonly HttpClient _http;

        public DSpaceClient(HttpClient http)
        {
            _http = http;
        }

        // apiBaseUrl jaise "https://escholar.umt.edu.pk/server"
        public async Task<List<DSpaceItem>> SearchAsync(string apiBaseUrl, string query, int size, CancellationToken ct)
        {
            string url = $"{apiBaseUrl.TrimEnd('/')}/api/discover/search/objects?query={Uri.EscapeDataString(query)}&size={size}";

            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            return Parse(document.RootElement);
        }

        // JSON ka raasta: _embedded -> searchResult -> _embedded -> objects[] -> _embedded -> indexableObject
        private static List<DSpaceItem> Parse(JsonElement root)
        {
            var items = new List<DSpaceItem>();

            if (!TryPath(root, out var objects, "_embedded", "searchResult", "_embedded", "objects")
                || objects.ValueKind != JsonValueKind.Array)
            {
                return items;
            }

            foreach (var entry in objects.EnumerateArray())
            {
                if (!TryPath(entry, out var item, "_embedded", "indexableObject"))
                {
                    continue;
                }

                // Sirf items (collections ya communities nahi)
                string? type = Text(item, "type");
                if (type != null && !type.Equals("item", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? uuid = Text(item, "uuid") ?? Text(item, "id");
                if (string.IsNullOrWhiteSpace(uuid))
                {
                    continue;
                }

                JsonElement metadata = default;
                bool hasMetadata = item.TryGetProperty("metadata", out metadata) && metadata.ValueKind == JsonValueKind.Object;

                List<string> Values(string key)
                {
                    var list = new List<string>();

                    if (hasMetadata
                        && metadata.TryGetProperty(key, out var array)
                        && array.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var element in array.EnumerateArray())
                        {
                            string? value = Text(element, "value");
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                list.Add(value.Trim());
                            }
                        }
                    }

                    return list;
                }

                string title = Values("dc.title").FirstOrDefault() ?? Text(item, "name") ?? string.Empty;

                items.Add(new DSpaceItem(
                    uuid,
                    title,
                    Values("dc.description.abstract").FirstOrDefault() ?? string.Empty,
                    Values("dc.contributor.author"),
                    Values("dc.date.issued").FirstOrDefault() ?? string.Empty,
                    Values("dc.description"),
                    Values("dc.subject")));
            }

            return items;
        }

        // Kai level andar tak naam ke saath jana. Raasta toot jaye to false.
        private static bool TryPath(JsonElement start, out JsonElement result, params string[] names)
        {
            result = start;

            foreach (var name in names)
            {
                if (result.ValueKind != JsonValueKind.Object || !result.TryGetProperty(name, out var next))
                {
                    return false;
                }

                result = next;
            }

            return true;
        }

        private static string? Text(JsonElement element, string name)
        {
            return element.ValueKind == JsonValueKind.Object
                   && element.TryGetProperty(name, out var value)
                   && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }
}