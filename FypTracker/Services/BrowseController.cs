using FypTracker.Models;
using FypTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace FypTracker.Controllers
{
    // Browse page: koi bhi (guest bhi) projects dekh sakta hai, filter laga sakta hai,
    // aur har project ka original page khol sakta hai. Login zaroori nahi.
    public class BrowseController : Controller
    {
        private readonly BrowseService _browse;
        private readonly ILogger<BrowseController> _logger;

        public BrowseController(BrowseService browse, ILogger<BrowseController> logger)
        {
            _browse = browse;
            _logger = logger;
        }

        // GET: /Browse?q=attendance&university=UMT&year=2024&area=...&awardsOnly=true&page=2
        [HttpGet]
        public async Task<IActionResult> Index(string? q, string? university, int? year, string? area, bool awardsOnly = false, int page = 1)
        {
            try
            {
                return View(await _browse.SearchAsync(q, university, year, area, awardsOnly, page));
            }
            catch (Exception ex)
            {
                // Database band ho to page khulta hai aur saaf paigham dikhata hai
                _logger.LogError(ex, "Could not load the browse page");
                return View(new BrowseViewModel
                {
                    Error = "The projects could not be loaded right now. Please try again in a moment."
                });
            }
        }
    }
}