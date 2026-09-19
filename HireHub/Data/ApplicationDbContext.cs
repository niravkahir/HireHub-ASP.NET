using HireHub.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HireHub.Data
{
    // Inherit from IdentityDbContext<ApplicationUser>, NOT plain DbContext
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Job> Jobs { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<SavedJob> SavedJobs { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<JobSeekerProfile> JobSeekerProfiles { get; set; }
        public DbSet<RecruiterProfile> RecruiterProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);   

            // One-to-one: ApplicationUser ↔ JobSeekerProfile
            builder.Entity<JobSeekerProfile>()
                .HasOne(p => p.User)
                .WithOne(u => u.JobSeekerProfile)
                .HasForeignKey<JobSeekerProfile>(p => p.UserId);

            // One-to-one: ApplicationUser ↔ RecruiterProfile
            builder.Entity<RecruiterProfile>()
                .HasOne(p => p.User)
                .WithOne(u => u.RecruiterProfile)
                .HasForeignKey<RecruiterProfile>(p => p.UserId);

            // Prevent duplicate applications (same user + same job)
            builder.Entity<JobApplication>()
                .HasIndex(a => new { a.JobId, a.JobSeekerProfileId })
                .IsUnique();

            // Prevent duplicate saves
            builder.Entity<SavedJob>()
                .HasIndex(s => new { s.JobId, s.JobSeekerProfileId })
                .IsUnique();

            builder.Entity<Job>()
                .Property(j => j.SalaryMin)
                .HasPrecision(18, 2);   

            builder.Entity<Job>()
                .Property(j => j.SalaryMax)
                .HasPrecision(18, 2);
        }
    }
}