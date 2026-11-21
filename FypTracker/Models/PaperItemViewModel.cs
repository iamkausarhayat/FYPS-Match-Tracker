namespace FypTracker.Models
{
    // Ek research paper, jaise Result page par dikhana hai
    public class PaperItemViewModel
    {
        public string Doi { get; set; } = string.Empty;      // paper ki pehchan (DOI)
        public string Title { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;  // pehle 3 authors
        public int? Year { get; set; }                        // saal na ho to khali
        public string Url { get; set; } = string.Empty;      // paper ka link
    }
}