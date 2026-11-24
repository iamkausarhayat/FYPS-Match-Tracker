using System.Diagnostics;
using System.Text.RegularExpressions;
using FypTracker.Data;
using FypTracker.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FypTracker.Services
{
    // Live search ka nateeja: kaun si site chali, kaun si nahi, kitne naye project mile
    public class LiveSearchResult
    {
        public bool Skipped { get; set; }
        public int NewProjects { get; set; }
        public int LeftOut { get; set; }
        public List<string> Reached { get; } = new();
        public List<string> Failed { get; } = new();

        // Student ko result page par dikhane wala chhota paigham (null = kuch kehna nahi)
        public string? Note
        {
            get
            {
                if (Skipped || (Reached.Count == 0 && Failed.Count == 0))
                {
                    return null;
                }

                var parts = new List<string>();

                if (Reached.Count > 0)
                {
                    string sites = string.Join(", ", Reached);
                    parts.Add(NewProjects > 0
                        ? $"Live search checked {sites} and added {NewProjects} new project{(NewProjects == 1 ? "" : "s")} to the comparison."
                        : $"Live search checked {sites} and found nothing new.");
                }

                if (Failed.Count > 0)
                {
                    parts.Add($"{string.Join(", ", Failed)} could not be reached just now, so only projects we saved earlier were used from there.");
                }

                if (LeftOut > 0)
                {
                    parts.Add("A few more candidates were left out to keep this check fast. They may be added next time.");
                }

                return string.Join(" ", parts);
            }
        }
    }

    // Student ke "Analyze" dabane par: pehle universities (DSpace) se naye candidates mangwata hai,
    // unke fingerprint bana kar database (cache) mein daalta hai. Phir SimilarityEngine sab se compare karta hai.
    // Ye kabhi exception bahar nahi phenkta: live search fail ho to bhi check chalta rehta hai.
    public class LiveSearchService
    {
        // Doosri universities ke projects ka department pata nahi hota, is liye ye (migration se pehle se maujood hai)
        private const string FallbackDepartmentName = "Other / Not specified";

        private readonly AppDbContext _db;
        private readonly DSpaceClient _client;
        private readonly MeaningScorer _scorer;
        private readonly IMemoryCache _cache;
        private readonly LiveSearchOptions _options;
        private readonly ILogger<LiveSearchService> _logger;

        public LiveSearchService(
            AppDbContext db,
            DSpaceClient client,
            MeaningScorer scorer,
            IMemoryCache cache,
            IOptions<LiveSearchOptions> options,
            ILogger<LiveSearchService> logger)
        {
            _db = db;
            _client = client;
            _scorer = scorer;
            _cache = cache;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<LiveSearchResult> TopUpAsync(string title, string description)
        {
            var result = new LiveSearchResult();

            if (!_options.Enabled)
            {
                result.Skipped = true;
                return result;
            }

            try
            {
                await RunAsync(title, description, result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Live search stopped because of an error");
            }

            return result;
        }

        private async Task RunAsync(string title, string description, LiveSearchResult result)
        {
            // 1. Chhoti keyword queries
            var queries = KeywordQueryBuilder.Build(title, description, _options.MaxQueries);

            var universities = await _db.Universities
                .AsNoTracking()
                .Where(u => u.SourceType == Entities.UniversitySourceType.DSpace && u.ApiBaseUrl != null)
                .ToListAsync();

            if (queries.Count == 0 || universities.Count == 0)
            {
                result.Skipped = true;
                return;
            }

            // 2. Sab universities se ek saath mangwao (kul 10 second ki hadd)
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.FetchTimeoutSeconds));

            var fetched = await Task.WhenAll(universities.Select(u => FetchAsync(u, queries, timeout.Token)));

            var candidates = new List<Candidate>();

            foreach (var (university, items, ok) in fetched)
            {
                if (ok)
                {
                    result.Reached.Add(university.ShortName);
                }
                else
                {
                    result.Failed.Add(university.ShortName);
                }

                foreach (var item in items)
                {
                    var candidate = ToCandidate(university, item);
                    if (candidate != null)
                    {
                        candidates.Add(candidate);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            // 3. Jo pehle se database mein hain unhein nikal do (uuid se, aur same content se)
            var uuids = candidates.Select(c => c.Item.Uuid).Distinct().ToList();
            var hashes = candidates.Select(c => c.Hash).Distinct().ToList();

            var knownIds = (await _db.FypProjects
                    .AsNoTracking()
                    .Where(p => p.ExternalId != null && uuids.Contains(p.ExternalId))
                    .Select(p => new { p.UniversityId, p.ExternalId })
                    .ToListAsync())
                .Select(p => $"{p.UniversityId}|{p.ExternalId}")
                .ToHashSet();

            var knownHashes = (await _db.FypProjects
                    .AsNoTracking()
                    .Where(p => p.ContentHash != null && hashes.Contains(p.ContentHash))
                    .Select(p => new { p.UniversityId, p.ContentHash })
                    .ToListAsync())
                .Select(p => $"{p.UniversityId}|{p.ContentHash}")
                .ToHashSet();

            var fresh = new List<Candidate>();

            foreach (var candidate in candidates)
            {
                if (knownIds.Contains($"{candidate.University.Id}|{candidate.Item.Uuid}"))
                {
                    continue;
                }

                // Same university ka same project (jaise COMSATS ka 2021 aur 2024 wala) sirf ek bar
                if (!knownHashes.Add($"{candidate.University.Id}|{candidate.Hash}"))
                {
                    continue;
                }

                fresh.Add(candidate);
            }

            if (fresh.Count == 0)
            {
                return;
            }

            // 4. Fingerprint banana dheema hai, is liye pehle un ko jo student ke lafzon se zyada milte hain
            string studentText = $"{title} {description}";

            var ranked = fresh
                .OrderByDescending(c => WordSimilarity.Compare(studentText, $"{c.Title} {c.Abstract}").Score)
                .ToList();

            var chosen = ranked.Take(_options.MaxNewProjectsPerCheck).ToList();
            result.LeftOut += ranked.Count - chosen.Count;

            int departmentId = await _db.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name == FallbackDepartmentName ? 0 : 1)
                .ThenBy(d => d.Id)
                .Select(d => d.Id)
                .FirstOrDefaultAsync();

            if (departmentId == 0)
            {
                return;
            }

            // 5. Fingerprint (time ki hadd ke andar) aur save
            var stopwatch = Stopwatch.StartNew();
            var budget = TimeSpan.FromSeconds(_options.EmbedBudgetSeconds);
            int added = 0;

            for (int i = 0; i < chosen.Count; i++)
            {
                if (stopwatch.Elapsed > budget)
                {
                    result.LeftOut += chosen.Count - i;
                    break;
                }

                var candidate = chosen[i];

                TextFingerprint fingerprint;
                try
                {
                    fingerprint = await _scorer.CreateFingerprintAsync(candidate.Title, candidate.Abstract);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not create a fingerprint for live project {Uuid}", candidate.Item.Uuid);
                    continue;
                }

                _db.FypProjects.Add(ToEntity(candidate, departmentId, fingerprint));
                added++;
            }

            if (added == 0)
            {
                return;
            }

            try
            {
                await _db.SaveChangesAsync();
                result.NewProjects = added;
            }
            catch (Exception ex)
            {
                // Jaise do students ne ek hi waqt wahi project save karna chaha. Ye check ko nahi rokta.
                _logger.LogWarning(ex, "Could not save the live projects (maybe another check saved them first)");

                // Warna ye adhoore records agli SaveChanges (engine ki) ko bhi tora denge
                foreach (var entry in _db.ChangeTracker.Entries<Entities.FypProject>()
                             .Where(e => e.State == EntityState.Added)
                             .ToList())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }

        // Ek university se saari queries chalata hai. Ok = kam az kam ek query chali.
        private async Task<(Entities.University University, List<DSpaceItem> Items, bool Ok)> FetchAsync(
            Entities.University university, List<string> queries, CancellationToken ct)
        {
            var items = new Dictionary<string, DSpaceItem>();
            bool anySuccess = false;

            foreach (var query in queries)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                string cacheKey = $"dspace|{university.Id}|{query}";

                if (!_cache.TryGetValue(cacheKey, out List<DSpaceItem>? found) || found == null)
                {
                    try
                    {
                        found = await _client.SearchAsync(university.ApiBaseUrl!, query, _options.ResultsPerQuery, ct);
                        _cache.Set(cacheKey, found, TimeSpan.FromHours(_options.CacheHours));
                        _logger.LogInformation("Live search {University} '{Query}': {Count} items", university.ShortName, query, found.Count);
                    }
                    catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException)
                    {
                        _logger.LogWarning("Live search {University} '{Query}' failed: {Reason}", university.ShortName, query, ex.Message);
                        continue;
                    }
                }

                anySuccess = true;

                foreach (var item in found)
                {
                    items.TryAdd(item.Uuid, item);
                }
            }

            return (university, items.Values.ToList(), anySuccess);
        }

        // ---------- helpers ----------

        private record Candidate(Entities.University University, DSpaceItem Item, string Title, string Abstract, int Year, string Hash);

        private static Candidate? ToCandidate(Entities.University university, DSpaceItem item)
        {
            string title = Regex.Replace(item.Title ?? string.Empty, @"\s+", " ").Trim();
            if (title.Length < 5)
            {
                return null;
            }

            if (title.Length > 300)
            {
                title = title.Substring(0, 300);
            }

            var yearMatch = Regex.Match(item.DateIssued ?? string.Empty, @"(19|20)\d{2}");
            if (!yearMatch.Success || !int.TryParse(yearMatch.Value, out int year) || year < 1990 || year > 2035)
            {
                return null;   // saal ke bina project save nahi hota
            }

            string text = (item.Abstract ?? string.Empty).Trim();
            if (text.Length > 4000)
            {
                text = text.Substring(0, 4000);
            }

            return new Candidate(university, item, title, text, year, ContentHasher.Create(title, text));
        }

        private static Entities.FypProject ToEntity(Candidate c, int departmentId, TextFingerprint fingerprint)
        {
            var university = c.University;

            // "Open original" ka link har site ke item ki uuid se banta hai
            string site = !string.IsNullOrWhiteSpace(university.Website)
                ? university.Website!.TrimEnd('/')
                : Regex.Replace(university.ApiBaseUrl!.TrimEnd('/'), "/server$", string.Empty);

            // Supervisor sirf tab jab saaf likha ho ("Supervised by: Bilal Hassan", UMT jaisa)
            string supervisor = string.Empty;
            foreach (var line in c.Item.Descriptions)
            {
                var match = Regex.Match(line, @"Supervis(?:ed|or)\s*(?:by)?\s*[:\-]\s*(?<name>[^\r\n;]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    supervisor = Cut(match.Groups["name"].Value.Trim().TrimEnd('.'), 100);
                    break;
                }
            }

            // Authors: "(Bs in computer science)" jaisa bracket wala hissa hata do
            string authors = string.Join("; ", c.Item.Authors
                .Select(a => Regex.Replace(a, @"\s*\([^)]*\)\s*", " ").Trim())
                .Where(a => a.Length > 0));

            // Area: pehla subject jo "BS Thesis" jaisa label nahi
            string? area = c.Item.Subjects.FirstOrDefault(s =>
                !Regex.IsMatch(s, @"\b(thesis|dissertation|project|bs|ms|mphil|phd)\b", RegexOptions.IgnoreCase));

            bool isFull = c.Abstract.Length >= 40;

            return new Entities.FypProject
            {
                Title = c.Title,
                Description = c.Abstract,
                SupervisorName = supervisor,
                Year = c.Year,
                CreatedAt = DateTime.UtcNow,
                EmbeddingJson = FingerprintSerializer.Serialize(fingerprint),
                EmbeddingModel = SimilarityRules.EmbeddingModelName,
                SourceUrl = Cut($"{site}/items/{c.Item.Uuid}", 500),
                ExternalId = Cut(c.Item.Uuid, 100),
                AuthorsText = authors.Length == 0 ? null : Cut(authors, 300),
                Area = area == null ? null : Cut(area.Trim(), 100),
                DataQuality = isFull ? Entities.DataQuality.Full : Entities.DataQuality.TitleOnly,
                ContentHash = c.Hash,
                LastFetchedAt = DateTime.UtcNow,
                DepartmentId = departmentId,
                UniversityId = university.Id
            };
        }

        private static string Cut(string value, int maxLength) =>
            value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}