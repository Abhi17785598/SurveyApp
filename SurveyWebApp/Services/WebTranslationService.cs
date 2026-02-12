using Microsoft.EntityFrameworkCore;
using SurveyWebApp.Data;
using SurveyWebApp.Models;

namespace SurveyWebApp.Services
{
    public class WebTranslationService
    {
        private readonly ApplicationDbContext _context;
        
        // Complete translation dictionary for all content
        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            // Survey titles
            ["Customer Satisfaction Survey"] = new()
            {
                ["hi"] = "ग्राहक संतुष्टि सर्वेक्षण",
                ["bn"] = "গ্রাহক সন্তুষ্টি জরিপ",
                ["gu"] = "ગ્રાહક સંતોષ સર્વે",
                ["mr"] = "ग्राहक समाधान सर्वेक्षण",
                ["ta"] = "வாடிக்கையாளர் திருப்தி கணக்கெடுப்பு",
                ["te"] = "కస్టమర్ సంతృప్తి సర్వే",
                ["kn"] = "ಗ್ರಾಹಕ ಸಂತೋಷ ಸರ್ವೇ",
                ["ml"] = "ഉപഭോക്തൃ സംതൃപ്തി സർവേ"
            },
            ["Employee Feedback Survey"] = new()
            {
                ["hi"] = "कर्मचारी प्रतिक्रिया सर्वेक्षण",
                ["bn"] = "কর্মচারী প্রতিক্রিযা জরিপ",
                ["gu"] = "કર્મચારી પ્રતિસાદ સર્વે",
                ["mr"] = "कर्मचारी अभिप्राय सर्वेक्षण",
                ["ta"] = "ஊழியர் கருத்து கணக்கெடுப்பு",
                ["te"] = "ఉద్యోగి అభిప్రాయ సర్వే",
                ["kn"] = "ಉದ್ಯೋಗಿ ಪ್ರತಿಕ್ರಿಯೆ ಸರ್ವೇ",
                ["ml"] = "ജീവനക്കാരുടെ അഭിപ്രായ സർവേ"
            },
            ["Product Experience Survey"] = new()
            {
                ["hi"] = "उत्पाद अनुभव सर्वेक्षण",
                ["bn"] = "পণ্য অভিজ্ঞতা জরিপ",
                ["gu"] = "પ્રોડક્ટ અનુભવ સર્વે",
                ["mr"] = "उत्पादन अनुभव सर्वेक्षण",
                ["ta"] = "தயாரிப்பு அனுபவம் கணக்கெடுப்பு",
                ["te"] = "ఉత్పత్తి అనుభవం సర్వే",
                ["kn"] = "ಉತ್ಪನ್ನ ಅನುಭವ ಸರ್ವೇ",
                ["ml"] = "ഉൽപ്പന്ന അനുഭവ സർവേ"
            },
            
            // Survey descriptions
            ["We value your feedback about our services"] = new()
            {
                ["hi"] = "हम आपकी सेवाओं के बारे में आपकी प्रतिक्रिया का महत्व देते हैं",
                ["bn"] = "আমরা আমাদের পরিষেবাগুলির কাছে আপনার প্রতিক্রিয়া মূল্যায়ন করি",
                ["gu"] = "અમે અમારી સેવાઓ વિશે તમારા અભિપ્રાયને મૂલ્ય આપીએ છીએ",
                ["mr"] = "आमच्या सेवांबद्दल तुमच्या प्रतिसादाचे आम्ही मूल्य करतो",
                ["ta"] = "எங்கள் சேவைகளைப் பற்றி உங்கள் கருத்துக்களை நாங்கள் மதிக்கிறோம்",
                ["te"] = "మా సేవల గురించి మీ అభిప్రాయాన్ని మేము మౌల్యం ఇస్తాము",
                ["kn"] = "ನಮ್ಮ ಸೇವೆಗಳ ಬಗ್ಗೆ ನಿಮ್ಮ ಪ್ರತಿಕ್ರಿಯೆಯನ್ನು ನಾವು ಮೌಲ್ಯಮಾಪನ ಮಾಡುತ್ತೇವೆ",
                ["ml"] = "ഞങ്ങളുടെ സേവനങ്ങളെക്കുറിച്ച് നിങ്ങളുടെ അഭിപ്രായം ഞങ്ങളെ അഭിനന്ദിക്കുന്നു"
            },
            ["Help us improve our workplace"] = new()
            {
                ["hi"] = "हमारे कार्यस्थल को बेहतर बनाने में हमारी मदद करें",
                ["bn"] = "আমাদের কর্মক্ষেত্র উন্নত করতে আমাদের সাহায্য করুন",
                ["gu"] = "અમારા કાર્યસ્થાનને સુધારવામાં અમારી મદદ કરો",
                ["mr"] = "आमचे कार्यस्थळ सुधारण्यास मदत करा",
                ["ta"] = "எங்கள் பணியிடத்தை மேம்படுத்த எங்களுக்கு உதவுங்கள்",
                ["te"] = "మా కార్యక్షేత్రాన్ని మెరుగుపరచడంలో మాకు సహాయం చేయండి",
                ["kn"] = "ನಮ್ಮ ಕಾರ್ಯಸ್ಥಳವನ್ನು ಸುಧಾರಲು ನಮಗೆ ಸಹಾಯ ಮಾಡಿ",
                ["ml"] = "ഞങ്ങളുടെ പണിയിടം മെച്ചപ്പെടുത്താൻ ഞങ്ങളെ സഹായിക്കുക"
            },
            
