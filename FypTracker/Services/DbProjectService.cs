using FypTracker.Data;
using FypTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace FypTracker.Services
{
    // Projects ko SQL Server mein rakhta hai.
    // Add/Edit par project ka "meaning fingerprint" khud banata aur save karta hai.
    public class DbProjectService : IProjectService
    {
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
                    Year = p.Year
                })
                .ToListAsync();
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
                    Year = p.Year
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

            var entity = new Entities.FypProject
            {
                Title = project.Title,
                Description = project.Description,
                SupervisorName = project.SupervisorName,
                Year = project.Year,
                DepartmentId = department.Id
            };

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

        // Title + description se fingerprint bana kar project mein rakh deta hai
        private async Task FillFingerprintAsync(Entities.FypProject entity)
        {
            var fingerprint = await _scorer.CreateFingerprintAsync(entity.Title, entity.Description);

            entity.EmbeddingJson = FingerprintSerializer.Serialize(fingerprint);
            entity.EmbeddingModel = SimilarityRules.EmbeddingModelName;
        }
    }
}