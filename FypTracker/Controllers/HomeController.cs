using System.Diagnostics;
using FypTracker.Models;
using FypTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace FypTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProjectService _projects;
        private readonly BrowseService _browse;

        public HomeController(IProjectService projects, BrowseService browse)
        {
            _projects = projects;
            _browse = browse;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeIndexViewModel();
            try
            {
                // Real number from the database. If the database is down, the landing page still opens.
                model.ProjectCount = await _projects.CountAsync();
                model.UniversityCount = await _browse.CountUniversitiesAsync();

                var top = await _browse.GetTopAsync(6);
                model.TopProjects = top.Items;
                model.TopAreAwards = top.AreAwards;
            }
            catch
            {
                model.ProjectCount = 0;
                model.UniversityCount = 0;
                model.TopProjects = new List<BrowseItem>();
            }
            return View(model);
        }

        // Shown for HTTP errors such as 404 and 403 (wired up in Program.cs)
        public IActionResult Status(int code)
        {
            var model = code switch
            {
                404 => new StatusPageViewModel
                {
                    Code = "404",
                    Title = "Page not found",
                    Message = "The page you are looking for does not exist or has been moved.",
                    Icon = "bi-compass"
                },
                403 => new StatusPageViewModel
                {
                    Code = "403",
                    Title = "Access denied",
                    Message = "You do not have permission to open this page.",
                    Icon = "bi-shield-lock"
                },
                401 => new StatusPageViewModel
                {
                    Code = "401",
                    Title = "Please log in",
                    Message = "You need to log in before you can open this page.",
                    Icon = "bi-person-lock"
                },
                _ => new StatusPageViewModel
                {
                    Code = code == 0 ? "Oops" : code.ToString(),
                    Title = "Something went wrong",
                    Message = "The request could not be completed. Please try again.",
                    Icon = "bi-exclamation-triangle"
                }
            };

            return View(model);
        }

        // Shown when the app crashes with an unexpected error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}