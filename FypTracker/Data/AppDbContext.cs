using FypTracker.Entities;
using Microsoft.EntityFrameworkCore;

namespace FypTracker.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Each DbSet becomes one table
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<FypProject> FypProjects => Set<FypProject>();
        public DbSet<ExternalPaper> ExternalPapers => Set<ExternalPaper>();
        public DbSet<SimilarityCheck> SimilarityChecks => Set<SimilarityCheck>();
        public DbSet<SimilarityMatch> SimilarityMatches => Set<SimilarityMatch>();

        // A fixed date, because seed data must never change between builds
        private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Department ----------
            modelBuilder.Entity<Department>(e =>
            {
                e.HasIndex(d => d.Name).IsUnique();

                e.HasMany(d => d.Projects)
                 .WithOne(p => p.Department)
                 .HasForeignKey(p => p.DepartmentId)
                 .OnDelete(DeleteBehavior.Restrict);   // a department with projects cannot be deleted

                e.HasMany(d => d.Users)
                 .WithOne(u => u.Department)
                 .HasForeignKey(u => u.DepartmentId)
                 .OnDelete(DeleteBehavior.SetNull);    // deleting a department only clears the users' link
            });

            // ---------- AppUser ----------
            modelBuilder.Entity<AppUser>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();       // two students cannot share an email
                e.HasIndex(u => u.Username).IsUnique();    // two admins cannot share a username

                e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);   // stored as "Student" / "Admin"

                e.HasMany(u => u.Checks)
                 .WithOne(c => c.User)
                 .HasForeignKey(c => c.UserId)
                 .OnDelete(DeleteBehavior.SetNull);        // old checks stay if the user is deleted
            });

            // ---------- FypProject ----------
            modelBuilder.Entity<FypProject>(e =>
            {
                e.HasIndex(p => p.Title).IsUnique();       // same rule as our "title must be unique" check
            });

            // ---------- ExternalPaper ----------
            modelBuilder.Entity<ExternalPaper>(e =>
            {
                e.HasIndex(p => p.ExternalId).IsUnique();  // never save the same paper twice
            });

            // ---------- SimilarityMatch ----------
            modelBuilder.Entity<SimilarityMatch>(e =>
            {
                e.Property(m => m.Source).HasConversion<string>().HasMaxLength(30);

                e.HasOne(m => m.SimilarityCheck)
                 .WithMany(c => c.Matches)
                 .HasForeignKey(m => m.SimilarityCheckId)
                 .OnDelete(DeleteBehavior.Cascade);        // deleting a check deletes its matches

                e.HasOne(m => m.FypProject)
                 .WithMany(p => p.Matches)
                 .HasForeignKey(m => m.FypProjectId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.ExternalPaper)
                 .WithMany(p => p.Matches)
                 .HasForeignKey(m => m.ExternalPaperId)
                 .OnDelete(DeleteBehavior.Cascade);

                // Rule enforced by the database itself: a match points to exactly one target
                e.ToTable(t => t.HasCheckConstraint(
                    "CK_SimilarityMatches_OneTarget",
                    "([FypProjectId] IS NOT NULL AND [ExternalPaperId] IS NULL) OR ([FypProjectId] IS NULL AND [ExternalPaperId] IS NOT NULL)"));
            });

            // ---------- Starting data (seed) ----------
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, Name = "Computer Science" },
                new Department { Id = 2, Name = "Software Engineering" },
                new Department { Id = 3, Name = "Artificial Intelligence" },
                new Department { Id = 4, Name = "Data Science" });

            modelBuilder.Entity<FypProject>().HasData(
                new FypProject
                {
                    Id = 1,
                    DepartmentId = 1,
                    Year = 2023,
                    SupervisorName = "Dr. Imran Shah",
                    CreatedAt = SeedDate,
                    Title = "Automated Class Presence System with Camera-Based Student Identification",
                    Description = "Identifies each student from a live camera feed and records presence in the class register automatically, so teachers no longer call the roll."
                },
                new FypProject
                {
                    Id = 2,
                    DepartmentId = 2,
                    Year = 2022,
                    SupervisorName = "Ms. Hina Malik",
                    CreatedAt = SeedDate,
                    Title = "Online Student Management Portal",
                    Description = "A web portal that stores student records and lets teachers update attendance and marks, while students view their results online."
                },
                new FypProject
                {
                    Id = 3,
                    DepartmentId = 1,
                    Year = 2021,
                    SupervisorName = "Dr. Sana Iqbal",
                    CreatedAt = SeedDate,
                    Title = "Library Seat Booking App",
                    Description = "Students reserve a study seat in advance, and the app releases the seat automatically if they do not arrive on time."
                },
                new FypProject
                {
                    Id = 4,
                    DepartmentId = 3,
                    Year = 2024,
                    SupervisorName = "Dr. Bilal Aslam",
                    CreatedAt = SeedDate,
                    Title = "Crop Disease Detection from Leaf Photos",
                    Description = "Farmers photograph a leaf and a trained model names the likely disease and suggests a treatment, even with a weak internet connection."
                },
                new FypProject
                {
                    Id = 5,
                    DepartmentId = 4,
                    Year = 2024,
                    SupervisorName = "Ms. Ayesha Noor",
                    CreatedAt = SeedDate,
                    Title = "Hospital Appointment Reminder and No-Show Predictor",
                    Description = "Predicts which patients are likely to miss an appointment and sends them reminders, helping clinics reduce empty time slots."
                },
                new FypProject
                {
                    Id = 6,
                    DepartmentId = 3,
                    Year = 2023,
                    SupervisorName = "Dr. Farhan Ali",
                    CreatedAt = SeedDate,
                    Title = "Campus Navigation Assistant for Visually Impaired Students",
                    Description = "A voice-guided mobile app that describes routes between campus buildings and warns users about obstacles and stairs."
                },
                new FypProject
                {
                    Id = 7,
                    DepartmentId = 2,
                    Year = 2022,
                    SupervisorName = "Mr. Usman Tariq",
                    CreatedAt = SeedDate,
                    Title = "Food Waste Reduction Platform for Local Restaurants",
                    Description = "Restaurants list surplus meals at a discount and nearby users reserve them, so edible food is not thrown away at closing time."
                },
                new FypProject
                {
                    Id = 8,
                    DepartmentId = 4,
                    Year = 2025,
                    SupervisorName = "Dr. Sadia Khan",
                    CreatedAt = SeedDate,
                    Title = "Public Transport Delay Prediction and Route Planner",
                    Description = "Uses past bus timings and weather data to predict delays and suggests the fastest route for daily commuters."
                });
        }
    }
}