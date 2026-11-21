using System.Security.Claims;
using FypTracker.Data;
using FypTracker.Entities;
using FypTracker.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FypTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        // ASP.NET ka tayyar password hasher: salt khud lagata hai aur hash banata hai
        private readonly PasswordHasher<AppUser> _hasher = new();

        public AccountController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // /Account/Choose
        public IActionResult Choose()
        {
            return View();
        }

        // ---------------- STUDENT ----------------

        [HttpGet]
        public IActionResult StudentLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StudentLogin(StudentLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.Role == UserRole.Student);

            if (user == null || !PasswordIsCorrect(user, model.Password))
            {
                // Ek hi paigham: hacker ko pata na chale ke email sahi tha ya password
                ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
                return View(model);
            }

            await SignInUserAsync(user);
            return RedirectToAction("Upload", "Check");
        }

        [HttpGet]
        public IActionResult StudentRegister()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StudentRegister(StudentRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var rollNumber = model.RollNumber.Trim();

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                return View(model);
            }

            if (await _db.Users.AnyAsync(u => u.RollNumber == rollNumber))
            {
                ModelState.AddModelError(nameof(model.RollNumber), "This roll number is already registered.");
                return View(model);
            }

            var department = await _db.Departments.FirstOrDefaultAsync(d => d.Name == model.Department);
            if (department == null)
            {
                ModelState.AddModelError(nameof(model.Department), "Please select a valid department.");
                return View(model);
            }

            var user = new AppUser
            {
                FullName = model.FullName.Trim(),
                Email = email,
                RollNumber = rollNumber,
                Role = UserRole.Student,
                DepartmentId = department.Id
            };
            user.PasswordHash = _hasher.HashPassword(user, model.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Registration successful! Please log in.";
            return RedirectToAction(nameof(StudentLogin));
        }

        // ---------------- ADMIN ----------------

        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var username = model.Username.Trim();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Role == UserRole.Admin);

            if (user == null || !PasswordIsCorrect(user, model.Password))
            {
                ModelState.AddModelError(string.Empty, "Username or password is incorrect.");
                return View(model);
            }

            await SignInUserAsync(user);
            return RedirectToAction("Projects", "Admin");
        }

        [HttpGet]
        public IActionResult AdminRegister()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminRegister(AdminRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Secret code configuration se aata hai. Agar wahan khali hai to koi admin nahi ban sakta.
            var secretCode = _config["AdminAccessCode"];
            if (string.IsNullOrWhiteSpace(secretCode) || model.AccessCode != secretCode)
            {
                ModelState.AddModelError(nameof(model.AccessCode), "Invalid admin access code.");
                return View(model);
            }

            var username = model.Username.Trim();

            if (await _db.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError(nameof(model.Username), "This username is already taken.");
                return View(model);
            }

            var user = new AppUser
            {
                FullName = model.FullName.Trim(),
                Username = username,
                Role = UserRole.Admin
            };
            user.PasswordHash = _hasher.HashPassword(user, model.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Admin account created! Please log in.";
            return RedirectToAction(nameof(AdminLogin));
        }

        // ---------------- LOGOUT ----------------

        // POST, taake koi doosri website sirf link se aap ko logout na kar sake
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Choose));
        }

        // Shown when a logged-in user opens a page their role is not allowed to use
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ---------------- helpers ----------------

        private bool PasswordIsCorrect(AppUser user, string password)
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            return result != PasswordVerificationResult.Failed;
        }

        // Cookie banata hai: id, naam aur role
        private async Task SignInUserAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString())   // "Student" ya "Admin"
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        }
    }
}