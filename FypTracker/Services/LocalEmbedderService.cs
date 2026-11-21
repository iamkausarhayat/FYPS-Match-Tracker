using LMSupply.Embedder;
using FypTracker.Models;

namespace FypTracker.Services
{
    // Loads the AI model ONCE and keeps it in memory (registered as a singleton in Program.cs).
    public class LocalEmbedderService : IEmbedder, IAsyncDisposable
    {
        private const string ModelName = SimilarityRules.EmbeddingModelName;

        private readonly SemaphoreSlim _lock = new(1, 1);   // one request at a time keeps it safe
        private IAsyncDisposable? _model;
        private Func<string, Task<float[]>>? _embed;

        public async Task<float[]> EmbedAsync(string text)
        {
            await _lock.WaitAsync();
            try
            {
                if (_embed == null)
                {
                    // Only the very first call loads the model.
                    var model = await LocalEmbedder.LoadAsync(ModelName);
                    _model = model;
                    _embed = async value => await model.EmbedAsync(value);
                }

                return await _embed(text);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<float[]>> EmbedManyAsync(IReadOnlyList<string> texts)
        {
            var vectors = new List<float[]>();

            foreach (var text in texts)
            {
                vectors.Add(await EmbedAsync(text));
            }

            return vectors;
        }

        public async ValueTask DisposeAsync()
        {
            if (_model != null)
            {
                await _model.DisposeAsync();
            }

            _lock.Dispose();
        }
    }
}