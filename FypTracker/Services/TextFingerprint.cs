namespace FypTracker.Services
{
    // The "meaning fingerprint" of one project or idea.
    public class TextFingerprint
    {
        public float[] TitleVector { get; set; } = Array.Empty<float>();
        public List<string> ChunkTexts { get; set; } = new();
        public List<float[]> ChunkVectors { get; set; } = new();
    }
}