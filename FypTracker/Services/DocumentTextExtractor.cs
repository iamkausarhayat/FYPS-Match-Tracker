using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace FypTracker.Services
{
    public record ExtractedDocument(string Title, string Description, bool FoundAbstract = false);

    // Kacha text + ye ke OCR use hua, ya scanned file thi magar OCR maujood na tha
    public record RawText(string Text, bool UsedOcr, bool ScannedButNoOcr);

    // Controller ko wapas milne wala tayyar jawab. Message: Ok=false par ghalti, Ok=true par note.
    public record FileExtractionResult(bool Ok, string Title, string Description, string Message);

    // Uploaded file (PDF, DOCX, TXT) se text nikalta hai aur Title + Description bana deta hai.
    // Student wala page aur Admin wala form dono isi ko istemal karte hain.
    public static class DocumentTextExtractor
    {
        private const int MaxPdfPages = 30;            // bohat bari PDF par waqt zaya na ho
        private const int MaxOcrPages = 8;             // OCR dheela hota hai, is liye kam pages
        private const int MinLettersForTextLayer = 80; // is se kam huroof = PDF mein asal text nahi (scan hai)
        private const long MaxXmlBytes = 20 * 1024 * 1024;
        private const long MaxFileBytes = 5 * 1024 * 1024;
        private const int MaxTitleLength = 150;
        private const int MaxDescriptionLength = 3000;
        private const int TitleSearchLines = 25;       // title pehli itni lines mein dhoondte hain

        // ---------- Title mein se nikalne wali lines (cover page ka kachra) ----------
        // Is list ko aap apni zaroorat ke mutabiq badal sakte hain.

        // In lafzon wali line HAMESHA kachra hai (university ka naam, certificate waghera)
        private static readonly Regex StrongJunk = new(
            @"\b(universit\w*|department|faculty|institute|college|campus|numl|thesis|dissertation|certificate|" +
            @"declaration|acknowledg\w*|approval|approved|submitted|supervis\w*|abstract|contents|prepared|dedication)\b" +
            @"|\b(bachelor|master)s?\s+(of|in)\b|\bfinal\s+year\b|\bpartial\s+fulfil\w*|\bsession\s*[:\-]?\s*\d{4}" +
            @"|\broll\s*(no|number)\b|\breg(istration)?\.?\s*(no|number)\b|\b(19|20)\d{2}\s*[-–/]\s*(19|20)?\d{2}\b" +
            @"|^\s*(by|name|names|id|students?|members?|group|team|date|advisor|co-?supervisor|roll|reg)\s*[:\-]|^\s*(table|list)\s+of\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Wo lines jo SIRF "Project Report" / "Proposal" / "Documentation" jaisay lafzon se bani hon.
        // "Parking Report Generator" jaisa asal title is mein nahi aata (us mein aur lafz bhi hain).
        private static readonly Regex WeakJunk = new(
            @"^\s*(a\s+|the\s+)?(project\s+|fyp\s+)?(report|proposal|documentation|document|synopsis|project)(\s+(report|proposal|documentation|document))?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // "1. Introduction", "Chapter 1", "Keywords: ..." jaisi headings title nahi hoti
        private static readonly Regex HeadingLine = new(
            @"^\s*((chapter|section)\s*\w+|(\d+\.?\s+)?introduction|key\s*words?\s*[:\-].*|index\s+terms.*)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // "FINAL YEAR PROJECT REPORT" jaisi line. Cover page par asal title aksar iske BAAD aata hai.
        private static readonly Regex ReportMarker = new(
            @"^\s*(a\s+|the\s+)?((final\s+year|fyp)\s+)?(project\s+)?(report|thesis|dissertation|proposal|synopsis)\s*$" +
            @"|^\s*(final\s+year|fyp)\s+project(\s+(report|proposal|thesis))?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // "Title: Smart Parking System" jaisi line
        private static readonly Regex TitleLabel = new(
            @"^(project\s+)?title\s*[:\-–—]\s*(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // ---------- Abstract dhoondne ke qaide ----------

        // "Abstract", "ABSTRACT:", "Abstract - text..." ya "Executive Summary"
        private static readonly Regex AbstractHeading = new(
            @"^\s*(abstract|executive\s+summary)\b[\s.:\-–—]*(.*)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Abstract yahan khatam hota hai
        private static readonly Regex AbstractEnd = new(
            @"^\s*(key\s*words?|index\s+terms|table\s+of\s+contents|contents|acknowledg\w*|dedication|" +
            @"list\s+of\s+\w+|chapter\s*(1|one|i)\b|1\.?\s+introduction|introduction)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // "Abstract ........ iii" (fehrist ki line) abstract nahi hoti
        private static readonly Regex PageNumberOnly = new(
            @"^(\d{1,3}|[ivxlc]{1,6})$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<string> ConnectorWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "of", "for", "and", "in", "on", "using", "with", "to", "the", "a", "an", "by", "based", "via", "&", "at", "from"
        };

        // =====================================================================
        // 0. File ki jaanch (size, type)
        // =====================================================================
        public static string? Validate(IFormFile? file)
        {
            if (file == null)
            {
                return "Please choose a file.";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".pdf" or ".docx" or ".txt"))
            {
                return "Only PDF, Word (.docx) or text (.txt) files are allowed.";
            }

            if (file.Length == 0)
            {
                return "The file is empty.";
            }

            if (file.Length > MaxFileBytes)
            {
                return "The file is too big. Maximum size is 5 MB.";
            }

            return null;
        }

        // =====================================================================
        // 1. Ek hi call: file -> Title + Description (+ saaf paigham)
        // =====================================================================
        public static async Task<FileExtractionResult> ExtractAsync(IFormFile file, OcrService? ocr, string manualHint)
        {
            var raw = await ReadTextAsync(file, ocr);

            if (LetterCount(raw.Text) < 20)
            {
                string message;

                if (raw.ScannedButNoOcr)
                {
                    message = "This PDF is a scanned picture and OCR (text reading from pictures) is not set up on the server. " + manualHint;
                }
                else if (raw.UsedOcr)
                {
                    message = "We tried to read this scan with OCR but found no readable text. Try a clearer, straight scan. " + manualHint;
                }
                else
                {
                    message = "No readable text was found in this file. " + manualHint;
                }

                return new FileExtractionResult(false, string.Empty, string.Empty, message);
            }

            var document = Split(raw.Text, file.FileName);

            if (string.IsNullOrWhiteSpace(document.Description))
            {
                return new FileExtractionResult(false, string.Empty, string.Empty,
                    "No readable text was found in this file. " + manualHint);
            }

            var note = new StringBuilder();

            if (raw.UsedOcr)
            {
                note.Append("This file was a scan, so we read it with OCR. OCR can make spelling mistakes. ");
            }

            note.Append(document.FoundAbstract
                ? "We found the Abstract and used it as the description. Please check the title and description."
                : "We could not find an Abstract heading, so we used the first text of the file. Please check the title and description.");

            return new FileExtractionResult(true, document.Title, document.Description, note.ToString());
        }

        // =====================================================================
        // 2. File se kacha text
        // =====================================================================
        public static async Task<RawText> ReadTextAsync(IFormFile file, OcrService? ocr = null)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // Memory mein copy: PDF reader ko seekable data chahiye
            using var memory = new MemoryStream();
            await file.CopyToAsync(memory);
            var bytes = memory.ToArray();

            switch (extension)
            {
                case ".pdf":
                    return await ReadPdfAsync(bytes, ocr);
                case ".docx":
                    return new RawText(ReadDocx(new MemoryStream(bytes)), false, false);
                case ".txt":
                    return new RawText(ReadTxt(new MemoryStream(bytes)), false, false);
                default:
                    throw new NotSupportedException("Unsupported file type.");
            }
        }

        // =====================================================================
        // 3. Kacha text -> Title + Description
        // =====================================================================
        public static ExtractedDocument Split(string rawText, string fileName)
        {
            var lines = rawText
                .Replace("\r", string.Empty)
                .Split('\n')
                .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
                .Where(line => line.Length > 0)
                .ToList();

            if (lines.Count == 0)
            {
                return new ExtractedDocument(TitleFromFileName(fileName), string.Empty);
            }

            // --- Title ---
            var (foundTitle, titleIndex) = FindTitle(lines);
            var title = foundTitle ?? TitleFromFileName(fileName);

            // --- Description: pehle "Abstract" dhoondo ---
            var abstractText = FindAbstract(lines);
            if (abstractText != null)
            {
                return new ExtractedDocument(title, Cut(abstractText, MaxDescriptionLength), true);
            }

            // --- Abstract nahi mila: title ke baad ka text, cover page ka kachra hata kar ---
            var after = lines.Skip(titleIndex + 1).Where(line => !IsJunkLine(line)).ToList();
            var description = string.Join(" ", after);

            if (description.Length < 50)
            {
                description = string.Join(" ", lines.Where(line => !IsJunkLine(line)));
            }

            if (description.Length < 50)
            {
                description = string.Join(" ", lines);
            }

            return new ExtractedDocument(title, Cut(description, MaxDescriptionLength));
        }

        // ---------- title ----------

        // (title, uski line ka number). Nahi mila to (null, -1).
        private static (string? Title, int Index) FindTitle(List<string> lines)
        {
            int limit = Math.Min(lines.Count, TitleSearchLines);

            // 1. Agar "Title: ..." likha ho to wohi sab se pakka hai
            for (int i = 0; i < limit; i++)
            {
                var label = TitleLabel.Match(lines[i]);
                if (label.Success && IsTitleLike(label.Groups[2].Value, 1))
                {
                    return Extend(lines, i, label.Groups[2].Value.Trim());
                }
            }

            // 2. Cover page: university ka naam (jo aksar 2 lines mein toot jata hai) "FINAL YEAR PROJECT REPORT"
            //    se pehle aata hai. Is liye us line ke baad se dhoondna shuru karte hain.
            int start = 0;
            for (int i = 0; i < limit; i++)
            {
                if (!ReportMarker.IsMatch(lines[i]))
                {
                    continue;
                }

                if (lines.Take(i).Any(line => StrongJunk.IsMatch(line)))
                {
                    start = i + 1;
                }

                break;
            }

            var found = FindFrom(lines, start, limit);
            if (found.Title == null && start > 0)
            {
                found = FindFrom(lines, 0, limit);   // marker ke baad kuch na mila to shuru se dekho
            }

            return found;
        }

        // Pehli aisi line jo title jaisi ho aur cover page ka kachra na ho
        private static (string? Title, int Index) FindFrom(List<string> lines, int from, int limit)
        {
            for (int i = from; i < limit; i++)
            {
                var line = lines[i];

                if (IsTitleLike(line) && !IsJunkLine(line))
                {
                    return Extend(lines, i, line);
                }
            }

            return (null, -1);
        }

        // Lamba title PDF mein 2-3 lines mein toot jata hai: agli lines bhi jod lo (sirf pakki surat mein)
        private static (string? Title, int Index) Extend(List<string> lines, int index, string first)
        {
            string title = first;
            int last = index;

            for (int step = 0; step < 2 && last + 1 < lines.Count; step++)
            {
                var next = lines[last + 1];

                bool endsWithConnector = ConnectorWords.Contains(title.Split(' ').Last());
                bool nextStartsLower = char.IsLower(next[0]);
                bool lineWasFull = title.Length >= 30 && next.Split(' ').Length <= 6;

                if ((endsWithConnector || nextStartsLower || lineWasFull)
                    && IsTitleLike(next, 1) && !IsJunkLine(next)
                    && title.Length + 1 + next.Length <= MaxTitleLength)
                {
                    title += " " + next;
                    last++;
                }
                else
                {
                    break;
                }
            }

            return (Tidy(title, forceTitleCase: IsAllCaps(first)), last);
        }

        private static bool IsTitleLike(string line, int minWords = 2)
        {
            int words = line.Split(' ').Length;

            return line.Length >= 5
                   && line.Length <= MaxTitleLength
                   && words >= minWords
                   && words <= 20
                   && !line.EndsWith('.')
                   && line.Count(char.IsLetter) >= 4
                   && !PageNumberOnly.IsMatch(line);
        }

        private static bool IsJunkLine(string line)
        {
            return StrongJunk.IsMatch(line) || WeakJunk.IsMatch(line) || HeadingLine.IsMatch(line);
        }

        private static bool IsAllCaps(string text)
        {
            return text.Any(char.IsLetter) && text == text.ToUpperInvariant();
        }

        // POORA CAPITAL title ko Title Case bana do: "SMART PARKING" -> "Smart Parking"
        private static string Tidy(string title, bool forceTitleCase = false)
        {
            title = title.Trim().Trim('"', '“', '”');

            if (forceTitleCase || IsAllCaps(title))
            {
                title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(title.ToLowerInvariant());
            }

            return Cut(title, MaxTitleLength);
        }

        // ---------- abstract ----------

        private static string? FindAbstract(List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var heading = AbstractHeading.Match(lines[i]);
                if (!heading.Success)
                {
                    continue;
                }

                var inline = heading.Groups[2].Value.Trim();

                // "Abstract ..... iii" fehrist ki line hai, asal abstract nahi
                if (PageNumberOnly.IsMatch(inline))
                {
                    continue;
                }

                var parts = new List<string>();
                if (inline.Length > 0)
                {
                    parts.Add(inline);
                }

                int total = inline.Length;

                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (AbstractEnd.IsMatch(lines[j]))
                    {
                        break;
                    }

                    parts.Add(lines[j]);
                    total += lines[j].Length + 1;

                    if (total > MaxDescriptionLength)
                    {
                        break;
                    }
                }

                var text = string.Join(" ", parts).Trim();

                // Itna chhota ho to ye asal abstract nahi, agla "Abstract" dekho
                if (text.Length >= 80)
                {
                    return text;
                }
            }

            return null;
        }

        // ---------- readers ----------

        private static async Task<RawText> ReadPdfAsync(byte[] bytes, OcrService? ocr)
        {
            var layerText = ReadPdfTextLayer(bytes);

            // PDF mein asal text maujood hai
            if (LetterCount(layerText) >= MinLettersForTextLayer)
            {
                return new RawText(layerText, false, false);
            }

            // Text nahi (scan / CamScanner): OCR ki koshish
            if (ocr == null || !ocr.IsAvailable)
            {
                return new RawText(layerText, false, true);
            }

            var ocrText = await ocr.ReadScannedPdfAsync(bytes, MaxOcrPages);
            return new RawText(layerText + "\n" + ocrText, true, false);
        }

        private static string ReadPdfTextLayer(byte[] bytes)
        {
            var text = new StringBuilder();

            using var pdf = PdfDocument.Open(bytes);
            foreach (var page in pdf.GetPages().Take(MaxPdfPages))
            {
                text.AppendLine(ContentOrderTextExtractor.GetText(page));

                if (text.Length > 20000)
                {
                    break;   // description ki hadd 3000 hai, itna kaafi hai
                }
            }

            return text.ToString();
        }

        private static string ReadDocx(Stream stream)
        {
            // .docx asal mein ek zip hai. Text word/document.xml mein hota hai.
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            var entry = zip.GetEntry("word/document.xml")
                        ?? throw new InvalidDataException("This is not a valid .docx file.");

            if (entry.Length > MaxXmlBytes)
            {
                throw new InvalidDataException("The document is too large to read.");
            }

            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            using var xmlStream = entry.Open();
            var document = XDocument.Load(xmlStream);

            var text = new StringBuilder();

            foreach (var paragraph in document.Descendants(w + "p"))
            {
                foreach (var node in paragraph.Descendants())
                {
                    if (node.Name == w + "t") text.Append(node.Value);
                    else if (node.Name == w + "tab" || node.Name == w + "br") text.Append(' ');
                }

                text.AppendLine();
            }

            return text.ToString();
        }

        private static string ReadTxt(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }

        // ---------- helpers ----------

        private static int LetterCount(string? text)
        {
            return string.IsNullOrEmpty(text) ? 0 : text.Count(char.IsLetter);
        }

        private static string TitleFromFileName(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            name = Regex.Replace(name, @"[_\-]+", " ").Trim();
            name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name);
            name = Cut(name, MaxTitleLength);

            return name.Length >= 5 ? name : "Untitled project";
        }

        private static string Cut(string value, int maxLength)
        {
            if (value.Length <= maxLength)
            {
                return value;
            }

            var cut = value[..maxLength];
            var lastSpace = cut.LastIndexOf(' ');
            return (lastSpace > maxLength / 2 ? cut[..lastSpace] : cut).Trim();
        }
    }
}