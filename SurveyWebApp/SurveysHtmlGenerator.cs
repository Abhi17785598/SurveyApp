using System.Text;
using System.Linq;
using SurveyWebApp.Models;

namespace SurveyWebApp
{
    public static class SurveysHtmlGenerator
    {
        public static string GenerateSurveysHtml(string language, string username, IEnumerable<Survey> surveys)
        {
            var sb = new StringBuilder();

            sb.Append($"<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>All Surveys</title>");
            sb.Append("<link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile.css?v={DateTime.Now.Ticks}' rel='stylesheet'></head><body>");
            
            // Add mobile-specific styles
            sb.Append("<style>");
            sb.Append("@media (max-width: 768px) {");
            sb.Append("  .survey-cards-page { padding: calc(80px + env(safe-area-inset-top)) 16px 32px; }");
            sb.Append("  .grid { grid-template-columns: 1fr; gap: 16px; }");
            sb.Append("  .survey-card { padding: 20px; }");
            sb.Append("  .btn-primary { width: 100%; padding: 15px; font-size: 16px; }");
            sb.Append("}");
            sb.Append("@media (max-width: 480px) {");
            sb.Append("  .survey-cards-page { padding: calc(70px + env(safe-area-inset-top)) 12px 24px; }");
            sb.Append("  .survey-card { padding: 16px; }");
            sb.Append("  .page-title { font-size: 24px; }");
            sb.Append("}");
            sb.Append("</style>");

            sb.Append("<header class='app-header'>");
            sb.Append("<div class='brand-logo'><img src='/assets/apptad-technologies-pvt-ltd-original-removebg-preview.png' class='logo' /></div>");
            sb.Append("<div class='brand-title'>SURVEY PANEL</div>");
            sb.Append("<div class='header-actions'>");
            sb.Append("<button class='icon-btn' onclick='toggleSettings()'>⚙️</button>");
            sb.Append("<button class='icon-btn' onclick='toggleAccount()'>👤</button>");
            sb.Append("</div></header>");

            sb.Append("<div id='searchBar' class='search-bar'>");
            sb.Append($"<input class='search-input' placeholder='{GetLocalizedPlaceholder(language)}' onkeyup='filterSurveys(this.value)' />");
            sb.Append("</div>");

            sb.Append("<div id='settingsMenu' class='dropdown'>");
            sb.Append($"<div class='dropdown-title'>⚙️ {GetLocalizedText("Settings", language)}</div>");
            sb.Append("<div class='dropdown-item' onclick='toggleSearch()'>");
            sb.Append("<span class='item-icon'>🔍</span>");
            sb.Append($"<span class='item-text'>{GetLocalizedText("Search", language)}</span>");
            sb.Append("</div>");
            sb.Append("<div class='dropdown-item' onclick='toggleLanguageDropdown()'>");
            sb.Append("<span class='item-icon'>🌐</span>");
            sb.Append($"<span class='item-text'>{GetLocalizedText("Language", language)}</span>");
            sb.Append("<span class='dropdown-arrow'>▼</span>");
            sb.Append("</div>");
            sb.Append("<div id='languageDropdown' class='language-dropdown' style='display: none;'>");
            sb.Append("<div class='language-option' onclick='selectLanguage(\"en\")'>English (English)</div>");
            sb.Append("<div class='language-option' onclick='selectLanguage(\"hi\")'>हिन्दी (Hindi)</div>");
            sb.Append("<div class='language-option' onclick='selectLanguage(\"bn\")'>বাংলা (Bengali)</div>");
            sb.Append("<div class='language-option' onclick='selectLanguage(\"gu\")'>ગુજરાતી (Gujarati)</div>");
            sb.Append("<div class='language-option' onclick='selectLanguage(\"mr\")'>मराठी (Marathi)</div>");
            sb.Append("<div class='language-option' onclick='selectLanguage(\"ta\")'>தமிழ் (Tamil)</div>");
            sb.Append("<div class='language-option' onclick='selectLanguage(\"te\")'>తెలుగు (Telugu)</div>");
            sb.Append("<div class='language-option' onclick='selectLanguage(\"kn\")'>ಕನ್ನಡ (Kannada)</div>");
            sb.Append("<div class='language-option' onclick='selectLanguage(\"ml\")'>മലയാളം (Malayalam)</div>");
            sb.Append("</div>");
            sb.Append($"<a href='/logout' class='dropdown-item logout-item'>");
            sb.Append("<span class='item-icon'>🚪</span>");
            sb.Append($"<span class='item-text'>{GetLocalizedText("Logout", language)}</span>");
            sb.Append("</a></div>");

            sb.Append("<div id='accountMenu' class='dropdown'>");
            sb.Append($"<div class='dropdown-title'>{GetLocalizedText("Account", language)}</div>");
            sb.Append($"<a>👤 {System.Net.WebUtility.HtmlEncode(username ?? "User")}</a>");
            sb.Append("</div>");

            sb.Append("<main class='survey-cards-page'>");
            sb.Append("<div class='page-header'>");
            sb.Append($"<h2 class='page-title'>{GetLocalizedText("All Surveys", language)}</h2>");
            sb.Append("</div>");
            sb.Append("<div class='grid'>");

            foreach (var s in surveys)
            {
                var localizedTitle = GetLocalizedSurveyTitle(s, language);
                var localizedDescription = GetLocalizedSurveyDescription(s, language);
                
                sb.Append("<div class='survey-card'>");
                sb.Append("<div class='card-head'>");
                sb.Append($"<h5>{System.Net.WebUtility.HtmlEncode(localizedTitle)}</h5>");
                sb.Append("</div>");
                sb.Append($"<p>{System.Net.WebUtility.HtmlEncode(localizedDescription ?? "")}</p>");
                sb.Append($"<a href='/survey/{s.Id}' class='btn-primary'>{GetLocalizedText("Start Survey", language)}</a>");
                sb.Append("</div>");
            }

            sb.Append("</div>");
            sb.Append("</main>");

            sb.Append("<script>");
            sb.Append("function toggleSearch() { toggle('searchBar'); }");
            sb.Append("function toggleSettings() { toggle('settingsMenu'); }");
            sb.Append("function toggleAccount() { toggle('accountMenu'); }");
            sb.Append("function toggle(id) { document.querySelectorAll('.dropdown').forEach(d => d.style.display = 'none'); const el = document.getElementById(id); if (el) el.style.display = 'block'; }");
            sb.Append("function toggleLanguageDropdown() { const el = document.getElementById('languageDropdown'); el.style.display = el.style.display === 'block' ? 'none' : 'block'; document.querySelector('.dropdown-arrow').textContent = el.style.display === 'block' ? '▲' : '▼'; }");
            sb.Append("function selectLanguage(lang) { window.location.href = `/?lang=${lang}`; }");
            sb.Append("document.addEventListener('click', function (e) { if (!e.target.closest('.icon-btn') && !e.target.closest('.dropdown') && !e.target.closest('.language-dropdown')) { document.querySelectorAll('.dropdown').forEach(d => d.style.display = 'none'); document.getElementById('languageDropdown').style.display = 'none'; const arrow = document.querySelector('.dropdown-arrow'); if (arrow) arrow.textContent = '▼'; } });");
            sb.Append("function filterSurveys(q) { q = q.toLowerCase(); document.querySelectorAll('.survey-card').forEach(card => { card.style.display = card.innerText.toLowerCase().includes(q) ? 'block' : 'none'; }); }");
            sb.Append("</script>");

            sb.Append("</body></html>");

            return sb.ToString();
        }

