namespace FypTracker.Models
{
    // Everything the result page needs.
    public class SimilarityResultViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string SupervisorName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

        public List<MatchItemViewModel> DepartmentMatches { get; set; } = new();
        public List<MatchItemViewModel> PaperMatches { get; set; } = new();

        // Related research papers from Crossref (sirf list, in ka percentage nahi nikalta)
        public List<PaperItemViewModel> Papers { get; set; } = new();

        // The closest item among all department FYPs and papers decides the verdict.
        public MatchItemViewModel? TopMatch =>
            DepartmentMatches.Concat(PaperMatches)
                             .OrderByDescending(m => m.FinalScore)
                             .FirstOrDefault();

        public double OverallScore => TopMatch?.FinalScore ?? 0;
        public int OverallPercent => (int)Math.Round(OverallScore);
        public string Verdict => SimilarityRules.VerdictFor(OverallScore);

        public string VerdictClass => Verdict switch
        {
            "Already Done" => "done",
            "Similar" => "similar",
            _ => "unique"
        };

        public string Advice => Verdict switch
        {
            "Already Done" => "Your idea is very close to existing work. Choose a different angle or talk to your supervisor.",
            "Similar" => "Parts of your idea overlap with existing work. Make clear what is new in your project.",
            _ => "Your idea looks original compared with what we checked. Share the report with your supervisor."
        };
    }
}