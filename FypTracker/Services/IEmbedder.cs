namespace FypTracker.Services
{
    // The "promise": anything that can turn text into meaning-numbers.
    // Now: LocalEmbedderService (the real AI model). In tests (B10) we can use a fake one.
    public interface IEmbedder
    {
        Task<float[]> EmbedAsync(string text);
        Task<List<float[]>> EmbedManyAsync(IReadOnlyList<string> texts);
    }
}