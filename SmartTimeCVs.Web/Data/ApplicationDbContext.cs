using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace SmartTimeCVs.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<JobApplication> JobApplication { get; set; }
        public DbSet<University> University { get; set; }
        public DbSet<Course> Course { get; set; }
        public DbSet<WorkExperience> WorkExperience { get; set; }
        public DbSet<GenderType> GenderType { get; set; }
        public DbSet<LevelType> LevelType { get; set; }
        public DbSet<MaritalStatusType> MaritalStatusType { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<JobApplication>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");
            builder.Entity<University>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");
            builder.Entity<Course>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");
            builder.Entity<WorkExperience>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");

            builder.Entity<JobApplication>()
                .HasOne(j => j.EnglishLevel)
                .WithMany(l => l.JobApplications)
                .HasForeignKey(j => j.EnglishLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobApplication>()
                .HasOne(j => j.OtherLanguageLevel)
                .WithMany()
                .HasForeignKey(j => j.OtherLanguageLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobApplication>()
                .HasOne(j => j.ComputerSkillsLevel)
                .WithMany()
                .HasForeignKey(j => j.ComputerSkillsLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobApplication>()
                .HasOne(j => j.Gender)
                .WithMany(g => g.JobApplications)
                .HasForeignKey(j => j.GenderId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(builder);
        }
    }
}
