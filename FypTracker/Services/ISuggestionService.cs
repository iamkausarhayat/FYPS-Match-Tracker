using FypTracker.Models;

namespace FypTracker.Services
{
    public interface ISuggestionService
    {
        // Normal path: AI suggestion if it is switched on and works, otherwise the template.
        Task<SuggestionViewModel> CreateAsync(string title, string description, SimilarityResultViewModel result);

        // Offline path: always works, no internet, no key. Also used for old saved checks.
        SuggestionViewModel CreateTemplate(SimilarityResultViewModel result);
    }
}