using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace FypTracker.Services
{
    public record ExtractedDocument(string Title, string Description);

    // Uploaded file (PDF, DOCX, TXT) se text nikalta hai aur Title + Description bana deta hai.
    public static class DocumentTextExtractor
    {
        private const int MaxPdfPages = 30;            // bohat bari PDF par waqt zaya na ho
        private const long MaxXmlBytes = 20 * 1024 * 1024;
        private const int MaxTitleLength = 150;
        private const int MaxDescriptionLength = 3000;

        // 1. File se kacha text
        public static async Task<string> ReadTextAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            // PDF reader ko seekable stream chahiye, is liye pehle memory mein copy
            using var memory = new MemoryStream();
            await file.CopyToAsync(memory);
            memory.Position = 0;

            return extension switch
            {
                ".pdf" => ReadPdf(memory),
                ".docx" => ReadDocx(memory),
                ".txt" => ReadTxt(memory),
                _ => throw new NotSupportedException("Unsupported file type.")
            };
        }

        // 2. Kacha text -> Title + Description
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

            string title;
            string description;

            // Pehli line agar chhoti aur title jaisi hai to wohi title hai
            var first = lines[0];
            bool looksLikeTitle = first.Length >= 5
                                  && first.Length <= MaxTitleLength
                                  && first.Split(' ').Length <= 20
                                  && !first.EndsWith('.');

            if (looksLikeTitle)
            {
                title = first;
                description = string.Join(" ", lines.Skip(1));

                // Agar baqi text bahut chhota ho to poora text description bana do
                if (description.Length < 50)
                {
                    description = string.Join(" ", lines);
                }
            }
            else
            {
                title = TitleFromFileName(fileName);
                description = string.Join(" ", lines);
            }

            return new ExtractedDocument(title, Cut(description, MaxDescriptionLength));
        }

        // ---------- readers ----------

        private static string ReadPdf(Stream stream)
        {
            var text = new StringBuilder();

            using var pdf = PdfDocument.Open(stream);
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