        private static string GetLocalizedText(string text, string language)
        {
            if (language == "en") return text;
            
            var translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["Settings"] = new() { ["hi"] = "सेटिंग्स", ["bn"] = "সেটিংস", ["gu"] = "સેટિંગ્સ", ["mr"] = "सेटिंग्स", ["ta"] = "அமைப்புகள்", ["te"] = "సెట్టింగ్‌లు", ["kn"] = "ಸೆಟ್ಟಿಂಗ್‌ಗಳು", ["ml"] = "ക്രമീകരണങ്ങൾ" },
                ["Search"] = new() { ["hi"] = "खोजें", ["bn"] = "অনুসন্ধান করুন", ["gu"] = "શોધો", ["mr"] = "शोधा", ["ta"] = "தேடுக", ["te"] = "శోధించండి", ["kn"] = "ಹುಡುಕಿ", ["ml"] = "തിരയുക" },
                ["Language"] = new() { ["hi"] = "भाषा", ["bn"] = "ভাষা", ["gu"] = "ભાષા", ["mr"] = "भाषा", ["ta"] = "மொழி", ["te"] = "భాష", ["kn"] = "ಭಾಷೆ", ["ml"] = "ഭാഷ" },
                ["Logout"] = new() { ["hi"] = "लॉगआउट", ["bn"] = "লগআউট", ["gu"] = "લૉગઆઉટ", ["mr"] = "लॉगआउट", ["ta"] = "வெளியேறு", ["te"] = "లాగ్‌అవుట్", ["kn"] = "ಲಾಗ್‌ಔಟ್", ["ml"] = "ലോഗൗട്ട്" },
                ["Account"] = new() { ["hi"] = "खाता", ["bn"] = "অ্যাকাউন্ট", ["gu"] = "એકાઉન્ટ", ["mr"] = "खाते", ["ta"] = "கணக்கு", ["te"] = "ఖాతా", ["kn"] = "ಖಾತೆ", ["ml"] = "അക്കൗണ്ട്" },
                ["All Surveys"] = new() { ["hi"] = "सभी सर्वेक्षण", ["bn"] = "সব জরিপ", ["gu"] = "બધા સર્વે", ["mr"] = "सर्व सर्वेक्षण", ["ta"] = "அனைத்து கருத்துக் கணக்கெடுப்புகள்", ["te"] = "అన్ని సర్వేలు", ["kn"] = "ಎಲ್ಲಾ ಸರ್ವೆಗಳು", ["ml"] = "എല്ലാ സർവേകളും" },
                ["Start Survey"] = new() { ["hi"] = "सर्वेक्षण शुरू करें", ["bn"] = "জরিপ শুরু করুন", ["gu"] = "સર્વે શરૂ કરો", ["mr"] = "सर्वेक्षण सुरू करा", ["ta"] = "கருத்துக் கணக்கெடுப்பைத் தொடங்கு", ["te"] = "సర్వేను ప్రారంభించండి", ["kn"] = "ಸರ್ವೆಯನ್ನು ಪ್ರಾರಂಭಿಸಿ", ["ml"] = "സർവേ ആരംഭിക്കുക" }
            };

