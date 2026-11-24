using FypTracker.Models;

namespace FypTracker.Services
{
    // The "promise": any class with these methods can supply project data.
    // Pehle: DummyProjectService (memory). Ab: DbProjectService (database).
    public interface IProjectService
    {
        Task<List<FypProject>> GetAllAsync();
        Task<int> CountAsync();                       // sirf ginti, poora data load nahi karta
        Task<List<string>> GetUniversityNamesAsync(); // admin form ke dropdown ke liye (short naam)
        Task<bool> TitleExistsAsync(string title, string university, int ignoreId); // isi university mein isi title ka project?
        Task<FypProject?> GetByIdAsync(int id);
        Task<bool> AddAsync(FypProject project);      // false = department nahi mili
        Task<bool> UpdateAsync(FypProject project);   // false = project nahi mila
        Task<bool> DeleteAsync(int id);               // false = project nahi mila
    }
}