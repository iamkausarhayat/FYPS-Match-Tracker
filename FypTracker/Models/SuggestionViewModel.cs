namespace FypTracker.Models
{
    // The friendly box at the bottom of the result page.
    public class SuggestionViewModel
    {
        public string Headline { get; set; } = string.Empty;   // e.g. "Congratulations! Your idea looks unique."
        public string Message { get; set; } = string.Empty;    // 2 to 3 simple sentences
        public List<string> Tips { get; set; } = new();        // 2 to 3 short ideas

        // "template" = written by our own rules (works offline), "ai" = written by the AI service
        public string Source { get; set; } = "template";
        public bool IsAi => Source == "ai";
    }
}