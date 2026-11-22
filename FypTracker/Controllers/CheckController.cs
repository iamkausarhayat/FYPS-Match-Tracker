using System.Security.Claims;
using FypTracker.Models;
using FypTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace FypTracker.Controllers
{
    // Guest bhi use kar sakta hai, is liye [Authorize] nahi.
    public class CheckController : Controller
    {
        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".txt" };

        private const long MaxFileBytes = 5 * 1024 * 1024;   // 5 MB

        private readonly SimilarityEngine _engine;
        private readonly PaperSearchService _papers;
        private readonly ILogger<CheckController> _logger;
        private readonly OcrService _ocr;

        public CheckController(SimilarityEngine engine, PaperSearchService papers, ILogger<CheckController> logger, OcrService ocr)
        {
            _engine = engine;
            _papers = papers;
            _logger = logger;
            _ocr = ocr;
        }

        // GET: /Check/Upload
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        // POST: /Check/ExtractText
        // Page ka JavaScript file chunte hi yahan bhejta hai. Hum text nikal kar title/description wapas dete hain.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtractText(IFormFile? document)
        {
            var fileError = ValidateFile(document);
            if (fileError != null)
            {
                return Json(new { ok = false, message = fileError });
            }

            try
            {
                var result = await DocumentTextExtractor.ExtractAsync(
                    document!, _ocr, "Please type the title and description yourself.");

                return result.Ok
                    ? Json(new { ok = true, title = result.Title, description = result.Description, note = result.Message })
                    : Json(new { ok = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read uploaded file {FileName}", document?.FileName);
                return Json(new
                {
                    ok = false,
                    message = "This file could not be read. It may be damaged or password protected. Please type the description yourself."
                });
            }
        }

        // POST: /Check/Upload -> Analyze button
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(CheckIdeaViewModel model)
        {
            // Browser par bharosa nahi: file server par dobara check hoti hai
            var fileError = ValidateFile(model.Document);
            if (fileError != null)
            {
                ModelState.AddModelError(nameof(model.Document), fileError);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var fileName = Path.GetFileName(model.Document!.FileName);
                var title = model.Title.Trim();
                var description = model.Description.Trim();

                // 1. Department ke FYPs se comparison
                var result = await _engine.CheckAsync(title, description, model.SupervisorName.Trim(), fileName);

                // 2. Research papers (Crossref). Yahan masla ho to bhi result bante rehta hai.
                var paperSearch = await _papers.SearchAsync(title, description);
                result.Papers = paperSearch.Papers;

                var id = await _engine.SaveAsync(result, description, GetCurrentUserId());

                // Paigham sirf agle page ke liye (redirect ke baad ek dafa dikhta hai)
                if (paperSearch.Error != null)
                {
                    TempData["PapersNote"] = paperSearch.Error;
                }

                return RedirectToAction(nameof(Result), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Similarity check failed");
                ModelState.AddModelError(string.Empty, "Something went wrong while analyzing your idea. Please try again.");
                return View(model);
            }
        }

        // GET: /Check/Result/5
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var loaded = await _engine.LoadAsync(id);
            if (loaded == null)
            {
                return NotFound();
            }

            var (result, ownerId) = loaded.Value;

            // Login wale student ka result sirf wohi (ya Admin) dekh sakta hai.
            // Guest ka check kisi ke naam nahi hota, wo link se khul jata hai.
            if (ownerId != null)
            {
                var isOwner = GetCurrentUserId() == ownerId;
                var isAdmin = User.IsInRole("Admin");

                if (!isOwner && !isAdmin)
                {
                    return NotFound();
                }
            }

            return View(result);
        }

        // ---------- helpers ----------

        private static string? ValidateFile(IFormFile? file)
        {
            if (file == null)
            {
                return "Please upload your proposal file.";
            }

            if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
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

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}