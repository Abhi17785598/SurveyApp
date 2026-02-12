using Microsoft.EntityFrameworkCore;
using SurveyWebApp.Models;

namespace SurveyWebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Survey> Surveys { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }
        public DbSet<Response> Responses { get; set; }
        public DbSet<ResponseAnswer> ResponseAnswers { get; set; }
        public DbSet<UserModel> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Survey configuration
            modelBuilder.Entity<Survey>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.AllowedStates).HasMaxLength(200);
                entity.HasOne(e => e.CreatedBy).WithMany(u => u.CreatedSurveys).HasForeignKey(e => e.CreatedById);
                
                // Configure Unicode support for multilingual Title columns
                entity.Property(e => e.Title_Hi).IsUnicode(true).HasMaxLength(200);
                entity.Property(e => e.Title_Bn).IsUnicode(true).HasMaxLength(200);
                entity.Property(e => e.Title_Gu).IsUnicode(true).HasMaxLength(200);
                entity.Property(e => e.Title_Mr).IsUnicode(true).HasMaxLength(200);
                entity.Property(e => e.Title_Ta).IsUnicode(true).HasMaxLength(200);
                entity.Property(e => e.Title_Te).IsUnicode(true).HasMaxLength(200);
                entity.Property(e => e.Title_Kn).IsUnicode(true).HasMaxLength(200);
                entity.Property(e => e.Title_Ml).IsUnicode(true).HasMaxLength(200);
                
                // Configure Unicode support for multilingual Description columns
                entity.Property(e => e.Description_Hi).IsUnicode(true).HasMaxLength(1000);
                entity.Property(e => e.Description_Bn).IsUnicode(true).HasMaxLength(1000);
                entity.Property(e => e.Description_Gu).IsUnicode(true).HasMaxLength(1000);
                entity.Property(e => e.Description_Mr).IsUnicode(true).HasMaxLength(1000);
                entity.Property(e => e.Description_Ta).IsUnicode(true).HasMaxLength(1000);
                entity.Property(e => e.Description_Te).IsUnicode(true).HasMaxLength(1000);
                entity.Property(e => e.Description_Kn).IsUnicode(true).HasMaxLength(1000);
                entity.Property(e => e.Description_Ml).IsUnicode(true).HasMaxLength(1000);
            });

            // Question configuration
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Placeholder).HasMaxLength(500);
                entity.HasOne(e => e.Survey).WithMany(s => s.Questions).HasForeignKey(e => e.SurveyId);
            });

            // QuestionOption configuration
            modelBuilder.Entity<QuestionOption>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired().HasMaxLength(500);
                
                // Configure Unicode support for translation columns
                entity.Property(e => e.OptionText_Hi).IsUnicode(true).HasMaxLength(500);
                entity.Property(e => e.OptionText_Bn).IsUnicode(true).HasMaxLength(500);
                entity.Property(e => e.OptionText_Gu).IsUnicode(true).HasMaxLength(500);
                entity.Property(e => e.OptionText_Mr).IsUnicode(true).HasMaxLength(500);
                entity.Property(e => e.OptionText_Ta).IsUnicode(true).HasMaxLength(500);
                entity.Property(e => e.OptionText_Te).IsUnicode(true).HasMaxLength(500);
                entity.Property(e => e.OptionText_Kn).IsUnicode(true).HasMaxLength(500);
                entity.Property(e => e.OptionText_Ml).IsUnicode(true).HasMaxLength(500);
                
                entity.HasOne(e => e.Question).WithMany(q => q.Options).HasForeignKey(e => e.QuestionId);
            });

            // Response configuration
            modelBuilder.Entity<Response>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.State).HasMaxLength(50);
                entity.HasOne(e => e.Survey).WithMany(s => s.Responses).HasForeignKey(e => e.SurveyId);
                entity.Ignore("UserId"); // Ignore UserId shadow property
                entity.Ignore("UserModelId"); // Ignore UserModelId shadow property
                entity.Ignore("UserModelId1"); // Ignore UserModelId1 shadow property
                entity.Ignore("UserModelId2"); // Ignore UserModelId2 shadow property
            });

            // ResponseAnswer configuration
            modelBuilder.Entity<ResponseAnswer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AnswerText).HasMaxLength(1000);
                entity.HasOne(e => e.Response).WithMany(r => r.Answers).HasForeignKey(e => e.ResponseId);
                entity.HasOne(e => e.Question).WithMany().HasForeignKey(e => e.QuestionId);
            });

            // UserModel configuration
            modelBuilder.Entity<UserModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.FirstName).HasMaxLength(50);
                entity.Property(e => e.LastName).HasMaxLength(50);
                entity.Property(e => e.State).HasMaxLength(50);
                entity.HasIndex(e => e.Username).IsUnique();
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Hash password for admin user
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var adminPasswordHash = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes("admin123")));
            
            // Create admin user
            var adminUser = new UserModel
            {
                Id = 1,
                Username = "admin",
                Email = "admin@survey.com",
                PasswordHash = adminPasswordHash,
                FirstName = "Admin",
                LastName = "User",
                IsActive = true
            };

            modelBuilder.Entity<UserModel>().HasData(adminUser);

            // Create sample survey
            var sampleSurvey = new Survey
            {
                Id = 1,
                Title = "Sample Programming Quiz",
                Description = "Test your programming knowledge with this basic quiz.",
                CreatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedById = adminUser.Id,
                Status = "Published",
                AllowedStates = "ALL"
            };

            modelBuilder.Entity<Survey>().HasData(sampleSurvey);

            // Create sample questions
            var questions = new[]
            {
                new Question
                {
                    Id = 1,
                    SurveyId = 1,
                    Text = "What is your primary programming language?",
                    Type = QuestionType.SingleChoice,
                    Order = 1,
                    IsRequired = true
                },
                new Question
                {
                    Id = 2,
                    SurveyId = 1,
                    Text = "How many years of programming experience do you have?",
                    Type = QuestionType.SingleChoice,
                    Order = 2,
                    IsRequired = true
                },
                new Question
                {
                    Id = 3,
                    SurveyId = 1,
                    Text = "Which programming paradigm do you prefer?",
                    Type = QuestionType.MultipleChoice,
                    Order = 3,
                    IsRequired = true
                }
            };
            modelBuilder.Entity<Question>().HasData(questions);

            // Create options for questions
            var options = new[]
            {
                // Options for question 1
                new QuestionOption { Id = 1, QuestionId = 1, Text = "JavaScript", Order = 1 },
                new QuestionOption { Id = 2, QuestionId = 1, Text = "Python", Order = 2 },
                new QuestionOption { Id = 3, QuestionId = 1, Text = "Java", Order = 3 },
                new QuestionOption { Id = 4, QuestionId = 1, Text = "C#", Order = 4 },
                new QuestionOption { Id = 5, QuestionId = 1, Text = "C++", Order = 5 },
                
                // Options for question 2
                new QuestionOption { Id = 6, QuestionId = 2, Text = "Less than 1 year", Order = 1 },
                new QuestionOption { Id = 7, QuestionId = 2, Text = "1-3 years", Order = 2 },
                new QuestionOption { Id = 8, QuestionId = 2, Text = "3-5 years", Order = 3 },
                new QuestionOption { Id = 9, QuestionId = 2, Text = "5-10 years", Order = 4 },
                new QuestionOption { Id = 10, QuestionId = 2, Text = "More than 10 years", Order = 5 },
                
                // Options for question 3
                new QuestionOption { Id = 11, QuestionId = 3, Text = "Object-Oriented Programming", Order = 1 },
                new QuestionOption { Id = 12, QuestionId = 3, Text = "Functional Programming", Order = 2 },
                new QuestionOption { Id = 13, QuestionId = 3, Text = "Procedural Programming", Order = 3 }
            };
            modelBuilder.Entity<QuestionOption>().HasData(options);
        }
    }
}