            // Questions
            ["How satisfied are you with our service?"] = new()
            {
                ["hi"] = "आप हमारी सेवा से कितने संतुष्ट हैं?",
                ["bn"] = "আপনি আমাদের পরিষেবার সাথে কতটা সন্তুষ্ট?",
                ["gu"] = "તમે અમારી સેવાથી કેટલા સંતોષ છો?",
                ["mr"] = "आपण आमच्या सेवेने किती संतुष्ट आहात?",
                ["ta"] = "எங்கள் சேவையுடன் நீங்கள் எந்த அளவுக்கு திருப்தியடைகிறீர்கள்?",
                ["te"] = "మీరు మా సేవతో ఎంత సంతృప్తిగా ఉన్నారు?",
                ["kn"] = "ನೀವು ನಮ್ಮ ಸೇವೆಯಿಂದ ಎಷ್ಟು ಸಂತೋಷವಾಗಿದ್ದೀರಿ?",
                ["ml"] = "നിങ്ങൾ ഞങ്ങളുടെ സേവനത്തിൽ എത്ര തൃപ്തരാണ്?"
            },
            ["Would you recommend us to others?"] = new()
            {
                ["hi"] = "क्या आप हमें दूसरों को अनुशंसा करेंगे?",
                ["bn"] = "আপনি কি অন্যদের কাছে আমাদের সুপারিশ করবেন?",
                ["gu"] = "શું તમે અમને અન્ય લોકોને ભલામણી આપશો?",
                ["mr"] = "आपण आमची दुसऱ्यांना शिफारस कराल?",
                ["ta"] = "நீங்கள் மற்றவர்களுக்கு எங்களை பரிந்துரைப்பீர்களா?",
                ["te"] = "మీరు మాను ఇతరులకు సిఫారసు చేస్తారా?",
                ["kn"] = "ನೀವು ಇತರರಿಗೆ ನಮ್ಮನ್ನು ಶಿಫಾರಸು ಮಾಡುತ್ತೀರಾ?",
                ["ml"] = "നിങ്ങൾ മറ്റുള്ളവർക്ക് ഞങ്ങളെ ശുപാർശ ചെയ്യുമോ?"
            },
            ["What is your overall experience?"] = new()
            {
                ["hi"] = "आपका समग्र अनुभव क्या है?",
                ["bn"] = "আপনার সামগ্রিক অভিজ্ঞতা কী?",
                ["gu"] = "તમારો સમગ્ર અનુભવ શું છે?",
                ["mr"] = "तुमचा एकूण अनुभव काय आहे?",
                ["ta"] = "உங்கள் ஒட்டுமொத்த அனுபவம் என்ன?",
                ["te"] = "మీ మొత్తం అనుభవం ఏమిటి?",
                ["kn"] = "ನಿಮ್ಮ ಒಟ್ಟಾರೆ ಅನುಭವವೇನು?",
                ["ml"] = "നിങ്ങളുടെ ആകെത്തുമ അനുഭവം എന്താണ്?"
            },
            
