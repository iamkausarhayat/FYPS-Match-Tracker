using FypTracker.Data;
using FypTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FypTracker.Services
{
    // Projects ko SQL Server mein rakhta hai.
    // Add/Edit par project ka "meaning fingerprint" khud banata aur save karta hai.
    public class DbProjectService : IProjectService
    {
        // Description is shorter than this = we treat the project as "title only"
        private const int MinFullDescriptionLength = 40;

        private readonly AppDbContext _db;
        private readonly MeaningScorer _scorer;

        public DbProjectService(AppDbContext db, MeaningScorer scorer)
        {
            _db = db;
            _scorer = scorer;
        }

        public async Task<List<FypProject>> GetAllAsync()
        {
            // Select se sirf zaroori columns aate hain. Bari EmbeddingJson load nahi hoti.
            return await _db.FypProjects
                .AsNoTracking()
                .Select(p => new FypProject
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Department = p.Department.Name,
                    SupervisorName = p.SupervisorName,
                    Year = p.Year,
                    University = p.University.ShortName,
                    SourceUrl = p.SourceUrl,
                    Area = p.Area,
                    IsTitleOnly = p.DataQuality == Entities.DataQuality.TitleOnly,
                    AuthorsText = p.AuthorsText,
                    IsAward = p.IsAward,
                    AwardText = p.AwardText
                })
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _db.FypProjects.CountAsync();
        }

        public async Task<List<string>> GetUniversityNamesAsync()
        {
            return await _db.Universities
                .AsNoTracking()
                .OrderBy(u => u.ShortName)
                .Select(u => u.ShortName)
                .ToListAsync();
        }

        public async Task<bool> TitleExistsAsync(string title, string university, int ignoreId)
        {
            var cleanTitle = (title ?? string.Empty).Trim();
            var uni = (university ?? string.Empty).Trim();

            return await _db.FypProjects.AnyAsync(p =>
                p.Id != ignoreId &&
                p.Title == cleanTitle &&
                (p.University.ShortName == uni || p.University.Name == uni));
        }

        public async Task<FypProject?> GetByIdAsync(int id)
        {
            return await _db.FypProjects
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new FypProject
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    Department = p.Department.Name,
                    SupervisorName = p.SupervisorName,
                    Year = p.Year,
                    University = p.University.ShortName,
                    SourceUrl = p.SourceUrl,
                    Area = p.Area,
                    IsTitleOnly = p.DataQuality == Entities.DataQuality.TitleOnly,
                    AuthorsText = p.AuthorsText,
                    IsAward = p.IsAward,
                    AwardText = p.AwardText
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> AddAsync(FypProject project)
        {
            var department = await _db.Departments
                .FirstOrDefaultAsync(d => d.Name == project.Department);

            if (department == null)
            {
                return false;
            }

            var university = await FindUniversityAsync(project.University);

            if (university == null)
            {
                return false;
            }

            var entity = new Entities.FypProject
            {
                Title = project.Title,
                Description = project.Description,
                SupervisorName = project.SupervisorName,
                Year = project.Year,
                DepartmentId = department.Id,
                UniversityId = university.Id,
                SourceUrl = CleanUrl(project.SourceUrl),
                Area = NullIfEmpty(project.Area, 100),
                AuthorsText = null,
                IsAward = project.IsAward,
                AwardText = project.IsAward ? NullIfEmpty(project.AwardText, 200) : null
            };

            ApplyTextInfo(entity);
            await FillFingerprintAsync(entity);

            _db.FypProjects.Add(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateAsync(FypProject project)
        {
            var entity = await _db.FypProjects.FindAsync(project.Id);
            if (entity == null)
            {
                return false;
            }

            var department = await _db.Departments
                .FirstOrDefaultAsync(d => d.Name == project.Department);

            if (department == null)
            {
                return false;
            }

            // Fingerprint tab dobara banta hai jab title ya description badle
            bool textChanged = entity.Title != project.Title
                            || entity.Description != project.Description
                            || entity.EmbeddingJson == null;

            entity.Title = project.Title;
            entity.Description = project.Description;
            entity.SupervisorName = project.SupervisorName;
            entity.Year = project.Year;
            entity.DepartmentId = department.Id;

            // The admin form now has university, link, area and award boxes, so it owns these values.
            if (!string.IsNullOrWhiteSpace(project.University))
            {
                var university = await FindUniversityAsync(project.University);
                if (university != null)
                {
                    entity.UniversityId = university.Id;
                }
            }

            entity.SourceUrl = CleanUrl(project.SourceUrl);
            entity.Area = NullIfEmpty(project.Area, 100);
            entity.IsAward = project.IsAward;
            entity.AwardText = project.IsAward ? NullIfEmpty(project.AwardText, 200) : null;

            ApplyTextInfo(entity);

            if (textChanged)
            {
                await FillFingerprintAsync(entity);
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _db.FypProjects.FindAsync(id);
            if (entity == null)
            {
                return false;
            }

            _db.FypProjects.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        // ---------- helpers ----------

        // Name ya short name se university dhoondta hai. Naam khali ho to pehli university (Id sab se chhoti).
        private async Task<Entities.University?> FindUniversityAsync(string? nameOrShort)
        {
            var key = (nameOrShort ?? string.Empty).Trim();

            if (key.Length > 0)
            {
                var found = await _db.Universities
                    .FirstOrDefaultAsync(u => u.Name == key || u.ShortName == key);

                if (found != null)
                {
                    return found;
                }
            }

            return await _db.Universities.OrderBy(u => u.Id).FirstOrDefaultAsync();
        }

        private static string? NullIfEmpty(string? value, int maxLength)
        {
            var trimmed = (value ?? string.Empty).Trim();

            if (trimmed.Length == 0)
            {
                return null;
            }

            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }

        // Link sirf tab rakhte hain jab wo sach mein http ya https ho (warna null)
        private static string? CleanUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            var trimmed = url.Trim();

            bool ok = Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
                      && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            return ok && trimmed.Length <= 500 ? trimmed : null;
        }

        // DataQuality aur ContentHash (duplicate pakadne ka nishan) set karta hai
        private static void ApplyTextInfo(Entities.FypProject entity)
        {
            entity.DataQuality = (entity.Description ?? string.Empty).Trim().Length >= MinFullDescriptionLength
                ? Entities.DataQuality.Full
                : Entities.DataQuality.TitleOnly;

            entity.ContentHash = ContentHasher.Create(entity.Title, entity.Description);
        }

        // Title + description se fingerprint bana kar project mein rakh deta hai
        private async Task FillFingerprintAsync(Entities.FypProject entity)
        {
            var fingerprint = await _scorer.CreateFingerprintAsync(entity.Title, entity.Description);

            entity.EmbeddingJson = FingerprintSerializer.Serialize(fingerprint);
            entity.EmbeddingModel = SimilarityRules.EmbeddingModelName;
        }
    }
}