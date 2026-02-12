using System.Text;
using System.Linq;
using SurveyWebApp.Models;

namespace SurveyWebApp
{
    public static class MobileHtmlGenerator
    {
        public static string GenerateMobileHtml(string language, string username, IEnumerable<Survey> surveys)
        {
            var sb = new StringBuilder();

            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>Surveys</title>");
            sb.Append("<link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile.css?v={DateTime.Now.Ticks}' rel='stylesheet'></head><body>");

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

            sb.Append("<main class='home-container'>");
            
            // ================= HOME SCREEN SECTION =================
            sb.Append("<div class='home-screen' id='homeScreen'>");
            
            // Welcome Hero Section
            sb.Append("<div class='welcome-hero'>");
            sb.Append("<div class='welcome-content'>");
            sb.Append($"<h1 class='welcome-title'>{GetLocalizedText("Welcome back", language)}, {System.Net.WebUtility.HtmlEncode(username ?? "User")}</h1>");
            sb.Append($"<p class='welcome-subtitle'>{GetLocalizedText("Complete surveys to share your valuable feedback and insights", language)}</p>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            // Available Surveys Quick Stats
            sb.Append("<div class='stats-container'>");
            sb.Append("<a href='/surveys' class='stat-card'>");
            sb.Append("<div class='stat-icon'>📊</div>");
            sb.Append("<div class='stat-content'>");
            sb.Append($"<h3>{surveys.Count()}</h3>");
            sb.Append($"<p>{GetLocalizedText("Total Surveys", language)}</p>");
            sb.Append("</div>");
            sb.Append("</a>");
            sb.Append("<a href='/average-time' class='stat-card'>");
            sb.Append("<div class='stat-icon'>⏱️</div>");
            sb.Append("<div class='stat-content'>");
            sb.Append("<h3>5-10</h3>");
            sb.Append($"<p>{GetLocalizedText("Minutes Average", language)}</p>");
            sb.Append("</div>");
            sb.Append("</a>");
            sb.Append("<a href='/security-platform' class='stat-card'>");
            sb.Append("<div class='stat-icon'>🎯</div>");
            sb.Append("<div class='stat-content'>");
            sb.Append("<h3>100%</h3>");
            sb.Append($"<p>{GetLocalizedText("Secure Platform", language)}</p>");
            sb.Append("</div>");
            sb.Append("</a>");
            sb.Append("</div>");
            
            // Trust Messages
            sb.Append("<div class='trust-messages'>");
            sb.Append("<a href='/secure-private' class='trust-item'>");
            sb.Append("<div class='trust-icon'>🔒</div>");
            sb.Append("<div class='trust-content'>");
            sb.Append($"<h3>{GetLocalizedText("Secure & Private", language)}</h3>");
            sb.Append($"<p>{GetLocalizedText("Your responses are encrypted and confidential", language)}</p>");
            sb.Append("</div>");
            sb.Append("</a>");
            sb.Append("<a href='/quick-easy' class='trust-item'>");
            sb.Append("<div class='trust-icon'>⚡</div>");
            sb.Append("<div class='trust-content'>");
            sb.Append($"<h3>{GetLocalizedText("Quick & Easy", language)}</h3>");
            sb.Append($"<p>{GetLocalizedText("Most surveys take less than 5 minutes", language)}</p>");
            sb.Append("</div>");
            sb.Append("</a>");
            sb.Append("<a href='/rewards-program' class='trust-item'>");
            sb.Append("<div class='trust-icon'>🏆</div>");
            sb.Append("<div class='trust-content'>");
            sb.Append($"<h3>{GetLocalizedText("Rewards Program", language)}</h3>");
            sb.Append($"<p>{GetLocalizedText("Earn points for completing surveys", language)}</p>");
            sb.Append("</div>");
            sb.Append("</a>");
            sb.Append("</div>");
            
            // Survey Preview List
            sb.Append("<div class='survey-preview-list'>");
            sb.Append($"<h2 class='section-title'>{GetLocalizedText("Available Surveys", language)}</h2>");
            sb.Append("<div class='preview-list'>");
            
            int previewCount = 0;
            foreach (var s in surveys)
            {
                if (previewCount >= 3) break; // Show only first 3 surveys as preview
                
                var localizedTitle = GetLocalizedSurveyTitle(s, language);
                var localizedDescription = GetLocalizedSurveyDescription(s, language);
                
                sb.Append("<div class='preview-item' onclick='startSurvey(" + s.Id + ")'>");
                sb.Append("<div class='preview-icon'>📋</div>");
                sb.Append("<div class='preview-content'>");
                sb.Append($"<h3 class='preview-title'>{System.Net.WebUtility.HtmlEncode(localizedTitle)}</h3>");
                sb.Append($"<p class='preview-desc'>{System.Net.WebUtility.HtmlEncode(localizedDescription ?? "")}</p>");
                sb.Append("</div>");
                sb.Append("<div class='preview-arrow'>→</div>");
                sb.Append("</div>");
                
                previewCount++;
            }
            
            sb.Append("</div>");
            sb.Append("</div>");
            
            // View All Surveys Button
            sb.Append("<div class='cta-section'>");
            sb.Append($"<a href='/surveys' class='view-all-btn'>");
            sb.Append($"<span class='btn-icon'>📊</span>");
            sb.Append($"<span class='btn-text'>{GetLocalizedText("View All Surveys", language)}</span>");
            sb.Append("</a>");
            sb.Append("</div>");
            
            sb.Append("</div>");

            sb.Append("</main>");

            sb.Append("<script>");
            sb.Append("function toggleSearch() { toggle('searchBar'); }");
            sb.Append("function toggleSettings() { toggle('settingsMenu'); }");
            sb.Append("function toggleAccount() { toggle('accountMenu'); }");
            sb.Append("function toggle(id) { document.querySelectorAll('.dropdown').forEach(d => d.style.display = 'none'); const el = document.getElementById(id); if (el) el.style.display = 'block'; }");
            sb.Append("function toggleLanguageDropdown() { const el = document.getElementById('languageDropdown'); el.style.display = el.style.display === 'block' ? 'none' : 'block'; document.querySelector('.dropdown-arrow').textContent = el.style.display === 'block' ? '▲' : '▼'; }");
            sb.Append("function startSurvey(surveyId) { window.location.href = '/survey/' + surveyId; }");
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
                ["Welcome back"] = new() { ["hi"] = "वापसी पर स्वागत है", ["bn"] = "ফিরে আপনাকে স্বাগতম", ["gu"] = "પાછા આવવામાં આપનું સ્વાગત છે", ["mr"] = "परत येण्यास स्वागत आहे", ["ta"] = "மீண்டும் வரவேற்கிறோம்", ["te"] = "తిరిగి రాకపోవడంలో స్వాగతం", ["kn"] = "ಮತ್ತೆ ಸ್ವಾಗತ", ["ml"] = "തിരിച്ചുവരവാന്‍ സ്വാഗതം" },
                ["Complete surveys to share your valuable feedback and insights"] = new() { ["hi"] = "अपनी कीमती प्रतिक्रिया और अंतर्दृष्टि साझा करने के लिए सर्वेक्षण पूर्ण करें", ["bn"] = "আপনার মূল্যবান প্রতিক্রিযা এবং অন্তর্দৃষ্টি শেয়ার করতে জরিপগুলি সম্পন্ন করুন", ["gu"] = "તમારી કિંમતી પ્રતિસાદ અને દ્રષ્ટિકોણ શેર કરવા માટે સર્વે પૂર્ણ કરો", ["mr"] = "आपले मौल्यवान अभिप्राय आणि अंतर्दृष्टि सामायिक करण्यासाठी सर्वेक्षण पूर्ण करा", ["ta"] = "உங்களின் மதிப்புள்ள கருத்துக்களையும் நுண்ணிய காட்சிகளையும் பகிரவதற்கு கருத்துக் கணக்கெடுப்புகளை முடிக்கவும்", ["te"] = "మీ విలువైన అభిప్రాయాలు మరియు ఒళనాలను పంచేయడానికి సర్వేలను పూర్తించండి", ["kn"] = "ನಿಮ್ಮ ಬೆಲೆಯುಳ್ಳ ಪ್ರತಿಕ್ರಿಯೆಗಳನ್ನು ಮತ್ತು ಒಳನೋಟಗಳನ್ನು ಹಂಚಿಕೊಳ್ಳಲು ಸರ್ವೆಗಳನ್ನು ಪೂರ್ಣಗೊಳಿಸಿ", ["ml"] = "നിങ്ങളുടെ വിലപ്പെട്ട അഭിപ്രായങ്ങളും ഉൾക്കാഴ്ചകളും പങ്കിടുന്നതിന് സർവേകൾ പൂർത്തിയാക്കുക" },
                ["Secure & Private"] = new() { ["hi"] = "सुरक्षित और निजी", ["bn"] = "নিরাপদ এবং ব্যক্তিগত", ["gu"] = "સુરક્ષિત અને ખાનગી", ["mr"] = "सुरक्षित आणि खाजगी", ["ta"] = "பாதுகாப்பானதும் தனிப்பட்டமானதும்", ["te"] = "సురక్షితం మరియు ఆంతరంగికం", ["kn"] = "ಸುರಕ್ಷಿತ ಮತ್ತು ಖಾಸಗಿ", ["ml"] = "സുരക്ഷിതവും സ്വകാര്യവും" },
                ["Your responses are encrypted and confidential"] = new() { ["hi"] = "आपकी प्रतिक्रियाएं एन्क्रिप्टेड और गोपनीय हैं", ["bn"] = "আপনার প্রতিক্রিযা এনক্রিপ্টেড এবং গোপনীয", ["gu"] = "તમારા જવાબો એન્ક્રિપ્ટેડ અને ગુપ્તિય છે", ["mr"] = "आपले प्रतिसाद एन्क्रिप्टेड आणि गोपनीय आहेत", ["ta"] = "உங்கள் பதில்கள் குறியாக்கப்பட்டு ரகசியமாக உள்ளன", ["te"] = "మీ స్పందనలు ఎన్క్రిప్ట్ చేయబడి రహస్యంగా ఉన్నాయి", ["kn"] = "ನಿಮ್ಮ ಪ್ರತಿಕ್ರಿಯೆಗಳು ಎನ್ಕ್ರಿಪ್ಟ್ ಮಾಡಲಾಗಿದ್ದೆ ಮತ್ತು ಗೌಪ್ಯವಾಗಿವೆ", ["ml"] = "നിങ്ങളുടെ പ്രതികരണങ്ങൾ എൻക്രിപ്റ്റ് ചെയ്ത് രഹസ്യമായിരിക്കുന്നു" },
                ["Quick & Easy"] = new() { ["hi"] = "त्वरित और आसान", ["bn"] = "দ্রুত এবং সহজ", ["gu"] = "ઝડપી અને સરળ", ["mr"] = "जलद आणि सोपे", ["ta"] = "வேகமானதும் எளிதமானதும்", ["te"] = "వేగవంతమైన మరియు సులభమైన", ["kn"] = "ತ್ವರಿತ ಮತ್ತು ಸುಲಭ", ["ml"] = "വേഗവും എളുപ്പവും" },
                ["Rewards Program"] = new() { ["hi"] = "पुरस्कार कार्यक्रम", ["bn"] = "পুরস্কার প্রোগ্রাম", ["gu"] = "પુરસ્કાર કાર્યક્રમ", ["mr"] = "बक्षीस कार्यक्रम", ["ta"] = "விருது திட்டம்", ["te"] = "బహుమతుల కార్యక్రమం", ["kn"] = "ಪ್ರಶಸ್ತಿ ಕಾರ್ಯಕ್ರಮ", ["ml"] = "റിവാർഡ് പ്രോഗ്രാം" },
                ["Earn points for completing surveys"] = new() { ["hi"] = "सर्वेक्षण पूर्ण करने पर अंक अर्जित करें", ["bn"] = "জরিপ সম্পন্ন করার জন্য পয়েন্ট অর্জন করুন", ["gu"] = "સર્વે પૂર્ણ કરવા માટે પોઇન્ટ્સ કમાવો", ["mr"] = "सर्वेक्षण पूर्ण केल्यावर गुण मिळवा", ["ta"] = "கருத்துக் கணக்கெடுப்புகளை முடிக்க புள்ளிகள் பெறுங்கள்", ["te"] = "సర్వేలను పూర్తి చేయడానికి పాయింట్లు సంపాదించండి", ["kn"] = "ಸರ್ವೆಗಳನ್ನು ಪೂರ್ಣಗೊಳಿಸಲು ಅಂಕಗಳನ್ನು ಗಳಿಸಿ", ["ml"] = "സർവേകൾ പൂർത്തിയാക്കുന്നതിന് പോയിന്റുകൾ നേടുക" },
                ["Available Surveys"] = new() { ["hi"] = "उपलब्ध सर्वेक्षण", ["bn"] = "উপলব্ধ জরিপ", ["gu"] = "ઉપલબ્ધ સર્વે", ["mr"] = "उपलब्ध सर्वेक्षण", ["ta"] = "கிடைக்கும் கருத்துக் கணக்கெடுப்புகள்", ["te"] = "అందుబాటులో ఉన్న సర్వేలు", ["kn"] = "ಲಭ್ಯವಿರುವ ಸರ್ವೆಗಳು", ["ml"] = "ലഭ്യമായ സർവേകൾ" },
                ["Minutes Average"] = new() { ["hi"] = "औसत मिनट", ["bn"] = "গড় মিনিট", ["gu"] = "સરેરાન મિનિટ", ["mr"] = "सरासरी मिनिटे", ["ta"] = "சராசரி நிமிடங்கள்", ["te"] = "సగటు నిమిషాలు", ["kn"] = "ಸರಾಸರಿ ನಿಮಿಷಗಳು", ["ml"] = "ശരാശരി മിനിറ്റുകൾ" },
                ["Secure Platform"] = new() { ["hi"] = "सुरक्षित प्लेटफॉर्म", ["bn"] = "নিরাপদ প্ল্যাটফর্ম", ["gu"] = "સુરક્ષિત પ્લેટફોર્મ", ["mr"] = "सुरक्षित प्लॅटफॉर्म", ["ta"] = "பாதுகாப்பான தளம்", ["te"] = "సురక్షిత ప్లాట్‌ఫారమ్", ["kn"] = "ಸುರಕ್ಷಿತ ವೇದಿಕೆ", ["ml"] = "സുരക്ഷിത പ്ലാറ്റ്ഫോം" },
                ["Most surveys take less than 5 minutes"] = new() { ["hi"] = "ज्यादातर सर्वेक्षण 5 मिनट से कम समय में होते हैं", ["bn"] = "বেশিরভাগ জরিপ 5 মিনিটের কম সমযে হয়", ["gu"] = "મોટાભાગના સર્વે 5 મિનિટથી ઓછા સમયમાં થાય છે", ["mr"] = "बहुतेक सर्वेक्षण ५ मिनिटांपेक्षा कमी वेळ घेतात", ["ta"] = "பெரும்பாலான கருத்துக் கணக்கெடுப்புகள் 5 நிமிடங்களுக்குள் முடியும்", ["te"] = "చాలా సర్వేలు 5 నిమిషాల కంటే తక్కువ సమయంలో పూర్తి అవుతాయి", ["kn"] = "ಬಹುತೇಕ ಸರ್ವೆಗಳು 5 ನಿಮಿಷಗಳಿಗಿಂತ ಕಡಿಮೆ ಸಮಯದಲ್ಲಿ ಪೂರ್ಣಗೊಳ್ಳುತ್ತವೆ", ["ml"] = "ഭൂരിഭാഗം സർവേകൾ 5 മിനിറ്റിൽ കുറവ് സമയം എടുക്കും" },
                ["Choose a survey below to get started"] = new() { ["hi"] = "शुरू करने के लिए नीचे एक सर्वेक्षण चुनें", ["bn"] = "শুরু করতে নীচে একটি জরিপ নির্বাচন করুন", ["gu"] = "શરૂ કરવા માટે નીચે એક સર્વે પસંદ કરો", ["mr"] = "सुरू करण्यासाठी खालील सर्वेक्षण निवडा", ["ta"] = "தொடங்க கீழே உள்ள ஒரு கருத்துக் கணக்கெடுப்பைத் தேர்ந்தெடுக்கவும்", ["te"] = "ప్రారంభించడానికి కింద ఒక సర్వేను ఎంచుకోండి", ["kn"] = "ಪ್ರಾರಂಭಿಸಲು ಕೆಳಗೆ ಒಂದು ಸರ್ವೆಯನ್ನು ಆರಿಸಿ", ["ml"] = "ആരംഭിക്കാൻ താഴെ ഒരു സർവേ തിരഞ്ഞെടുക്കുക" },
                ["All Surveys"] = new() { ["hi"] = "सभी सर्वेक्षण", ["bn"] = "সব জরিপ", ["gu"] = "બધા સર્વે", ["mr"] = "सर्व सर्वेक्षण", ["ta"] = "அனைத்து கருத்துக் கணக்கெடுப்புகள்", ["te"] = "అన్ని సర్వేలు", ["kn"] = "ಎಲ್ಲಾ ಸರ್ವೆಗಳು", ["ml"] = "എല്ലാ സർവേകളും" },
                ["Start Survey"] = new() { ["hi"] = "सर्वेक्षण शुरू करें", ["bn"] = "জরিপ শুরু করুন", ["gu"] = "સર્વે શરૂ કરો", ["mr"] = "सर्वेक्षण सुरू करा", ["ta"] = "கருத்துக் கணக்கெடுப்பைத் தொடங்கு", ["te"] = "సర్వేను ప్రారంభించండి", ["kn"] = "ಸರ್ವೆಯನ್ನು ಪ್ರಾರಂಭಿಸಿ", ["ml"] = "സർവേ ആരംഭിക്കുക" },
                ["View All Surveys"] = new() { ["hi"] = "सभी सर्वेक्षण देखें", ["bn"] = "সব জরিপ দেখুন", ["gu"] = "બધા સર્વે જુઓ", ["mr"] = "सर्व सर्वेक्षण पहा", ["ta"] = "அனைத்து கருத்துக் கணக்கெடுப்புகளையும் காண்க", ["te"] = "అన్ని సర్వేలను చూడండి", ["kn"] = "ಎಲ್ಲಾ ಸರ್ವೆಗಳನ್ನು ವೀಕ್ಷಿಸಿ", ["ml"] = "എല്ലാ സർവേകളും കാണുക" }
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
                ["kn"] = "ಸರ್ವೇಗಳನ್ನು ಹುಡುಕಿ...",
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

        public static string GenerateSecurePrivateHtml(string language, string username)
        {
            var sb = new StringBuilder();
            
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>Secure & Private</title>");
            sb.Append("<link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'>");
            sb.Append("<link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile.css?v={DateTime.Now.Ticks}' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile-features.css?v={DateTime.Now.Ticks}' rel='stylesheet'></head><body>");

            // Header
            sb.Append("<header class='app-header'>");
            sb.Append("<div class='brand-logo'><img src='/assets/apptad-technologies-pvt-ltd-original-removebg-preview.png' class='logo' /></div>");
            sb.Append("<div class='brand-title'>SECURE & PRIVATE</div>");
            sb.Append("<div class='header-actions'>");
            sb.Append("<button class='icon-btn' onclick='toggleSettings()'>⚙️</button>");
            sb.Append("<button class='icon-btn' onclick='toggleAccount()'>👤</button>");
            sb.Append("</div></header>");

            // Hero Section
            sb.Append("<main class='feature-page'>");
            sb.Append("<div class='hero-section'>");
            sb.Append("<div class='hero-icon'>🔐</div>");
            sb.Append($"<h1 class='hero-title'>{GetLocalizedText("Your Privacy Fortress", language)}</h1>");
            sb.Append($"<p class='hero-subtitle'>{GetLocalizedText("Military-grade protection for your data", language)}</p>");
            sb.Append("</div>");

            // Security Metrics
            sb.Append("<div class='metrics-grid'>");
            sb.Append("<div class='metric-card'>");
            sb.Append("<div class='metric-icon'>�️</div>");
            sb.Append("<div class='metric-value'>256-bit</div>");
            sb.Append("<div class='metric-label'>Encryption</div>");
            sb.Append("</div>");
            sb.Append("<div class='metric-card'>");
            sb.Append("<div class='metric-icon'>🔒</div>");
            sb.Append("<div class='metric-value'>100%</div>");
            sb.Append("<div class='metric-label'>Private</div>");
            sb.Append("</div>");
            sb.Append("<div class='metric-card'>");
            sb.Append("<div class='metric-icon'>🌍</div>");
            sb.Append("<div class='metric-value'>GDPR</div>");
            sb.Append("<div class='metric-label'>Compliant</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            // Feature Cards
            sb.Append("<div class='feature-grid'>");
            sb.Append("<div class='feature-card premium'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🔐</div>");
            sb.Append("<div class='feature-badge'>PRO</div>");
            sb.Append("</div>");
            sb.Append("<h3>End-to-End Encryption</h3>");
            sb.Append("<p>Your data is locked with AES-256 military-grade encryption from device to server.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-lock'></i> Encrypted</span>");
            sb.Append("<span class='stat'><i class='fas fa-check-circle'></i> Verified</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🛡️</div>");
            sb.Append("</div>");
            sb.Append("<h3>Data Protection</h3>");
            sb.Append("<p>GDPR, CCPA & HIPAA compliant. Your data never leaves our secure ecosystem.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-shield-alt'></i> Protected</span>");
            sb.Append("<span class='stat'><i class='fas fa-gavel'></i> Legal</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>👁️‍🗨️</div>");
            sb.Append("</div>");
            sb.Append("<h3>Anonymous Mode</h3>");
            sb.Append("<p>Complete anonymity. No tracking, no profiling, just your honest opinions.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-user-secret'></i> Anonymous</span>");
            sb.Append("<span class='stat'><i class='fas fa-eye-slash'></i> Private</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card premium'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🔐</div>");
            sb.Append("<div class='feature-badge'>NEW</div>");
            sb.Append("</div>");
            sb.Append("<h3>Secure Servers</h3>");
            sb.Append("<p>ISO 27001 certified data centers with 24/7 security monitoring and biometric access.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-server'></i> Secure</span>");
            sb.Append("<span class='stat'><i class='fas fa-certificate'></i> Certified</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            // Trust Badge
            sb.Append("<div class='trust-badge'>");
            sb.Append("<div class='badge-content'>");
            sb.Append("<div class='badge-icon'>🏆</div>");
            sb.Append("<div class='badge-text'>");
            sb.Append("<h3>Trusted by 1M+ Users</h3>");
            sb.Append("<p>Zero data breaches since 2019</p>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='back-button-container'>");
            sb.Append("<a href='/' class='btn-secondary'><i class='fas fa-arrow-left'></i> Back to Home</a>");
            sb.Append("</div>");
            
            sb.Append("</main>");
            sb.Append("</body></html>");
            
            return sb.ToString();
        }

        public static string GenerateQuickEasyHtml(string language, string username)
        {
            var sb = new StringBuilder();
            
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>Quick & Easy</title>");
            sb.Append("<link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'>");
            sb.Append("<link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile.css?v={DateTime.Now.Ticks}' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile-features.css?v={DateTime.Now.Ticks}' rel='stylesheet'></head><body>");

            // Header
            sb.Append("<header class='app-header'>");
            sb.Append("<div class='brand-logo'><img src='/assets/apptad-technologies-pvt-ltd-original-removebg-preview.png' class='logo' /></div>");
            sb.Append("<div class='brand-title'>QUICK & EASY</div>");
            sb.Append("<div class='header-actions'>");
            sb.Append("<button class='icon-btn' onclick='toggleSettings()'>⚙️</button>");
            sb.Append("<button class='icon-btn' onclick='toggleAccount()'>👤</button>");
            sb.Append("</div></header>");

            // Hero Section
            sb.Append("<main class='feature-page'>");
            sb.Append("<div class='hero-section'>");
            sb.Append("<div class='hero-icon'>⚡</div>");
            sb.Append($"<h1 class='hero-title'>{GetLocalizedText("Lightning Fast Surveys", language)}</h1>");
            sb.Append($"<p class='hero-subtitle'>{GetLocalizedText("Complete surveys in seconds, not minutes", language)}</p>");
            sb.Append("</div>");

            // Speed Metrics
            sb.Append("<div class='speed-metrics'>");
            sb.Append("<div class='speed-card'>");
            sb.Append("<div class='speed-icon'>🚀</div>");
            sb.Append("<div class='speed-value'>2.5x</div>");
            sb.Append("<div class='speed-label'>Faster</div>");
            sb.Append("</div>");
            sb.Append("<div class='speed-card'>");
            sb.Append("<div class='speed-icon'>⏱️</div>");
            sb.Append("<div class='speed-value'>3 min</div>");
            sb.Append("<div class='speed-label'>Average</div>");
            sb.Append("</div>");
            sb.Append("<div class='speed-card'>");
            sb.Append("<div class='speed-icon'>📱</div>");
            sb.Append("<div class='speed-value'>100%</div>");
            sb.Append("<div class='speed-label'>Mobile</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            // Feature Cards
            sb.Append("<div class='feature-grid'>");
            sb.Append("<div class='feature-card premium'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>⚡</div>");
            sb.Append("<div class='feature-badge'>FAST</div>");
            sb.Append("</div>");
            sb.Append("<h3>Lightning Speed</h3>");
            sb.Append("<p>Optimized interface loads instantly. Smart caching makes every interaction feel instantaneous.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-bolt'></i> Instant</span>");
            sb.Append("<span class='stat'><i class='fas fa-tachometer-alt'></i> Optimized</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>📱</div>");
            sb.Append("</div>");
            sb.Append("<h3>Mobile First</h3>");
            sb.Append("<p>Designed for thumbs. Large touch targets, smooth gestures, and perfect readability on any screen.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-mobile-alt'></i> Responsive</span>");
            sb.Append("<span class='stat'><i class='fas fa-hand-pointer'></i> Touch</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🧠</div>");
            sb.Append("</div>");
            sb.Append("<h3>Smart Questions</h3>");
            sb.Append("<p>AI-powered question flow adapts to your answers. Skip irrelevant questions and save time.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-brain'></i> Smart</span>");
            sb.Append("<span class='stat'><i class='fas fa-route'></i> Adaptive</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card premium'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🎯</div>");
            sb.Append("<div class='feature-badge'>PRO</div>");
            sb.Append("</div>");
            sb.Append("<h3>Progress Tracking</h3>");
            sb.Append("<p>Visual progress bars, time estimates, and completion rewards make every survey engaging.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-chart-line'></i> Visual</span>");
            sb.Append("<span class='stat'><i class='fas fa-trophy'></i> Rewards</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            // Achievement Badge
            sb.Append("<div class='achievement-badge'>");
            sb.Append("<div class='badge-content'>");
            sb.Append("<div class='badge-icon'>🏅</div>");
            sb.Append("<div class='badge-text'>");
            sb.Append("<h3>Speed Champion</h3>");
            sb.Append("<p>Complete 10 surveys under 3 minutes each</p>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='back-button-container'>");
            sb.Append("<a href='/' class='btn-secondary'><i class='fas fa-arrow-left'></i> Back to Home</a>");
            sb.Append("</div>");
            
            sb.Append("</main>");
            sb.Append("</body></html>");
            
            return sb.ToString();
        }

        public static string GenerateRewardsProgramHtml(string language, string username)
        {
            var sb = new StringBuilder();
            
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>Rewards Program</title>");
            sb.Append("<link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'>");
            sb.Append("<link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile.css?v={DateTime.Now.Ticks}' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile-features.css?v={DateTime.Now.Ticks}' rel='stylesheet'></head><body>");

            // Header
            sb.Append("<header class='app-header'>");
            sb.Append("<div class='brand-logo'><img src='/assets/apptad-technologies-pvt-ltd-original-removebg-preview.png' class='logo' /></div>");
            sb.Append("<div class='brand-title'>REWARDS PROGRAM</div>");
            sb.Append("<div class='header-actions'>");
            sb.Append("<button class='icon-btn' onclick='toggleSettings()'>⚙️</button>");
            sb.Append("<button class='icon-btn' onclick='toggleAccount()'>👤</button>");
            sb.Append("</div></header>");

            // Hero Section with Enhanced Visual Design
            sb.Append("<main class='feature-page'>");
            sb.Append("<div class='hero-section rewards-hero enhanced'>");
            sb.Append("<div class='hero-background'>");
            sb.Append("<div class='hero-particles'>");
            sb.Append("<div class='particle'></div>");
            sb.Append("<div class='particle'></div>");
            sb.Append("<div class='particle'></div>");
            sb.Append("<div class='particle'></div>");
            sb.Append("<div class='particle'></div>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("<div class='hero-content'>");
            sb.Append("<div class='hero-icon animated'>🏆</div>");
            sb.Append($"<h1 class='hero-title'>{GetLocalizedText("Earn & Win Rewards", language)}</h1>");
            sb.Append($"<p class='hero-subtitle'>{GetLocalizedText("Turn your opinions into amazing prizes", language)}</p>");
            sb.Append("</div>");
            sb.Append("<div class='floating-rewards'>");
            sb.Append("<div class='floating-coin'>💎</div>");
            sb.Append("<div class='floating-coin'>🏅</div>");
            sb.Append("<div class='floating-coin'>🎁</div>");
            sb.Append("<div class='floating-coin'>💰</div>");
            sb.Append("<div class='floating-coin'>⭐</div>");
            sb.Append("<div class='floating-coin'>🎯</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            // Enhanced Points Overview with Live Stats
            sb.Append("<div class='points-overview enhanced'>");
            sb.Append("<div class='points-card main premium'>");
            sb.Append("<div class='points-header'>");
            sb.Append("<div class='points-icon'>💎</div>");
            sb.Append("<div class='points-info'>");
            sb.Append("<div class='points-value'>1,250</div>");
            sb.Append("<div class='points-label'>Total Points</div>");
            sb.Append("</div>");
            sb.Append("<div class='points-rank'>🥈 Silver</div>");
            sb.Append("</div>");
            sb.Append("<div class='points-progress'>");
            sb.Append("<div class='progress-info'>");
            sb.Append("<span>Level Progress</span>");
            sb.Append("<span class='progress-text'>62.5%</span>");
            sb.Append("</div>");
            sb.Append("<div class='progress-bar enhanced' style='width: 62.5%'></div>");
            sb.Append("<div class='points-next'>250 pts to Gold Level 🥇</div>");
            sb.Append("</div>");
            sb.Append("<div class='points-actions'>");
            sb.Append("<button class='btn-reward primary'>Redeem Points</button>");
            sb.Append("<button class='btn-reward secondary'>View History</button>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("<div class='points-card'>");
            sb.Append("<div class='points-icon'>🎁</div>");
            sb.Append("<div class='points-value'>3</div>");
            sb.Append("<div class='points-label'>Redemptions</div>");
            sb.Append("<div class='points-change positive'>+1 this week</div>");
            sb.Append("</div>");
            sb.Append("<div class='points-card'>");
            sb.Append("<div class='points-icon'>🔥</div>");
            sb.Append("<div class='points-value'>15</div>");
            sb.Append("<div class='points-label'>Day Streak</div>");
            sb.Append("<div class='streak-fire'>🔥🔥🔥</div>");
            sb.Append("</div>");
            sb.Append("<div class='points-card bonus'>");
            sb.Append("<div class='points-icon'>⚡</div>");
            sb.Append("<div class='points-value'>2x</div>");
            sb.Append("<div class='points-label'>Bonus Active</div>");
            sb.Append("<div class='bonus-timer'>2:45:30 left</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            // Interactive Rewards Store
            sb.Append("<div class='rewards-store'>");
            sb.Append("<div class='store-header'>");
            sb.Append("<h3 class='store-title'>🛍️ Rewards Store</h3>");
            sb.Append("<div class='store-filters'>");
            sb.Append("<button class='filter-btn active'>All</button>");
            sb.Append("<button class='filter-btn'>Gift Cards</button>");
            sb.Append("<button class='filter-btn'>Cash</button>");
            sb.Append("<button class='filter-btn'>Charity</button>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("<div class='store-grid'>");
            sb.Append("<div class='reward-item popular'>");
            sb.Append("<div class='reward-badge'>POPULAR</div>");
            sb.Append("<div class='reward-image'>🎫</div>");
            sb.Append("<div class='reward-info'>");
            sb.Append("<h4>Amazon Gift Card</h4>");
            sb.Append("<p>10 Value</p>");
            sb.Append("<div class='reward-price'>1000 pts</div>");
            sb.Append("</div>");
            sb.Append("<button class='reward-btn'>Redeem</button>");
            sb.Append("</div>");
            sb.Append("<div class='reward-item new'>");
            sb.Append("<div class='reward-badge'>NEW</div>");
            sb.Append("<div class='reward-image'>💵</div>");
            sb.Append("<div class='reward-info'>");
            sb.Append("<h4>PayPal Cash</h4>");
            sb.Append("<p>5 Value</p>");
            sb.Append("<div class='reward-price'>500 pts</div>");
            sb.Append("</div>");
            sb.Append("<button class='reward-btn'>Redeem</button>");
            sb.Append("</div>");
            sb.Append("<div class='reward-item'>");
            sb.Append("<div class='reward-image'>❤️</div>");
            sb.Append("<div class='reward-info'>");
            sb.Append("<h4>Charity Donation</h4>");
            sb.Append("<p>Support Education</p>");
            sb.Append("<div class='reward-price'>250 pts</div>");
            sb.Append("</div>");
            sb.Append("<button class='reward-btn'>Donate</button>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            // Achievements Section
            sb.Append("<div class='achievements-section'>");
            sb.Append("<h3 class='section-title'>🏆 Your Achievements</h3>");
            sb.Append("<div class='achievements-grid'>");
            sb.Append("<div class='achievement-card unlocked'>");
            sb.Append("<div class='achievement-icon'>�</div>");
            sb.Append("<div class='achievement-name'>First Survey</div>");
            sb.Append("<div class='achievement-desc'>Completed your first survey</div>");
            sb.Append("<div class='achievement-points'>+50 pts</div>");
            sb.Append("</div>");
            sb.Append("<div class='achievement-card unlocked'>");
            sb.Append("<div class='achievement-icon'>🔥</div>");
            sb.Append("<div class='achievement-name'>Week Warrior</div>");
            sb.Append("<div class='achievement-desc'>7-day streak completed</div>");
            sb.Append("<div class='achievement-points'>+100 pts</div>");
            sb.Append("</div>");
            sb.Append("<div class='achievement-card locked'>");
            sb.Append("<div class='achievement-icon'>👑</div>");
            sb.Append("<div class='achievement-name'>Survey Master</div>");
            sb.Append("<div class='achievement-desc'>Complete 100 surveys</div>");
            sb.Append("<div class='achievement-progress'>45/100</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            // Leaderboard Preview
            sb.Append("<div class='leaderboard-preview'>");
            sb.Append("<div class='leaderboard-header'>");
            sb.Append("<h3 class='leaderboard-title'>🏅 Global Leaderboard</h3>");
            sb.Append("<button class='view-all-btn'>View All</button>");
            sb.Append("</div>");
            sb.Append("<div class='leaderboard-list'>");
            sb.Append("<div class='leaderboard-item you'>");
            sb.Append("<div class='rank'>#42</div>");
            sb.Append("<div class='player-info'>");
            sb.Append("<div class='player-avatar'>👤</div>");
            sb.Append("<div class='player-name'>You</div>");
            sb.Append("</div>");
            sb.Append("<div class='player-points'>1,250 pts</div>");
            sb.Append("<div class='player-change positive'>↑ 3</div>");
            sb.Append("</div>");
            sb.Append("<div class='leaderboard-item'>");
            sb.Append("<div class='rank gold'>#1</div>");
            sb.Append("<div class='player-info'>");
            sb.Append("<div class='player-avatar'>🏆</div>");
            sb.Append("<div class='player-name'>SurveyKing</div>");
            sb.Append("</div>");
            sb.Append("<div class='player-points'>15,420 pts</div>");
            sb.Append("<div class='player-change'>-</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            // Special Offer Banner
            sb.Append("<div class='special-offer enhanced'>");
            sb.Append("<div class='offer-content'>");
            sb.Append("<div class='offer-icon'>🎉</div>");
            sb.Append("<div class='offer-text'>");
            sb.Append("<h3>Double Points Weekend!</h3>");
            sb.Append("<p>Complete surveys this weekend for 2x points</p>");
            sb.Append("<div class='offer-timer'>Ends in: 1d 14h 32m</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='back-button-container'>");
            sb.Append("<a href='/' class='btn-secondary'><i class='fas fa-arrow-left'></i> Back to Home</a>");
            sb.Append("</div>");
            
            sb.Append("</main>");
            sb.Append("</body></html>");
            
            return sb.ToString();
        }

        public static string GenerateAverageTimeHtml(string language, string username)
        {
            var sb = new StringBuilder();
            
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>Average Time</title>");
            sb.Append("<link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'>");
            sb.Append("<link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile.css?v={DateTime.Now.Ticks}' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile-features.css?v={DateTime.Now.Ticks}' rel='stylesheet'></head><body>");

            // Header
            sb.Append("<header class='app-header'>");
            sb.Append("<div class='brand-logo'><img src='/assets/apptad-technologies-pvt-ltd-original-removebg-preview.png' class='logo' /></div>");
            sb.Append("<div class='brand-title'>AVERAGE TIME</div>");
            sb.Append("<div class='header-actions'>");
            sb.Append("<button class='icon-btn' onclick='toggleSettings()'>⚙️</button>");
            sb.Append("<button class='icon-btn' onclick='toggleAccount()'>👤</button>");
            sb.Append("</div></header>");

            // Hero Section
            sb.Append("<main class='feature-page'>");
            sb.Append("<div class='hero-section'>");
            sb.Append("<div class='hero-icon'>⏱️</div>");
            sb.Append($"<h1 class='hero-title'>{GetLocalizedText("Time Analytics Dashboard", language)}</h1>");
            sb.Append($"<p class='hero-subtitle'>{GetLocalizedText("Master your survey time management", language)}</p>");
            sb.Append("</div>");

            // Time Comparison
            sb.Append("<div class='time-comparison'>");
            sb.Append("<div class='time-card user'>");
            sb.Append("<div class='time-icon'>⚡</div>");
            sb.Append("<div class='time-value'>3.5 min</div>");
            sb.Append("<div class='time-label'>Your Average</div>");
            sb.Append("<div class='time-badge'>FAST</div>");
            sb.Append("</div>");
            sb.Append("<div class='vs-divider'>VS</div>");
            sb.Append("<div class='time-card platform'>");
            sb.Append("<div class='time-icon'>📊</div>");
            sb.Append("<div class='time-value'>5.2 min</div>");
            sb.Append("<div class='time-label'>Platform Avg</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            // Survey Categories
            sb.Append("<div class='survey-categories'>");
            sb.Append("<h3 class='section-title'>Survey Categories</h3>");
            sb.Append("<div class='category-grid'>");
            sb.Append("<div class='category-card quick'>");
            sb.Append("<div class='category-icon'>⚡</div>");
            sb.Append("<div class='category-time'>1-3 min</div>");
            sb.Append("<div class='category-name'>Quick Surveys</div>");
            sb.Append("<div class='category-desc'>Perfect for coffee breaks</div>");
            sb.Append("<div class='category-points'>+10 pts</div>");
            sb.Append("</div>");
            sb.Append("<div class='category-card standard'>");
            sb.Append("<div class='category-icon'>📝</div>");
            sb.Append("<div class='category-time'>3-7 min</div>");
            sb.Append("<div class='category-name'>Standard Surveys</div>");
            sb.Append("<div class='category-desc'>Most popular choice</div>");
            sb.Append("<div class='category-points'>+25 pts</div>");
            sb.Append("</div>");
            sb.Append("<div class='category-card detailed'>");
            sb.Append("<div class='category-icon'>🔬</div>");
            sb.Append("<div class='category-time'>7-15 min</div>");
            sb.Append("<div class='category-name'>Detailed Surveys</div>");
            sb.Append("<div class='category-desc'>In-depth research</div>");
            sb.Append("<div class='category-points'>+50 pts</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            // Feature Cards
            sb.Append("<div class='feature-grid'>");
            sb.Append("<div class='feature-card premium'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🎯</div>");
            sb.Append("<div class='feature-badge'>SMART</div>");
            sb.Append("</div>");
            sb.Append("<h3>Smart Time AI</h3>");
            sb.Append("<p>Our AI learns your schedule and suggests the perfect surveys for your available time.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-robot'></i> AI</span>");
            sb.Append("<span class='stat'><i class='fas fa-brain'></i> Smart</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>�</div>");
            sb.Append("</div>");
            sb.Append("<h3>Time Tracking</h3>");
            sb.Append("<p>Detailed analytics of your survey completion patterns with insights and trends.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-chart-bar'></i> Analytics</span>");
            sb.Append("<span class='stat'><i class='fas fa-trending-up'></i> Trends</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>⏰</div>");
            sb.Append("</div>");
            sb.Append("<h3>Time Bonuses</h3>");
            sb.Append("<p>Complete surveys faster than average for bonus points and speed achievements.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-bolt'></i> Speed</span>");
            sb.Append("<span class='stat'><i class='fas fa-gift'></i> Bonus</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card premium'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🏆</div>");
            sb.Append("<div class='feature-badge'>PRO</div>");
            sb.Append("</div>");
            sb.Append("<h3>Time Challenges</h3>");
            sb.Append("<p>Weekly speed challenges. Complete 5 surveys under 3 minutes each for exclusive rewards!</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-flag-checkered'></i> Challenge</span>");
            sb.Append("<span class='stat'><i class='fas fa-medal'></i> Rewards</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            // Achievement Banner
            sb.Append("<div class='achievement-banner'>");
            sb.Append("<div class='banner-content'>");
            sb.Append("<div class='banner-icon'>🚀</div>");
            sb.Append("<div class='banner-text'>");
            sb.Append("<h3>Speed Demon Achievement</h3>");
            sb.Append("<p>You're 32% faster than average users!</p>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='back-button-container'>");
            sb.Append("<a href='/' class='btn-secondary'><i class='fas fa-arrow-left'></i> Back to Home</a>");
            sb.Append("</div>");
            
            sb.Append("</main>");
            sb.Append("</body></html>");
            
            return sb.ToString();
        }

        public static string GenerateSecurityPlatformHtml(string language, string username)
        {
            var sb = new StringBuilder();
            
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>Security Platform</title>");
            sb.Append("<link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'>");
            sb.Append("<link href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile.css?v={DateTime.Now.Ticks}' rel='stylesheet'>");
            sb.Append($"<link href='/css/mobile-features.css?v={DateTime.Now.Ticks}' rel='stylesheet'></head><body>");

            // Header
            sb.Append("<header class='app-header'>");
            sb.Append("<div class='brand-logo'><img src='/assets/apptad-technologies-pvt-ltd-original-removebg-preview.png' class='logo' /></div>");
            sb.Append("<div class='brand-title'>SECURITY PLATFORM</div>");
            sb.Append("<div class='header-actions'>");
            sb.Append("<button class='icon-btn' onclick='toggleSettings()'>⚙️</button>");
            sb.Append("<button class='icon-btn' onclick='toggleAccount()'>👤</button>");
            sb.Append("</div></header>");

            // Hero Section
            sb.Append("<main class='feature-page'>");
            sb.Append("<div class='hero-section'>");
            sb.Append("<div class='hero-icon'>🛡️</div>");
            sb.Append($"<h1 class='hero-title'>{GetLocalizedText("Fortress Security", language)}</h1>");
            sb.Append($"<p class='hero-subtitle'>{GetLocalizedText("Enterprise-grade protection for your peace of mind", language)}</p>");
            sb.Append("</div>");

            // Security Score
            sb.Append("<div class='security-score'>");
            sb.Append("<div class='score-circle'>");
            sb.Append("<div class='score-value'>100%</div>");
            sb.Append("<div class='score-label'>Security Score</div>");
            sb.Append("</div>");
            sb.Append("<div class='score-details'>");
            sb.Append("<div class='score-item'>");
            sb.Append("<i class='fas fa-check-circle'></i>");
            sb.Append("<span>Zero Breaches</span>");
            sb.Append("</div>");
            sb.Append("<div class='score-item'>");
            sb.Append("<i class='fas fa-shield-alt'></i>");
            sb.Append("<span>Active Protection</span>");
            sb.Append("</div>");
            sb.Append("<div class='score-item'>");
            sb.Append("<i class='fas fa-lock'></i>");
            sb.Append("<span>Encrypted Data</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");

            // Feature Cards
            sb.Append("<div class='feature-grid'>");
            sb.Append("<div class='feature-card premium'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🔐</div>");
            sb.Append("<div class='feature-badge'>PRO</div>");
            sb.Append("</div>");
            sb.Append("<h3>Advanced Encryption</h3>");
            sb.Append("<p>Military-grade AES-256 encryption with TLS 1.3. Your data is protected at rest and in transit.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-key'></i> AES-256</span>");
            sb.Append("<span class='stat'><i class='fas fa-lock'></i> TLS 1.3</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🔍</div>");
            sb.Append("</div>");
            sb.Append("<h3>Security Audits</h3>");
            sb.Append("<p>Quarterly third-party security audits and continuous penetration testing ensure maximum protection.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-search'></i> Audits</span>");
            sb.Append("<span class='stat'><i class='fas fa-user-shield'></i> Tested</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🤖</div>");
            sb.Append("</div>");
            sb.Append("<h3>AI Security</h3>");
            sb.Append("<p>Machine learning algorithms detect and prevent threats in real-time 24/7 automated protection.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-robot'></i> AI</span>");
            sb.Append("<span class='stat'><i class='fas fa-brain'></i> Smart</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='feature-card premium'>");
            sb.Append("<div class='feature-icon-wrapper'>");
            sb.Append("<div class='feature-icon'>🌐</div>");
            sb.Append("<div class='feature-badge'>GLOBAL</div>");
            sb.Append("</div>");
            sb.Append("<h3>Global Compliance</h3>");
            sb.Append("<p>Fully compliant with GDPR, CCPA, HIPAA, SOC 2, and ISO 27001 international standards.</p>");
            sb.Append("<div class='feature-stats'>");
            sb.Append("<span class='stat'><i class='fas fa-certificate'></i> ISO 27001</span>");
            sb.Append("<span class='stat'><i class='fas fa-gavel'></i> Legal</span>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            // Security Certifications
            sb.Append("<div class='certifications'>");
            sb.Append("<h3 class='section-title'>Security Certifications</h3>");
            sb.Append("<div class='cert-grid'>");
            sb.Append("<div class='cert-item'>");
            sb.Append("<div class='cert-icon'>🏆</div>");
            sb.Append("<div class='cert-name'>ISO 27001</div>");
            sb.Append("<div class='cert-desc'>Information Security</div>");
            sb.Append("</div>");
            sb.Append("<div class='cert-item'>");
            sb.Append("<div class='cert-icon'>🛡️</div>");
            sb.Append("<div class='cert-name'>SOC 2</div>");
            sb.Append("<div class='cert-desc'>Service Organization</div>");
            sb.Append("</div>");
            sb.Append("<div class='cert-item'>");
            sb.Append("<div class='cert-icon'>🔒</div>");
            sb.Append("<div class='cert-name'>GDPR</div>");
            sb.Append("<div class='cert-desc'>Data Protection</div>");
            sb.Append("</div>");
            sb.Append("<div class='cert-item'>");
            sb.Append("<div class='cert-icon'>⚖️</div>");
            sb.Append("<div class='cert-name'>HIPAA</div>");
            sb.Append("<div class='cert-desc'>Healthcare Privacy</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</div>");
            
            sb.Append("<div class='back-button-container'>");
            sb.Append("<a href='/' class='btn-secondary'><i class='fas fa-arrow-left'></i> Back to Home</a>");
            sb.Append("</div>");
            
            sb.Append("</main>");
            sb.Append("</body></html>");
            
            return sb.ToString();
        }
    }
}
