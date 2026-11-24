namespace FypTracker.Services
{
    // Live search ki settings. Ye sab "defaults" hain: appsettings.json mein "LiveSearch" naam ka
    // section na ho tab bhi app yahi numbers use karti hai.
    public class LiveSearchOptions
    {
        // false = live search band, sirf database mein pehle se saved projects se compare
        public bool Enabled { get; set; } = true;

        // Har university se kitni alag keyword searches (zyada = behtar coverage, lekin zyada requests)
        public int MaxQueries { get; set; } = 4;

        // Har search se kitne results mangwane hain
        public int ResultsPerQuery { get; set; } = 20;

        // Sab universities ka intezar kul itne second
        public int FetchTimeoutSeconds { get; set; } = 10;

        // Ek check mein sirf itne NAYE projects ke fingerprint bante hain (AI model dheema hai)
        public int MaxNewProjectsPerCheck { get; set; } = 12;

        // Naye projects ke fingerprint banane par kul itne second se zyada nahi lagane
        public int EmbedBudgetSeconds { get; set; } = 20;

        // Ek hi search dobara itne ghante tak university se nahi mangwate (memory mein yaad rakhte hain)
        public int CacheHours { get; set; } = 6;
    }
}