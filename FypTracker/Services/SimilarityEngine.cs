using System.Text.Json;
using FypTracker.Data;
using FypTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FypTracker.Services
{
    // Ek idea ko tamam universities ke saved projects se compare karta hai,
    // result ko database mein save karta hai, aur wapis load bhi karta hai.
    public class SimilarityEngine
    {
        private readonly AppDbContext _db;
        private readonly MeaningScorer _scorer;
        private readonly ILogger<SimilarityEngine> _logger;

        public SimilarityEngine(AppDbContext db, MeaningScorer scorer, ILogger<SimilarityEngine> logger)
        {
            _db = db;
            _scorer = scorer;
            _logger = logger;
        }

        // ---------- 1. Compare ----------
        public async Task<SimilarityResultViewModel> CheckAsync(
            string title, string description, string supervisorName, string fileName)
        {
            // Student ke idea ka naya fingerprint (AI model use hota hai)
            var student = await _scorer.CreateFingerprintAsync(title, description);
            var studentText = $"{title} {description}";

            var projects = await _db.FypProjects
                .Include(p => p.Department)
                .Include(p => p.University)
                .ToListAsync();

            bool changed = false;

            // Same project do bar aaye (jaise COMSATS ka 2021 aur 2024 wala) to sirf ek gina jata hai.
            // Key = ContentHash. Hash khali ho to usi waqt bana kar save kar dete hain.
            var bestByKey = new Dictionary<string, MatchItemViewModel>();
            var universityIds = new HashSet<int>();

            foreach (var project in projects)
            {
                var fingerprint = FingerprintSerializer.Deserialize(project.EmbeddingJson);

                // Fingerprint nahi hai (ya model badal gaya) to abhi bana kar save kar do
                if (fingerprint == null || project.EmbeddingModel != SimilarityRules.EmbeddingModelName)
                {
                    fingerprint = await _scorer.CreateFingerprintAsync(project.Title, project.Description);
                    project.EmbeddingJson = FingerprintSerializer.Serialize(fingerprint);
                    project.EmbeddingModel = SimilarityRules.EmbeddingModelName;
                    changed = true;
                }

                if (string.IsNullOrEmpty(project.ContentHash))
                {
                    project.ContentHash = ContentHasher.Create(project.Title, project.Description);
                    changed = true;
                }

                var meaning = MeaningScorer.Compare(student, fingerprint);
                var words = WordSimilarity.Compare(studentText, $"{project.Title} {project.Description}");

                var match = ToMatch(project);
                match.MeaningScore = meaning.Score;
                match.WordScore = words.Score;
                match.CommonKeywords = words.CommonKeywords;
                match.BestPassage = meaning.BestPassage;

                // Tuning ke liye: Output window mein har project ke raw numbers dikhte hain
                if (match.FinalScore >= 10)
                {
                    _logger.LogInformation(
                        "SCORE {Title} | raw={Raw:F3} title={T:F3} cover={C:F3} bestPair={B:F3} | meaning={M} words={W} final={F}",
                        project.Title.Length > 50 ? project.Title[..50] : project.Title,
                        meaning.RawCosine, meaning.TitleCosine, meaning.CoverageCosine, meaning.BestPairCosine,
                        meaning.Score, words.Score, match.FinalScore);
                }

                universityIds.Add(project.UniversityId);

                // Duplicate mile to jis ka score zyada ho wo rakho
                if (!bestByKey.TryGetValue(project.ContentHash!, out var existing)
                    || match.FinalScore > existing.FinalScore)
                {
                    bestByKey[project.ContentHash!] = match;
                }
            }

            if (changed)
            {
                await _db.SaveChangesAsync();
            }

            return new SimilarityResultViewModel
            {
                Title = title,
                SupervisorName = supervisorName,
                FileName = fileName,
                ProjectMatches = bestByKey.Values
                    .OrderByDescending(m => m.FinalScore)
                    .Take(SimilarityRules.MaxStoredMatches)
                    .ToList(),
                PaperMatches = new List<MatchItemViewModel>(),   // papers ka percentage nahi nikalta
                ComparedProjectCount = bestByKey.Count,
                ComparedUniversityCount = universityIds.Count
            };
        }

        // ---------- 2. Save ----------
        // Check, uske matches aur research papers database mein likhta hai. Naye check ki Id wapas deta hai.
        public async Task<int> SaveAsync(SimilarityResultViewModel result, string description, int? userId)
        {
            // Agar cookie kisi aise user ki hai jo database se hat chuka, to guest samjho
            if (userId != null && !await _db.Users.AnyAsync(u => u.Id == userId))
            {
                userId = null;
            }

            var check = new Entities.SimilarityCheck
            {
                Title = Cut(result.Title, 150),
                Description = Cut(description, 3000),
                SupervisorName = Cut(result.SupervisorName, 60),
                FileName = Cut(result.FileName, 260),
                OverallScore = result.OverallScore,
                Verdict = result.Verdict,
                CheckedAt = DateTime.UtcNow,
                ComparedProjectCount = result.ComparedProjectCount,
                ComparedUniversityCount = result.ComparedUniversityCount,
                SuggestionJson = result.Suggestion == null ? null : JsonSerializer.Serialize(result.Suggestion),
                UserId = userId
            };

            foreach (var match in result.ProjectMatches.Where(m => m.ProjectId != null))
            {
                check.Matches.Add(new Entities.SimilarityMatch
                {
                    Source = Entities.MatchSource.DepartmentProject,
                    MeaningScore = match.MeaningScore,
                    WordScore = match.WordScore,
                    FinalScore = match.FinalScore,
                    CommonKeywords = Cut(string.Join(", ", match.CommonKeywords), 500),
                    BestPassage = Cut(match.BestPassage, 1000),
                    FypProjectId = match.ProjectId
                });
            }

            // Research papers: ExternalPapers table mein save karo (ek DOI sirf ek dafa),
            // phir har paper ko is check se jodo
            var seenDois = new HashSet<string>();

            for (int i = 0; i < result.Papers.Count; i++)
            {
                var paper = result.Papers[i];
                var doi = Cut(paper.Doi.Trim().ToLowerInvariant(), 100);

                // khali DOI ya is check mein pehle aa chuka DOI chhod do
                if (doi.Length == 0 || !seenDois.Add(doi))
                {
                    continue;
                }

                var entity = await _db.ExternalPapers.FirstOrDefaultAsync(p => p.ExternalId == doi);

                if (entity == null)
                {
                    entity = new Entities.ExternalPaper
                    {
                        ExternalId = doi,
                        Title = Cut(paper.Title, 500),
                        PublicationYear = paper.Year,
                        Url = Cut(paper.Url, 500),
                        Authors = Cut(paper.Authors, 300),
                        FetchedAt = DateTime.UtcNow
                    };

                    _db.ExternalPapers.Add(entity);
                }

                check.Matches.Add(new Entities.SimilarityMatch
                {
                    Source = Entities.MatchSource.ResearchPaper,
                    ExternalPaper = entity
                });
            }

            _db.SimilarityChecks.Add(check);
            await _db.SaveChangesAsync();
            return check.Id;
        }

        // ---------- 3. Load ----------
        // Save kiya hua check wapis Result page ke format mein. Nahi mila to null.
        public async Task<(SimilarityResultViewModel Result, int? OwnerId)?> LoadAsync(int id)
        {
            var check = await _db.SimilarityChecks
                .AsNoTracking()
                .Include(c => c.Matches)
                    .ThenInclude(m => m.FypProject)
                        .ThenInclude(p => p!.Department)
                .Include(c => c.Matches)
                    .ThenInclude(m => m.FypProject)
                        .ThenInclude(p => p!.University)
                .Include(c => c.Matches)
                    .ThenInclude(m => m.ExternalPaper)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (check == null)
            {
                return null;
            }

            var result = new SimilarityResultViewModel
            {
                Title = check.Title,
                SupervisorName = check.SupervisorName,
                FileName = check.FileName ?? string.Empty,
                ComparedProjectCount = check.ComparedProjectCount,
                ComparedUniversityCount = check.ComparedUniversityCount,
                Suggestion = ReadSuggestion(check.SuggestionJson),
                ProjectMatches = check.Matches
                    .Where(m => m.FypProject != null)
                    .Select(m =>
                    {
                        var item = ToMatch(m.FypProject!);
                        item.MeaningScore = m.MeaningScore;
                        item.WordScore = m.WordScore;
                        item.CommonKeywords = m.CommonKeywords
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList();
                        item.BestPassage = m.BestPassage;
                        return item;
                    })
                    .OrderByDescending(m => m.FinalScore)
                    .ToList(),
                Papers = check.Matches
                    .Where(m => m.ExternalPaper != null)
                    .OrderBy(m => m.Id)
                    .Select(m => new PaperItemViewModel
                    {
                        Doi = m.ExternalPaper!.ExternalId,
                        Title = m.ExternalPaper.Title,
                        Authors = m.ExternalPaper.Authors ?? string.Empty,
                        Year = m.ExternalPaper.PublicationYear,
                        Url = m.ExternalPaper.Url ?? string.Empty
                    })
                    .ToList()
            };

            return (result, check.UserId);
        }

        // ---------- helpers ----------

        // Saved JSON -> SuggestionViewModel. Empty or broken text = null (the controller then makes a template one).
        private static SuggestionViewModel? ReadSuggestion(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<SuggestionViewModel>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // Database project -> result page card (scores baad mein bharte hain).
        // Project ke saath Department aur University load honi chahiye.
        private static MatchItemViewModel ToMatch(Entities.FypProject p)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(p.University?.ShortName)) parts.Add(p.University.ShortName);
            if (!string.IsNullOrWhiteSpace(p.Department?.Name)) parts.Add(p.Department.Name);
            parts.Add(p.Year.ToString());
            if (!string.IsNullOrWhiteSpace(p.SupervisorName)) parts.Add("Supervisor: " + p.SupervisorName);

            return new MatchItemViewModel
            {
                ProjectId = p.Id,
                Title = p.Title,
                Detail = string.Join(" • ", parts),
                UniversityName = p.University?.Name ?? string.Empty,
                UniversityShort = p.University?.ShortName ?? string.Empty,
                Year = p.Year,
                Area = p.Area ?? p.Department?.Name ?? string.Empty,
                Supervisor = p.SupervisorName ?? string.Empty,
                Authors = p.AuthorsText ?? string.Empty,
                SourceUrl = SafeUrl(p.SourceUrl),
                IsLowConfidence = p.DataQuality == Entities.DataQuality.TitleOnly
            };
        }

        // Sirf http / https link aage jaata hai (javascript: jaise khatarnak links nahi)
        private static string? SafeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            bool ok = Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
                      && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            return ok ? uri!.AbsoluteUri : null;
        }

        private static string Cut(string? value, int maxLength)
        {
            value ??= string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}