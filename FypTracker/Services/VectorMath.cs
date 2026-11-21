namespace FypTracker.Services
{
    public static class VectorMath
    {
        // Cosine similarity of two meaning-vectors.
        //  about 1  = same direction (same meaning)
        //  about 0  = unrelated
        //  below 0  = opposite
        public static double Cosine(float[] a, float[] b)
        {
            if (a.Length != b.Length)
            {
                throw new ArgumentException("Both vectors must have the same length.");
            }

            double dot = 0;      // how much the two arrows point the same way
            double normA = 0;    // length of arrow A (squared)
            double normB = 0;    // length of arrow B (squared)

            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
            {
                return 0;
            }

            return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }
}