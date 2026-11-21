using FypTracker.Models;

namespace FypTracker.Services
{
    // The "promise": any class with these methods can supply project data.
    // Pehle: DummyProjectService (memory). Ab: DbProjectService (database).
    public interface IProjectService
    {
        Task<List<FypProject>> GetAllAsync();
        Task<FypProject?> GetByIdAsync(int id);
        Task<bool> AddAsync(FypProject project);      // false = department nahi mili
        Task<bool> UpdateAsync(FypProject project);   // false = project nahi mila
        Task<bool> DeleteAsync(int id);               // false = project nahi mila
    }
}