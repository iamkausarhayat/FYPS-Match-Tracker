namespace FypTracker.Models
{
    // Everything the result page needs.
    public class SimilarityResultViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;   // optional, can be empty
        public string FileName { get; set; } = string.Empty;

        // All matching projects from all universities
        public List<MatchItemViewModel> ProjectMatches { get; set; } = new();
        public List<MatchItemViewModel> PaperMatches { get; set; } = new();

        // Related research papers from Crossref (sirf list, in ka percentage nahi nikalta)
        public List<PaperItemViewModel> Papers { get; set; } = new();

        // The friendly suggestion box (set by SuggestionService)
        public SuggestionViewModel? Suggestion { get; set; }

        // Coverage: how much did we really compare with?
        public int ComparedProjectCount { get; set; }
        public int ComparedUniversityCount { get; set; }

        // The closest item decides the verdict.
        public MatchItemViewModel? TopMatch =>
            ProjectMatches.Concat(PaperMatches)
                          .OrderByDescending(m => m.FinalScore)
                          .FirstOrDefault();

        // Everything except the top match, best first. Low scores are kept on purpose,
        // so a "Unique" student can still see the closest projects.
        public List<MatchItemViewModel> OtherMatches =>
            ProjectMatches.OrderByDescending(m => m.FinalScore)
                          .Skip(1)
                          .ToList();

        // Short university names that appear in the list (for the filter box)
        public List<string> UniversityFilterNames =>
            OtherMatches.Select(m => m.UniversityShort)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList();

        public double OverallScore => TopMatch?.FinalScore ?? 0;
        public int OverallPercent => (int)Math.Round(OverallScore);
        public string Verdict => SimilarityRules.VerdictFor(OverallScore);

        public string VerdictClass => SimilarityRules.ClassFor(OverallScore);

        // "Compared with 120 projects from 3 universities." We never say "100% unique".
        public string CoverageText
        {
            get
            {
                if (ComparedProjectCount == 0)
                {
                    // Old checks (made before coverage was saved) have 0 here but still have matches
                    return ProjectMatches.Count > 0
                        ? "Coverage was not recorded for this older check."
                        : "No projects were available to compare with yet.";
                }

                string projects = ComparedProjectCount == 1 ? "1 project" : $"{ComparedProjectCount} projects";
                string universities = ComparedUniversityCount == 1 ? "1 university" : $"{ComparedUniversityCount} universities";
                return $"Your idea was compared with {projects} from {universities}.";
            }
        }

        // Short text used by the PDF: headline + message of the suggestion
        public string Advice
        {
            get
            {
                if (Suggestion != null)
                {
                    return Suggestion.Headline + " " + Suggestion.Message;
                }

                return Verdict switch
                {
                    "Already Done" => "This idea looks already done. Please think of a different idea or a new angle.",
                    "Similar" => "Think a little more. Your idea is close to some existing projects.",
                    _ => "Congratulations! Your idea looks unique compared with the projects we checked."
                };
            }
        }
    }
}