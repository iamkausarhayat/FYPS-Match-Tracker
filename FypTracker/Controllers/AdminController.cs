using FypTracker.Models;
using FypTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FypTracker.Controllers
{
    [Authorize(Roles = "Admin")]   // sirf login kiya hua Admin andar aa sakta hai
    public class AdminController : Controller
    {
        private readonly IProjectService _projects;
        private readonly ILogger<AdminController> _logger;
        private readonly OcrService _ocr;

        // The framework hands us the helpers (Dependency Injection).
        public AdminController(IProjectService projects, ILogger<AdminController> logger, OcrService ocr)
        {
            _projects = projects;
            _logger = logger;
            _ocr = ocr;
        }

        // GET: /Admin/Projects?q=...&department=...
        [HttpGet]
        public async Task<IActionResult> Projects(string? q, string? department)
        {
            try
            {
                var all = await _projects.GetAllAsync();
                IEnumerable<FypProject> filtered = all;

                if (!string.IsNullOrWhiteSpace(q))
                {
                    filtered = filtered.Where(p =>
                        p.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        p.University.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        p.SupervisorName.Contains(q, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(department))
                {
                    filtered = filtered.Where(p => p.Department == department);
                }

                var model = new AdminProjectsViewModel
                {
                    Projects = filtered.OrderByDescending(p => p.Year).ThenBy(p => p.Title).ToList(),
                    TotalProjects = all.Count,
                    DepartmentCount = all.Select(p => p.Department).Distinct().Count(),
                    LatestYear = all.Count == 0 ? 0 : all.Max(p => p.Year),
                    ByDepartment = all.GroupBy(p => p.Department).ToDictionary(g => g.Key, g => g.Count()),
                    Query = q,
                    SelectedDepartment = department
                };

                return View(model);
            }
            catch (Exception ex)
            {
                // Database band: crash nahi, page khulta hai aur laal paigham dikhata hai
                _logger.LogError(ex, "Could not load the projects list");
                TempData["Error"] = "The projects could not be loaded right now. Please try again in a moment.";

                return View(new AdminProjectsViewModel { Query = q, SelectedDepartment = department });
            }
        }

        // GET: /Admin/Create -> empty form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return await FormViewAsync(new ProjectFormViewModel { Year = DateTime.Now.Year });
        }

        // POST: /Admin/Create -> Save button
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectFormViewModel model)
        {
            try
            {
                if (await _projects.TitleExistsAsync(model.Title, model.University, model.Id))
                {
                    ModelState.AddModelError(nameof(model.Title), "This university already has a project with this title.");
                }

                if (!ModelState.IsValid)
                {
                    return await FormViewAsync(model);
                }

                if (!await _projects.AddAsync(ToProject(model)))
                {
                    ModelState.AddModelError(nameof(model.Department), "This department or university was not found.");
                    return await FormViewAsync(model);
                }
            }
            catch (Exception ex)
            {
                // Database ya AI model (fingerprint banate waqt) fail ho to form wapas aata hai
                _logger.LogError(ex, "Could not add a project");
                ModelState.AddModelError(string.Empty, "The project could not be saved. Please try again in a moment.");
                return await FormViewAsync(model);
            }

            TempData["Success"] = "Project added successfully.";
            return RedirectToAction(nameof(Projects));
        }

        // GET: /Admin/Edit/3 -> form filled with the project
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var project = await _projects.GetByIdAsync(id);
                if (project == null)
                {
                    return NotFound();   // shows our 404 page
                }

                return await FormViewAsync(new ProjectFormViewModel
                {
                    Id = project.Id,
                    Title = project.Title,
                    University = project.University,
                    Department = project.Department,
                    SupervisorName = project.SupervisorName,
                    Year = project.Year,
                    Description = project.Description,
                    SourceUrl = project.SourceUrl,
                    Area = project.Area,
                    IsAward = project.IsAward,
                    AwardText = project.AwardText
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not open project {Id} for editing", id);
                TempData["Error"] = "The project could not be opened right now. Please try again in a moment.";
                return RedirectToAction(nameof(Projects));
            }
        }

        // POST: /Admin/Edit -> Save changes button
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectFormViewModel model)
        {
            try
            {
                if (await _projects.TitleExistsAsync(model.Title, model.University, model.Id))
                {
                    ModelState.AddModelError(nameof(model.Title), "Another project of this university already uses this title.");
                }

                if (!ModelState.IsValid)
                {
                    return await FormViewAsync(model);
                }

                if (!await _projects.UpdateAsync(ToProject(model)))
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not update project {Id}", model.Id);
                ModelState.AddModelError(string.Empty, "The changes could not be saved. Please try again in a moment.");
                return await FormViewAsync(model);
            }

            TempData["Success"] = "Project updated successfully.";
            return RedirectToAction(nameof(Projects));
        }

        // POST: /Admin/Delete -> "Yes, delete" in the confirmation box
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (await _projects.DeleteAsync(id))
                {
                    TempData["Success"] = "Project deleted.";
                }
                else
                {
                    TempData["Error"] = "That project could not be found. It may already be deleted.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not delete project {Id}", id);
                TempData["Error"] = "The project could not be deleted right now. Please try again in a moment.";
            }

            return RedirectToAction(nameof(Projects));
        }

        // POST: /Admin/ExtractText
        // Admin form ka JavaScript file chunte hi yahan bhejta hai. Hum title aur abstract nikal kar wapas dete hain.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtractText(IFormFile? document)
        {
            var fileError = DocumentTextExtractor.Validate(document);
            if (fileError != null)
            {
                return Json(new { ok = false, message = fileError });
            }

            try
            {
                var result = await DocumentTextExtractor.ExtractAsync(
                    document!, _ocr, "Please type or paste the title and abstract yourself.");

                return result.Ok
                    ? Json(new { ok = true, title = result.Title, description = result.Description, note = result.Message })
                    : Json(new { ok = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Admin could not read uploaded file {FileName}", document?.FileName);
                return Json(new
                {
                    ok = false,
                    message = "This file could not be read. It may be damaged or password protected. Please type or paste the text yourself."
                });
            }
        }

        // ---------- helpers ----------

        // Add/Edit form dikhata hai, university ki list ke saath
        private async Task<IActionResult> FormViewAsync(ProjectFormViewModel model)
        {
            try
            {
                ViewBag.Universities = await _projects.GetUniversityNamesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load the university list");
                ViewBag.Universities = new List<string>();
            }

            return View("ProjectForm", model);
        }

        // Copy form values into a data object
        private static FypProject ToProject(ProjectFormViewModel model)
        {
            return new FypProject
            {
                Id = model.Id,
                Title = model.Title.Trim(),
                University = model.University,
                Department = model.Department,
                SupervisorName = (model.SupervisorName ?? string.Empty).Trim(),
                Year = model.Year,
                Description = (model.Description ?? string.Empty).Trim(),
                SourceUrl = model.SourceUrl,
                Area = model.Area,
                IsAward = model.IsAward,
                AwardText = model.AwardText
            };
        }
    }
}