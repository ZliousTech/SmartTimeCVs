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
        public DbSet<InterviewSchedule> InterviewSchedule { get; set; }
        public DbSet<University> University { get; set; }
        public DbSet<Course> Course { get; set; }
        public DbSet<WorkExperience> WorkExperience { get; set; }
        public DbSet<GenderType> GenderType { get; set; }
        public DbSet<LevelType> LevelType { get; set; }
        public DbSet<MaritalStatusType> MaritalStatusType { get; set; }
        public DbSet<AttachmentFile> AttachmentFile { get; set; }
        public DbSet<JobOffer> JobOffer { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractType> ContractTypes { get; set; }
        public DbSet<ContractCategory> ContractCategories { get; set; }
        public DbSet<DocumentRequirementLookup> DocumentRequirementLookups { get; set; }
        public DbSet<ContractAttachment> ContractAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<JobApplication>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");
            builder.Entity<JobApplication>().Property(p => p.ExpectedSalary).HasPrecision(8);
            builder.Entity<University>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");
            builder.Entity<Course>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");
            builder.Entity<WorkExperience>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");
            builder.Entity<AttachmentFile>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");
            builder.Entity<Contract>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");
            builder.Entity<ContractAttachment>().Property(p => p.CreatedOn).HasDefaultValueSql("GETDATE()");
            builder.Entity<Contract>().Property(p => p.MonthlySalary).HasPrecision(18, 2);
            builder.Entity<JobApplication>().Property(p => p.IsShortListed);
            builder.Entity<JobApplication>().Property(p => p.IsExcluded);
            builder.Entity<JobApplication>().Property(p => p.IsHolding);
            builder.Entity<JobApplication>().Property(p => p.IsImported).HasDefaultValue(false);
            

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

            builder.Entity<JobOffer>()
                .HasOne(o => o.JobApplication)
                .WithOne(j => j.JobOffer)
                .HasForeignKey<JobOffer>(o => o.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(builder);
        }
    }
}
