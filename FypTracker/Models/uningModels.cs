namespace FypTracker.Models
{
    // Tuning page ke liye chhoti chhoti classes.

    public enum TuningLabel
    {
        Same = 1,        // sach mein wahi idea (project ye hi kaam karta hai)
        Different = 2    // bilkul alag idea
    }

    // Admin ke likhe hue ek jore ka data (abhi tak score nahi bana)
    public class TuningPair
    {
        public int Number { get; set; }                    // 1 se shuru
        public TuningLabel Label { get; set; }
        public string IdeaTitle { get; set; } = string.Empty;
        public string IdeaDescription { get; set; } = string.Empty;
        public string ProjectTitle { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;   // khali ho sakta hai (title-only)
        public string Note { get; set; } = string.Empty;
    }

    // Ek jore ka nateeja
    public class TuningPairResult
    {
        public int Number { get; set; }
        public TuningLabel Label { get; set; }
        public string IdeaTitle { get; set; } = string.Empty;
        public string ProjectTitle { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public bool ProjectTitleOnly { get; set; }

        public double TitleCosine { get; set; }
        public double DescriptionCosine { get; set; }
        public double RawCosine { get; set; }            // model ka asli number, scaling se pehle
        public double WordScore { get; set; }            // 0 se 100

        public double FinalNow { get; set; }             // abhi ke CosineFloor/Ceiling ke saath
        public string VerdictNow { get; set; } = string.Empty;
        public double CreditNow { get; set; }            // 1 = sahi, 0.5 = adha sahi, 0 = ghalat

        public double FinalSuggested { get; set; }       // tajweez kiye hue numbers ke saath (suggestion ho to)
        public string VerdictSuggested { get; set; } = string.Empty;
        public double CreditSuggested { get; set; }
    }

    // Poori tuning ki report
    public class TuningReport
    {
        public List<TuningPairResult> Pairs { get; set; } = new();
        public List<string> SkippedMessages { get; set; } = new();   // jo jore score nahi ho sake

        public int SameCount { get; set; }
        public int DifferentCount { get; set; }

        // "Same" jore: score ka sab se kam, beech wala, sab se zyada (abhi ke numbers par)
        public double SameMin { get; set; }
        public double SameMedian { get; set; }
        public double SameMax { get; set; }

        public double DifferentMin { get; set; }
        public double DifferentMedian { get; set; }
        public double DifferentMax { get; set; }

        // Abhi ke numbers par nateeja
        public int SameAlreadyDone { get; set; }         // sahi: Already Done mila
        public int SameOnlySimilar { get; set; }         // adha sahi: Similar mila
        public int SameMissed { get; set; }              // ghalat: Unique mila
        public int DifferentUnique { get; set; }         // sahi: Unique mila
        public int DifferentSimilar { get; set; }        // adha ghalat: Similar mila
        public int DifferentWrongDone { get; set; }      // ghalat: Already Done mila

        public double AccuracyNow { get; set; }          // 0 se 100

        // Tajweez (sirf tab jab dono qism ke kam az kam MinPerLabel jore hon)
        public bool HasSuggestion { get; set; }
        public double SuggestedFloor { get; set; }
        public double SuggestedCeiling { get; set; }
        public double AccuracySuggested { get; set; }
        public bool SuggestionBetter { get; set; }       // kya tajweez abhi se behtar hai?

        public double CurrentFloor { get; set; }
        public double CurrentCeiling { get; set; }
        public string? WarningMessage { get; set; }
    }

    public class TuningPageViewModel
    {
        public string Input { get; set; } = string.Empty;
        public string ExampleText { get; set; } = string.Empty;
        public string? Error { get; set; }
        public List<string> ParseErrors { get; set; } = new();
        public TuningReport? Report { get; set; }
    }
}