            return translations.TryGetValue(text, out var langDict) && langDict.TryGetValue(language, out var translated) ? translated : text;
        }

        private static string GetLocalizedPlaceholder(string language)
        {
            var placeholders = new Dictionary<string, string>
            {
                ["en"] = "Search surveys...",
                ["hi"] = "सर्वेक्षण खोजें...",
                ["bn"] = "জরিপ খুঁজুন...",
                ["gu"] = "સર્વે શોધો...",
                ["mr"] = "सर्वेक्षण शोधा...",
                ["ta"] = "கருத்துக் கணக்கெடுப்புகளைத் தேடு...",
                ["te"] = "సర్వేలను శోధించండి...",
                ["kn"] = "ಸರ್ವೆಗಳನ್ನು ಹುಡುಕಿ...",
                ["ml"] = "സർവേകൾ തിരയുക..."
            };

            return placeholders.TryGetValue(language, out var placeholder) ? placeholder : placeholders["en"];
        }

        private static string GetLocalizedSurveyTitle(Survey survey, string language)
        {
            if (language == "en") return survey.Title;
            
            return language switch
            {
                "hi" => survey.Title_Hi ?? survey.Title,
                "bn" => survey.Title_Bn ?? survey.Title,
                "gu" => survey.Title_Gu ?? survey.Title,
                "mr" => survey.Title_Mr ?? survey.Title,
                "ta" => survey.Title_Ta ?? survey.Title,
                "te" => survey.Title_Te ?? survey.Title,
                "kn" => survey.Title_Kn ?? survey.Title,
                "ml" => survey.Title_Ml ?? survey.Title,
                _ => survey.Title
            };
        }

        private static string GetLocalizedSurveyDescription(Survey survey, string language)
        {
            if (language == "en") return survey.Description;
            
            return language switch
            {
                "hi" => survey.Description_Hi ?? survey.Description,
                "bn" => survey.Description_Bn ?? survey.Description,
                "gu" => survey.Description_Gu ?? survey.Description,
                "mr" => survey.Description_Mr ?? survey.Description,
                "ta" => survey.Description_Ta ?? survey.Description,
                "te" => survey.Description_Te ?? survey.Description,
                "kn" => survey.Description_Kn ?? survey.Description,
                "ml" => survey.Description_Ml ?? survey.Description,
                _ => survey.Description
            };
        }
    }
}
