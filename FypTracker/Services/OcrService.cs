using System.Text;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FypTracker.Services
{
    // Scanned PDF (CamScanner waghera) ki tasveeron se text parhta hai (OCR).
    // Agar tessdata/eng.traineddata maujood nahi to IsAvailable = false hota hai
    // aur app crash nahi hoti: bas saaf paigham dikhata hai.
    public class OcrService
    {
        private const int MaxCharacters = 20000;   // description ki hadd 3000 hai, itna kaafi hai
        private const int GoodEnoughLetters = 20;  // itne huroof mil jayen to tasveer theek parhi gayi

        private readonly string _tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        private readonly SemaphoreSlim _gate = new(1, 1);   // OCR bhari kaam hai: ek waqt mein ek hi
        private readonly ILogger<OcrService> _logger;

        public OcrService(ILogger<OcrService> logger)
        {
            _logger = logger;
        }

        // Kya English language file maujood hai aur theek lagti hai?
        // Asli eng.traineddata (tessdata_fast) taqreeban 4 MB ki hoti hai. Chhoti file kharab hai.
        public bool IsAvailable
        {
            get
            {
                var file = new FileInfo(Path.Combine(_tessDataPath, "eng.traineddata"));
                return file.Exists && file.Length > 1_000_000;
            }
        }

        public async Task<string> ReadScannedPdfAsync(byte[] pdfBytes, int maxPages)
        {
            if (!IsAvailable)
            {
                return string.Empty;
            }

            await _gate.WaitAsync();
            try
            {
                // OCR CPU par chalta hai, is liye alag thread par taake web server na ruke
                return await Task.Run(() => Run(pdfBytes, maxPages));
            }
            catch (Exception ex)
            {
                // Engine ya native DLL ka masla: app crash nahi, bas log aur khali text
                _logger.LogError(ex, "OCR failed. tessdata path: {Path}", _tessDataPath);
                return string.Empty;
            }
            finally
            {
                _gate.Release();
            }
        }

        private string Run(byte[] pdfBytes, int maxPages)
        {
            var text = new StringBuilder();
            int imageCount = 0;
            int pageCount = 0;

            using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
            engine.DefaultPageSegMode = PageSegMode.Auto;

            using var pdf = PdfDocument.Open(pdfBytes);

            foreach (var page in pdf.GetPages().Take(maxPages))
            {
                pageCount++;

                // CamScanner ki har page aam tor par ek bari tasveer hoti hai
                foreach (var image in page.GetImages())
                {
                    imageCount++;

                    var pageText = ReadImage(engine, image, page.Number);
                    if (pageText.Length > 0)
                    {
                        text.AppendLine(pageText);
                    }
                }

                if (text.Length > MaxCharacters)
                {
                    break;
                }
            }

            if (text.Length == 0)
            {
                _logger.LogWarning("OCR finished but found no text. Pages checked: {Pages}, images found: {Images}.",
                    pageCount, imageCount);
            }

            return text.ToString();
        }

        // Ek tasveer ko kai tareeqon se try karta hai, jo pehle theek text de wohi le leta hai
        private string ReadImage(TesseractEngine engine, IPdfImage image, int pageNumber)
        {
            string best = string.Empty;

            foreach (var (name, bytes) in Candidates(image))
            {
                try
                {
                    using var pix = Pix.LoadFromMemory(bytes);
                    using var result = engine.Process(pix);

                    var text = result.GetText() ?? string.Empty;
                    int letters = text.Count(char.IsLetter);

                    _logger.LogInformation("OCR page {Page}, {Name}: {Bytes} bytes, {W}x{H} px -> {Letters} letters.",
                        pageNumber, name, bytes.Length, pix.Width, pix.Height, letters);

                    if (letters >= GoodEnoughLetters)
                    {
                        return text;
                    }

                    if (text.Length > best.Length)
                    {
                        best = text;
                    }
                }
                catch (Exception ex)
                {
                    // ye tareeqa na chale to agla try hota hai
                    _logger.LogWarning(ex, "OCR page {Page}: could not read image using {Name}.", pageNumber, name);
                }
            }

            return best;
        }

        // Tasveer ke bytes ke mumkin roop. JPEG (CamScanner) ho to pehle seedha JPEG, kyunke
        // PdfPig ka PNG banana JPEG par aksar galat tasveer deta hai.
        private List<(string Name, byte[] Bytes)> Candidates(IPdfImage image)
        {
            var list = new List<(string, byte[])>();

            byte[] raw = image.RawBytes.ToArray();

            // 1. JPEG seedha mila? (kabhi PDF mein JPEG kisi aur parat ke andar lipta hota hai)
            var jpeg = FindJpeg(raw);

            // 2. ASCII85 mein lipta hua ho (kuch PDF banane wale tools aisa karte hain) to pehle kholo
            if (jpeg == null)
            {
                var decoded = DecodeAscii85(raw);
                if (decoded != null)
                {
                    jpeg = FindJpeg(decoded);
                }
            }

            bool rawIsJpeg = jpeg != null;

            if (jpeg != null)
            {
                list.Add(("jpeg", jpeg));
            }
            else
            {
                var header = Convert.ToHexString(raw.Take(12).ToArray());
                _logger.LogWarning("OCR: image is not JPEG. Raw bytes: {Length}, first bytes: {Header}.", raw.Length, header);
            }

            try
            {
                if (image.TryGetPng(out var png) && png != null && png.Length > 0)
                {
                    list.Add(("png", png));
                }
            }
            catch
            {
                // PNG na ban saka, baqi tareeqe chalenge
            }

            if (!rawIsJpeg && raw.Length > 0)
            {
                list.Add(("raw", raw));
            }

            return list;
        }

        // Bytes mein JPEG dhoondta hai (FF D8 FF se shuru, FF D9 par khatam)
        private static byte[]? FindJpeg(byte[] data)
        {
            int start = -1;
            for (int i = 0; i + 2 < data.Length; i++)
            {
                if (data[i] == 0xFF && data[i + 1] == 0xD8 && data[i + 2] == 0xFF)
                {
                    start = i;
                    break;
                }
            }

            if (start < 0)
            {
                return null;
            }

            int end = data.Length;
            for (int i = data.Length - 2; i > start; i--)
            {
                if (data[i] == 0xFF && data[i + 1] == 0xD9)
                {
                    end = i + 2;
                    break;
                }
            }

            return data[start..end];
        }

        // ASCII85 ko bytes mein badalta hai. Ye ASCII85 nahi to null.
        private static byte[]? DecodeAscii85(byte[] data)
        {
            var output = new List<byte>(data.Length);
            var group = new uint[5];
            int count = 0;
            int index = 0;

            if (data.Length > 1 && data[0] == (byte)'<' && data[1] == (byte)'~')
            {
                index = 2;
            }

            for (; index < data.Length; index++)
            {
                byte b = data[index];

                if (b == (byte)'~')
                {
                    break;   // "~>" = khatam
                }

                if (b is (byte)' ' or (byte)'\n' or (byte)'\r' or (byte)'\t' or (byte)'\f' or 0)
                {
                    continue;
                }

                if (b == (byte)'z' && count == 0)
                {
                    output.AddRange(new byte[4]);
                    continue;
                }

                if (b < (byte)'!' || b > (byte)'u')
                {
                    return null;   // ASCII85 nahi hai
                }

                group[count++] = (uint)(b - (byte)'!');

                if (count == 5)
                {
                    Ascii85Flush(group, 5, output);
                    count = 0;
                }
            }

            if (count > 1)
            {
                Ascii85Flush(group, count, output);
            }

            return output.Count > 0 ? output.ToArray() : null;
        }

        private static void Ascii85Flush(uint[] group, int count, List<byte> output)
        {
            ulong value = 0;
            for (int i = 0; i < 5; i++)
            {
                value = value * 85 + (i < count ? group[i] : 84);
            }

            int bytesOut = count - 1;
            for (int i = 0; i < bytesOut; i++)
            {
                output.Add((byte)(value >> (24 - 8 * i)));
            }
        }
    }
}