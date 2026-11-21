using FypTracker.Data;
using FypTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FypTracker.Services
{
    // Ek idea ko NUML ke saare FYP projects se compare karta hai,
    // result ko database mein save karta hai, aur wapis load bhi karta hai.
    public class SimilarityEngine
    {
        private const int MaxMatches = 5;

        private readonly AppDbContext _db;
        private readonly MeaningScorer _scorer;

        public SimilarityEngine(AppDbContext db, MeaningScorer scorer)
        {
            _db = db;
            _scorer = scorer;
        }

        // ---------- 1. Compare ----------
        public async Task<SimilarityResultViewModel> CheckAsync(
            string title, string description, string supervisorName, string fileName)
        {
            // Student ke idea ka naya fingerprint (AI model use hota hai)
            var student = await _scorer.CreateFingerprintAsync(title, description);
            var studentText = $"{title} {description}";

            var projects = await _db.FypProjects.Include(p => p.Department).ToListAsync();

            bool changed = false;
            var matches = new List<MatchItemViewModel>();

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

                var meaning = MeaningScorer.Compare(student, fingerprint);
                var words = WordSimilarity.Compare(studentText, $"{project.Title} {project.Description}");

                matches.Add(new MatchItemViewModel
                {
                    ProjectId = project.Id,
                    Title = project.Title,
                    Detail = $"{project.Department.Name} • {project.Year} • Supervisor: {project.SupervisorName}",
                    MeaningScore = meaning.Score,
                    WordScore = words.Score,
                    CommonKeywords = words.CommonKeywords,
                    BestPassage = meaning.BestPassage
                });
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
                DepartmentMatches = matches
                    .OrderByDescending(m => m.FinalScore)
                    .Take(MaxMatches)
                    .ToList(),
                PaperMatches = new List<MatchItemViewModel>()   // papers ka percentage nahi nikalta
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
                UserId = userId
            };

            foreach (var match in result.DepartmentMatches.Where(m => m.ProjectId != null))
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
                DepartmentMatches = check.Matches
                    .Where(m => m.FypProject != null)
                    .Select(m => new MatchItemViewModel
                    {
                        ProjectId = m.FypProjectId,
                        Title = m.FypProject!.Title,
                        Detail = $"{m.FypProject.Department.Name} • {m.FypProject.Year} • Supervisor: {m.FypProject.SupervisorName}",
                        MeaningScore = m.MeaningScore,
                        WordScore = m.WordScore,
                        CommonKeywords = m.CommonKeywords
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .ToList(),
                        BestPassage = m.BestPassage
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

        private static string Cut(string? value, int maxLength)
        {
            value ??= string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}