using FypTracker.Data;
using FypTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FypTracker.Services
{
    // Browse page aur home page ke liye projects dhoondta hai.
    // Filter database mein hi lagta hai (poora data memory mein nahi aata), aur ek page mein 12 projects.
    public class BrowseService
    {
        private readonly AppDbContext _db;

        public BrowseService(AppDbContext db)
        {
            _db = db;
        }

        // ---------- Browse page ----------
        public async Task<BrowseViewModel> SearchAsync(
            string? query, string? university, int? year, string? area, bool awardsOnly, int page)
        {
            var model = new BrowseViewModel
            {
                Query = Clean(query, 100),
                University = Clean(university, 30),
                Year = year,
                Area = Clean(area, 100),
                AwardsOnly = awardsOnly
            };

            var projects = _db.FypProjects.AsNoTracking().AsQueryable();

            // Search: har lafz title, description, area ya authors mein hona chahiye (zyada se zyada 5 lafz)
            if (model.Query != null)
            {
                var words = model.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                       .Take(5);

                foreach (var w in words)
                {
                    var word = w;
                    projects = projects.Where(p =>
                        p.Title.Contains(word) ||
                        p.Description.Contains(word) ||
                        (p.Area != null && p.Area.Contains(word)) ||
                        (p.AuthorsText != null && p.AuthorsText.Contains(word)));
                }
            }

            if (model.University != null)
            {
                var uni = model.University;
                projects = projects.Where(p => p.University.ShortName == uni);
            }

            if (model.Year.HasValue)
            {
                var y = model.Year.Value;
                projects = projects.Where(p => p.Year == y);
            }

            if (model.Area != null)
            {
                var a = model.Area;
                projects = projects.Where(p => p.Area == a);
            }

            if (model.AwardsOnly)
            {
                projects = projects.Where(p => p.IsAward);
            }

            model.Total = await projects.CountAsync();
            model.TotalPages = Math.Max(1, (int)Math.Ceiling(model.Total / (double)BrowseViewModel.PageSize));
            model.Page = Math.Clamp(page, 1, model.TotalPages);

            model.Items = await ToItems(projects
                    .OrderByDescending(p => p.Year)
                    .ThenBy(p => p.Title)
                    .Skip((model.Page - 1) * BrowseViewModel.PageSize)
                    .Take(BrowseViewModel.PageSize))
                .ToListAsync();

            // Filter box ki choices (poore database se, filter se alag)
            model.Universities = (await _db.FypProjects
                    .GroupBy(p => p.University.ShortName)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .ToListAsync())
                .Select(g => new BrowseFacet(g.Name, g.Count))
                .OrderBy(f => f.Name)
                .ToList();

            model.Years = await _db.FypProjects
                .Select(p => p.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            model.Areas = (await _db.FypProjects
                    .Where(p => p.Area != null && p.Area != "")
                    .GroupBy(p => p.Area)
                    .Select(g => new { Name = g.Key ?? string.Empty, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .Take(40)
                    .ToListAsync())
                .Select(g => new BrowseFacet(g.Name, g.Count))
                .OrderBy(f => f.Name)
                .ToList();

            return model;
        }

        // ---------- Home page: top FYPs ----------
        // Award wale projects. Agar abhi koi award wala nahi, to sab se naye projects (AreAwards = false).
        public async Task<(List<BrowseItem> Items, bool AreAwards)> GetTopAsync(int take)
        {
            var awards = await ToItems(_db.FypProjects
                    .AsNoTracking()
                    .Where(p => p.IsAward)
                    .OrderByDescending(p => p.Year)
                    .ThenBy(p => p.Title)
                    .Take(take))
                .ToListAsync();

            if (awards.Count > 0)
            {
                return (awards, true);
            }

            var latest = await ToItems(_db.FypProjects
                    .AsNoTracking()
                    .Where(p => p.DataQuality == Entities.DataQuality.Full)
                    .OrderByDescending(p => p.Year)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(take))
                .ToListAsync();

            return (latest, false);
        }

        // Kitni universities ke projects hamare paas hain
        public Task<int> CountUniversitiesAsync()
        {
            return _db.Universities.CountAsync(u => u.Projects.Any());
        }

        // ---------- helpers ----------

        // Bari EmbeddingJson load nahi hoti, sirf card ke columns aate hain
        private static IQueryable<BrowseItem> ToItems(IQueryable<Entities.FypProject> projects)
        {
            return projects.Select(p => new BrowseItem
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                University = p.University.ShortName,
                UniversityName = p.University.Name,
                Year = p.Year,
                Area = p.Area,
                Supervisor = p.SupervisorName,
                Authors = p.AuthorsText,
                SourceUrl = p.SourceUrl,
                IsAward = p.IsAward,
                AwardText = p.AwardText,
                IsTitleOnly = p.DataQuality == Entities.DataQuality.TitleOnly
            });
        }

        private static string? Clean(string? value, int maxLength)
        {
            var trimmed = (value ?? string.Empty).Trim();

            if (trimmed.Length == 0)
            {
                return null;
            }

            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }
    }
}