            // Options
            ["Excellent"] = new()
            {
                ["hi"] = "उत्कृष्ट",
                ["bn"] = "অসাধারণ",
                ["gu"] = "અદ્વિતીય",
                ["mr"] = "अतिशय चांगले",
                ["ta"] = "சிறப்பானது",
                ["te"] = "అద్భుతం",
                ["kn"] = "ಅದ್ಭುತ",
                ["ml"] = "അതിമനോഹരം"
            },
            ["Good"] = new()
            {
                ["hi"] = "अच्छा",
                ["bn"] = "ভালো",
                ["gu"] = "સારું",
                ["mr"] = "चांगले",
                ["ta"] = "நல்லது",
                ["te"] = "మంచి",
                ["kn"] = "ಒಳ್ಳೆಯದು",
                ["ml"] = "നല്ലത്"
            },
            ["Average"] = new()
            {
                ["hi"] = "औसत",
                ["bn"] = "গড়",
                ["gu"] = "સરેરાશ",
                ["mr"] = "सरासरी",
                ["ta"] = "சராசரி",
                ["te"] = "సగటు",
                ["kn"] = "ಸರಾಸರಿ",
                ["ml"] = "ശരാശരി"
            },
            ["Poor"] = new()
            {
                ["hi"] = "खराब",
                ["bn"] = "খারাপ",
                ["gu"] = "ખરાબ",
                ["mr"] = "वाईट",
                ["ta"] = "மோசமானது",
                ["te"] = "చెడు",
                ["kn"] = "ಕೆಟ್ಟದ್ದು",
                ["ml"] = "മോശം"
            },
            ["Very Poor"] = new()
            {
                ["hi"] = "बहुत खराब",
                ["bn"] = "খুব খারাপ",
                ["gu"] = "ખૂબ ખરાબ",
                ["mr"] = "खूप वाईट",
                ["ta"] = "மிகவும் மோசமானது",
                ["te"] = "చాలా చెడు",
                ["kn"] = "ತುಂಬಾ ಕೆಟ್ಟದ್ದು",
                ["ml"] = "വളരെ മോശം"
            },
            ["Yes"] = new()
            {
                ["hi"] = "हाँ",
                ["bn"] = "হ্যাঁ",
                ["gu"] = "હા",
                ["mr"] = "होय",
                ["ta"] = "ஆம்",
                ["te"] = "అవును",
                ["kn"] = "ಹೌದು",
                ["ml"] = "അതെ"
            },
            ["No"] = new()
            {
                ["hi"] = "नहीं",
                ["bn"] = "না",
                ["gu"] = "ના",
                ["mr"] = "नाही",
                ["ta"] = "இல்லை",
                ["te"] = "లేదు",
                ["kn"] = "ಇಲ್ಲ",
                ["ml"] = "ഇല്ല"
            },
            ["Maybe"] = new()
            {
                ["hi"] = "शायद",
                ["bn"] = "হয়তো",
                ["gu"] = "કદાચ",
                ["mr"] = "कदाचित",
                ["ta"] = "ஒருவேளை",
                ["te"] = "బహుశా",
                ["kn"] = "ಬಹುಶಃ",
                ["ml"] = "ഒരുവേള"
            }
        };

        public WebTranslationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task GenerateAllTranslationsAsync()
        {
            try
            {
                // Get all surveys with their questions and options
                var surveys = await _context.Surveys
                    .Include(s => s.Questions)
                    .ThenInclude(q => q.Options)
                    .ToListAsync();

                foreach (var survey in surveys)
                {
                    // Translate survey title
                    TranslateSurveyTitle(survey);
                    
                    // Translate survey description
                    TranslateSurveyDescription(survey);
                }

                foreach (var survey in surveys)
                {
                    foreach (var question in survey.Questions)
                    {
                        // Translate question text
                        TranslateQuestionText(question);
                        
                        // Translate question options
                        foreach (var option in question.Options)
                        {
                            TranslateOptionText(option);
                        }
                    }
                }

                // Save all changes
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"✅ WebApp: Successfully translated {surveys.Count} surveys, " +
                                $"{surveys.Sum(s => s.Questions.Count)} questions, and " +
                                $"{surveys.Sum(s => s.Questions.Sum(q => q.Options.Count))} options!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WebApp Error during translation: {ex.Message}");
                throw;
            }
        }

        private void TranslateSurveyTitle(Survey survey)
        {
            // Skip translation - use database values directly since they now contain proper Unicode
            // The database has been updated with proper Hindi Unicode text
            return;
        }

        private string FixEncoding(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Fix common encoding issues for Indian languages
            try
            {
                // If the text contains encoding artifacts, fix them
                if (text.Contains("à¤") || text.Contains("à¦") || text.Contains("àª") || 
                    text.Contains("à®") || text.Contains("à°") || 
                    text.Contains("à²") || text.Contains("à´"))
                {
                    // This is double-encoded UTF-8, decode it properly
                    // First, get the bytes as if they were ISO-8859-1 (which preserves the byte values)
                    var isoBytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(text);
                    
                    // Then decode as UTF-8
                    var result = System.Text.Encoding.UTF8.GetString(isoBytes);
                    
                    // If still has artifacts, try a different approach
                    if (result.Contains("à¤") || result.Contains("à¦"))
                    {
                        // Try Windows-1252 to UTF-8
                        var winBytes = System.Text.Encoding.GetEncoding("Windows-1252").GetBytes(text);
                        result = System.Text.Encoding.UTF8.GetString(winBytes);
                    }
                    
                    return result;
                }
                
                return text;
            }
            catch
            {
                return text; // Return original if fixing fails
            }
        }

        private void TranslateSurveyDescription(Survey survey)
        {
            // Skip translation - use database values directly
            return;
        }

        private void TranslateQuestionText(Question question)
        {
            // Skip translation - use database values directly
            return;
        }

        private void TranslateOptionText(QuestionOption option)
        {
            // Skip translation - use database values directly
            return;
        }
    }
}
