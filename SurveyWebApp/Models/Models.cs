using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SurveyWebApp.Models
{
    public enum QuestionType
    {
        SingleChoice = 0,
        MultipleChoice = 1,
        TrueFalse = 2,
        Descriptive = 3,
        Scale = 4,
        LongText = 5,
        Rating = 6,
        Dropdown = 7
    }

    public class Survey
    {
        public int Id { get; set; }
        
        [NotMapped]
        public string Name { get => Title; set => Title = value; }
        
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Multi-language support
        public string Language { get; set; } = "en";
        public string? Title_Hi { get; set; }  // Hindi
        public string? Title_Bn { get; set; }  // Bengali
        public string? Title_Gu { get; set; }  // Gujarati
        public string? Title_Mr { get; set; }  // Marathi
        public string? Title_Ta { get; set; }  // Tamil
        public string? Title_Te { get; set; }  // Telugu
        public string? Title_Kn { get; set; }  // Kannada
        public string? Title_Ml { get; set; }  // Malayalam
        
        // Description translations for all 8 Indian languages
        public string? Description_Hi { get; set; }  // Hindi
        public string? Description_Bn { get; set; }  // Bengali
        public string? Description_Gu { get; set; }  // Gujarati
        public string? Description_Mr { get; set; }  // Marathi
        public string? Description_Ta { get; set; }  // Tamil
        public string? Description_Te { get; set; }  // Telugu
        public string? Description_Kn { get; set; }  // Kannada
        public string? Description_Ml { get; set; }  // Malayalam
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
        public bool IsActive { get; set; } = true;
        
        [NotMapped]
        public string Status { get; set; } = "Draft";
        
        [NotMapped]
        public DateTime? StartDate { get; set; }
        
        [NotMapped]
        public DateTime? EndDate { get; set; }
        
        // Relationships
        public int CreatedById { get; set; }
        
        [ForeignKey("CreatedById")]
        public UserModel CreatedBy { get; set; } = null!;
        
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Response> Responses { get; set; } = new List<Response>();
        
        // State management
        public string? AllowedStates { get; set; }
        public int? MaxResponses { get; set; }
        
        // Helper properties
        [NotMapped]
        public bool IsPublished => PublishedAt.HasValue && PublishedAt <= DateTime.UtcNow;
        
        [NotMapped]
        public int ResponseCount => Responses?.Count ?? 0;
        
        public bool IsStateAllowed(string state)
        {
            if (string.IsNullOrEmpty(AllowedStates) || AllowedStates.Trim() == "ALL")
                return true;
                
            var states = AllowedStates.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return Array.Exists(states, s => s.Equals(state, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class Question
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        
        // Multi-language support
        public string? QuestionText_Hi { get; set; }
        public string? QuestionText_Bn { get; set; }
        public string? QuestionText_Gu { get; set; }
        public string? QuestionText_Mr { get; set; }
        public string? QuestionText_Ta { get; set; }
        public string? QuestionText_Te { get; set; }
        public string? QuestionText_Kn { get; set; }
        public string? QuestionText_Ml { get; set; }
        
        public QuestionType Type { get; set; }
        public bool IsRequired { get; set; }
        public int Order { get; set; }
        public int SurveyId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Additional properties
        public string? Placeholder { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        
        [ForeignKey("SurveyId")]
        public Survey Survey { get; set; } = null!;
        
        public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    }

    public class QuestionOption
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        
        // Multi-language support
        public string? OptionText_Hi { get; set; }
        public string? OptionText_Bn { get; set; }
        public string? OptionText_Gu { get; set; }
        public string? OptionText_Mr { get; set; }
        public string? OptionText_Ta { get; set; }
        public string? OptionText_Te { get; set; }
        public string? OptionText_Kn { get; set; }
        public string? OptionText_Ml { get; set; }
        
        public int QuestionId { get; set; }
        public int Order { get; set; }
        public bool IsCorrect { get; set; }
        public int? ScaleValue { get; set; }
        
        [ForeignKey("QuestionId")]
        public Question Question { get; set; } = null!;
    }

    public class Response
    {
        public int Id { get; set; }
        public int SurveyId { get; set; }
        public Survey Survey { get; set; } = null!;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string State { get; set; } = string.Empty; // User's state at time of response
        public string? UserName { get; set; } // User's name who submitted the survey
        public ICollection<ResponseAnswer> Answers { get; set; } = new List<ResponseAnswer>();
    }

    public class ResponseAnswer
    {
        public int Id { get; set; }
        public int ResponseId { get; set; }
        public int QuestionId { get; set; }
        public string? AnswerText { get; set; } // For descriptive answers
        public int? SelectedOptionId { get; set; } // For single/multiple choice
        public int? ScaleValue { get; set; } // For scale questions (1-5, 1-10, etc.)
        
        [ForeignKey("ResponseId")]
        public Response Response { get; set; } = null!;
        
        [ForeignKey("QuestionId")]
        public Question Question { get; set; } = null!;
    }

    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? State { get; set; }
        public bool IsActive { get; set; } = true;
        public string PasswordHash { get; set; } = string.Empty;
        
        public ICollection<Survey> CreatedSurveys { get; set; } = new List<Survey>();
        public ICollection<Response> Responses { get; set; } = new List<Response>();
    }
}
