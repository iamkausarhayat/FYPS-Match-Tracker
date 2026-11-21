using System.Text.Json;

namespace FypTracker.Services
{
    // Turns a fingerprint into text for the database, and back again.
    public static class FingerprintSerializer
    {
        public static string Serialize(TextFingerprint fingerprint)
        {
            return JsonSerializer.Serialize(fingerprint);
        }

        public static TextFingerprint? Deserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<TextFingerprint>(json);
            }
            catch (JsonException)
            {
                return null;   // a damaged value must never crash the app
            }
        }
    }
}