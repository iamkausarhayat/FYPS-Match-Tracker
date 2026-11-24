using FypTracker.Models;
using FypTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace FypTracker.Controllers
{
    // [ApiController]: batata hai ke yeh API hai (HTML page nahi, JSON dega).
    // [Route("api/projects")]: is controller ke sab URL "/api/projects" se shuru honge.
    [ApiController]
    [Route("api/projects")]
    public class ProjectsApiController : ControllerBase
    {
        private readonly IProjectService _projects;

        // Dependency Injection: framework khud IProjectService (database wali) de deta hai
        public ProjectsApiController(IProjectService projects)
        {
            _projects = projects;
        }

        // GET /api/projects?q=camera&department=Computer Science
        [HttpGet]
        public async Task<ActionResult<List<ProjectDto>>> GetAll(string? q, string? department)
        {
            try
            {
                var all = await _projects.GetAllAsync();
                IEnumerable<FypProject> filtered = all;

                if (!string.IsNullOrWhiteSpace(q))
                {
                    filtered = filtered.Where(p =>
                        p.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        p.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                        p.SupervisorName.Contains(q, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(department))
                {
                    filtered = filtered.Where(p =>
                        string.Equals(p.Department, department, StringComparison.OrdinalIgnoreCase));
                }

                var result = filtered
                    .OrderByDescending(p => p.Year)
                    .ThenBy(p => p.Title)
                    .Select(ToDto)
                    .ToList();

                return Ok(result);   // 200 OK
            }
            catch (Exception)
            {
                return DatabaseDown();
            }
        }

        // GET /api/projects/3
        // {id:int} ka matlab: sirf number chalega. "stats" ya "abc" yahan nahi aayenge.
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProjectDto>> GetById(int id)
        {
            try
            {
                var project = await _projects.GetByIdAsync(id);

                if (project == null)
                {
                    // 404 + chhota JSON paigham
                    return NotFound(new { message = $"No project found with id {id}." });
                }

                return Ok(ToDto(project));
            }
            catch (Exception)
            {
                return DatabaseDown();
            }
        }

        // GET /api/projects/stats
        [HttpGet("stats")]
        public async Task<ActionResult<ProjectStatsDto>> GetStats()
        {
            try
            {
                var all = await _projects.GetAllAsync();

                var stats = new ProjectStatsDto
                {
                    TotalProjects = all.Count,
                    DepartmentCount = all.Select(p => p.Department).Distinct().Count(),
                    LatestYear = all.Count == 0 ? 0 : all.Max(p => p.Year),
                    ByDepartment = all
                        .GroupBy(p => p.Department)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(stats);
            }
            catch (Exception)
            {
                return DatabaseDown();
            }
        }

        // ---------- helpers ----------

        // Data object ko DTO (bahar bhejne wali parchi) mein badalta hai
        private static ProjectDto ToDto(FypProject p)
        {
            return new ProjectDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Department = p.Department,
                SupervisorName = p.SupervisorName,
                Year = p.Year,
                University = p.University,
                SourceUrl = p.SourceUrl,
                Area = p.Area,
                IsTitleOnly = p.IsTitleOnly
            };
        }

        // Database band ho to crash nahi, saaf paigham aur 503 code
        private ObjectResult DatabaseDown()
        {
            return StatusCode(503, new { message = "The service is temporarily unavailable. Please try again later." });
        }
    }
}