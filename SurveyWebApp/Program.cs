using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using SurveyWebApp.Data;
using SurveyWebApp.Models;
using SurveyWebApp.Services;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.AspNetCore.ResponseCompression;
using System.Linq;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// ================= PERFORMANCE OPTIMIZATIONS =================
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "text/html", "text/css", "application/javascript", "image/svg+xml" });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.WriteIndented = false;
});

// ================= CORS =================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ================= DB =================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .EnableSensitiveDataLogging(false)
    .EnableServiceProviderCaching()
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
);

// Register Translation Service
builder.Services.AddScoped<SurveyWebApp.Services.WebTranslationService>();

// ================= AUTHENTICATION =================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ================= AUTO DATABASE CREATION =================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated(); // Creates database and tables
}

// ================= PERFORMANCE MONITORING =================
app.Use(async (context, next) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        await next();
    }
    finally
    {
        stopwatch.Stop();
        var duration = stopwatch.ElapsedMilliseconds;
        
        // Log slow requests (> 1000ms)
        if (duration > 1000)
        {
            Console.WriteLine($"[PERF] Slow request: {context.Request.Path} took {duration}ms");
        }
        
        // Only add performance header if response hasn't started
        if (!context.Response.HasStarted)
        {
            try
            {
                context.Response.Headers["X-Response-Time"] = $"{duration}ms";
            }
            catch
            {
                // Ignore header errors - response already started
            }
        }
    }
});

// ================= PERFORMANCE MIDDLEWARE =================
app.UseResponseCompression();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    await next();
});

// ================= SESSION MIDDLEWARE =================
app.UseSession();

// ================= STATIC FILES WITH CACHING =================
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (!string.IsNullOrEmpty(ctx.File.Name))
        {
            var extension = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
            switch (extension)
            {
                case ".css":
                case ".js":
                    ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000";
                    break;
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".ico":
                    ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=2592000";
                    break;
                default:
                    ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=86400";
                    break;
            }
        }
    }
});

var assetsPath = Path.Combine(builder.Environment.ContentRootPath, "Assets");
if (Directory.Exists(assetsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(assetsPath),
        RequestPath = "/assets"
    });
};

// ================= API CONTROLLERS =================
app.UseCors("AllowAll");
app.MapControllers();

// ================= AUTHENTICATION ENDPOINTS =================

// Helper function for password hashing
string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hashedBytes);
}

// Helper function to get localized title
string GetLocalizedTitle(SurveyWebApp.Models.Survey survey, string language)
{
    if (language == "en") return survey.Title;
    
    // Check if translation exists in database
    var translation = language switch
    {
        "hi" => !string.IsNullOrEmpty(survey.Title_Hi) ? survey.Title_Hi : null,
        "bn" => !string.IsNullOrEmpty(survey.Title_Bn) ? survey.Title_Bn : null,
        "gu" => !string.IsNullOrEmpty(survey.Title_Gu) ? survey.Title_Gu : null,
        "mr" => !string.IsNullOrEmpty(survey.Title_Mr) ? survey.Title_Mr : null,
        "ta" => !string.IsNullOrEmpty(survey.Title_Ta) ? survey.Title_Ta : null,
        "te" => !string.IsNullOrEmpty(survey.Title_Te) ? survey.Title_Te : null,
        "kn" => !string.IsNullOrEmpty(survey.Title_Kn) ? survey.Title_Kn : null,
        "ml" => !string.IsNullOrEmpty(survey.Title_Ml) ? survey.Title_Ml : null,
        _ => null
    };
    
    if (translation != null) return translation;
    
    // Auto-translate ANY survey title
    return AutoTranslateAnyText(survey.Title, language);
}

// Helper function to get localized description
string GetLocalizedDescription(SurveyWebApp.Models.Survey survey, string language)
{
    if (language == "en") return survey.Description ?? "Take this survey";
    
    // Check if translation exists in database
    var translation = language switch
    {
        "hi" => !string.IsNullOrEmpty(survey.Description_Hi) ? survey.Description_Hi : null,
        "bn" => !string.IsNullOrEmpty(survey.Description_Bn) ? survey.Description_Bn : null,
        "gu" => !string.IsNullOrEmpty(survey.Description_Gu) ? survey.Description_Gu : null,
        "mr" => !string.IsNullOrEmpty(survey.Description_Mr) ? survey.Description_Mr : null,
        "ta" => !string.IsNullOrEmpty(survey.Description_Ta) ? survey.Description_Ta : null,
        "te" => !string.IsNullOrEmpty(survey.Description_Te) ? survey.Description_Te : null,
        "kn" => !string.IsNullOrEmpty(survey.Description_Kn) ? survey.Description_Kn : null,
        "ml" => !string.IsNullOrEmpty(survey.Description_Ml) ? survey.Description_Ml : null,
        _ => null
    };
    
    if (translation != null) return translation;
    
    // Auto-translate ANY survey description
    return AutoTranslateAnyText(survey.Description ?? "Take this survey", language);
}

// Enhanced auto-translation for survey titles
string AutoTranslateAnyText(string text, string language)
{
    if (string.IsNullOrEmpty(text) || language == "en") return text;
    
    // Real translations for common survey content
    var translations = new Dictionary<string, Dictionary<string, string>>
    {
        ["Customer Satisfaction Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "ग्राहक संतुष्टि सर्वेक्षण",
            ["bn"] = "গ্রাহক সন্তুষ্টি জরিপ",
            ["gu"] = "ગ્રાહક સંતુષ્ટિ સર્વેક્ષણ",
            ["mr"] = "ग्राहक समाधान सर्वेक्षण",
            ["ta"] = "வாடிகர் திருப்தி கணக்கெடுப்பு",
            ["te"] = "వినియోగి సంతృప్తి సర్వే",
            ["kn"] = "ಗ್ರಾಹಕರ ತೃಪ್ತಿ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ഉപഭോക്തൃപ്പതി സർവേ"
        },
        ["Employee Engagement Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "कर्मचारी संलग्नता सर्वेक्षण",
            ["bn"] = "কর্মচারী জড়তা জরিপ",
            ["gu"] = "કર્મચારી સંલગ્નતા સર્વેક્ષણ",
            ["mr"] = "कर्मचारी सहभाग सर्वेक्षण",
            ["ta"] = "ஊழியர் ஈடுபாடு கணக்கெடுப்பு",
            ["te"] = "ఉద్యోగి నిమగ్నత సర్వే",
            ["kn"] = "ಉದ್ಯೋಗಿ ತೊಡಗುವಿಕೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ജീവനക്കാരൻ ഇൻവോൾവ്മെന്റ് സർവേ"
        },
        ["Product Feedback Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "उत्पाद प्रतिक्रिया सर्वेक्षण",
            ["bn"] = "পণ্য প্রতিক্রিযা জরিপ",
            ["gu"] = "પ્રોડક્ટ ફીડબેક સર્વેક્ષણ",
            ["mr"] = "उत्पादन प्रतिसाद सर्वेक्षण",
            ["ta"] = "தயாரிப்பு கருத்து கணக்கெடுப்பு",
            ["te"] = "ఉత్పత్తి అభిప్రాయ సర్వే",
            ["kn"] = "ಉತ್ಪನ್ನ ಪ್ರತಿಕ್ರಿಯೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ഉൽപ്പന്ന ഫീഡ്ബാക്ക് സർവേ"
        },
        ["Training Effectiveness Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "प्रशिक्षण प्रभावकारिता सर्वेक्षण",
            ["bn"] = "প্রশিক্ষণ কার্যকারিতা জরিপ",
            ["gu"] = "તાલીમ અસરકારકતા સર્વેક્ષણ",
            ["mr"] = "प्रशिक्षण प्रभावीपणा सर्वेक्षण",
            ["ta"] = "பயிற்சி செயல்திறன் கணக்கெடுப்பு",
            ["te"] = "శిక్షణ ప్రభావవంతమైన సర్వే",
            ["kn"] = "ತರಬೇತಿ ಪರಿಣಾಮಕಾರಿತೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "പരിശീലന ഫലപ്രാചലി സർവേ"
        },
        ["Workplace Environment Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "कार्यस्थल पर्यावरण सर्वेक्षण",
            ["bn"] = "কর্মস্থল পরিবেশ জরিপ",
            ["gu"] = "કાર્યસ્થળ પર્યાવરણ સર્વેક્ષણ",
            ["mr"] = "कार्यस्थळ पर्यावरण सर्वेक्षण",
            ["ta"] = "வேலையிடல் சூழல் கணக்கெடுப்பு",
            ["te"] = "కార్యాలయ వాతావరణ సర్వే",
            ["kn"] = "ಕೆಲಸದ ಪರಿಸರ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ജോലിസ്ഥലം പരിസ്ഥിതി സർവേ"
        },
        ["Leadership Assessment Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "नेतृत्व मूल्यांकन सर्वेक्षण",
            ["bn"] = "নেতৃত্ব মূল্যাযন জরিপ",
            ["gu"] = "લીડરશીપ મૂલ્યાંકન સર્વેક્ષણ",
            ["mr"] = "नेतृत्व मूल्यमापन सर्वेक्षण",
            ["ta"] = "தலைமைத்துவ மதிப்பீடு கணக்கெடுப்பு",
            ["te"] = "నాయకత్వ మూల్యాంకన సర్వే",
            ["kn"] = "ನಾಯಕತ್ವ ಮೌಲ್ಯಮಾಪನ ಸರ್ವೇಕ్ಷಣ",
            ["ml"] = "നേതൃത്വ വിലയിരുത്തൽ സർവേ"
        },
        ["IT Satisfaction Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "आईटी संतुष्टि सर्वेक्षण",
            ["bn"] = "আইটি সন্তুষ্টি জরিপ",
            ["gu"] = "આઈટી સંતુષ્ટિ સર્વેક્ષણ",
            ["mr"] = "आयटी समाधान सर्वेक्षण",
            ["ta"] = "ஐடி திருப்தி கணக்கெடுப்பு",
            ["te"] = "ఐటి సంతృప్తి సర్వే",
            ["kn"] = "ಐಟಿ ಸಂತುಷ್ಟಿ ಸರ್ವೇಕ్ಷಣ",
            ["ml"] = "ഐടി സംതൃപ്തി സർവേ"
        },
        ["HR Services Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "एचआर सेवाएं सर्वेक्षण",
            ["bn"] = "এইচআর সেবা জরিপ",
            ["gu"] = "એચઆર સેવાઓ સર્વેક્ષણ",
            ["mr"] = "एचआर सेवा सर्वेक्षण",
            ["ta"] = "ஹெச்ஆர் சேவைகள் கணக்கெடுப்பு",
            ["te"] = "హెచ్‌ఆర్ సేవల సర్వే",
            ["kn"] = "ಎಚ್‌ಆರ್ ಸೇವೆಗಳ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "എച്ച്ആർ സേവനങ്ങൾ സർവേ"
        },
        ["Benefits Satisfaction Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "लाभ संतुष्टि सर्वेक्षण",
            ["bn"] = "সুবিধা সন্তুষ্টি জরিপ",
            ["gu"] = "લાભો સંતુષ્ટિ સર્વેક્ષણ",
            ["mr"] = "फायदे समाधान सर्वेक्षण",
            ["ta"] = "நன்மைகள் திருப்தி கணக்கெடுப்பு",
            ["te"] = "ప్రయోజనాలు సంతృప్తి సర్వే",
            ["kn"] = "ಪ್ರಯೋಜನಗಳ ಸಂತುಷ್ಟಿ ಸರ್ವೇಕ્ಷಣ",
            ["ml"] = "ആനുകൂല്യങ്ങൾ സംതൃപ്തി സർവേ"
        },
        ["Communication Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "संचार सर्वेक्षण",
            ["bn"] = "যোগাযোগ জরিপ",
            ["gu"] = "સંચાર સર્વેક્ષણ",
            ["mr"] = "संचार सर्वेक्षण",
            ["ta"] = "தகவல் தொடர்பு கணக்கெடுப்பு",
            ["te"] = "వ్యాపార సందేశాల సర్వే",
            ["kn"] = "ಸಂವಹನ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ആശയവിനിമയ സർവേ"
        },
        ["Diversity and Inclusion Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "विविधता और समावेश सर्वेक्षण",
            ["bn"] = "বৈচিত্র্য ও অন্তর্ভুক্তি জরিপ",
            ["gu"] = "વિવિધતા અને સમાવેશ સર્વેક્ષણ",
            ["mr"] = "विविधता आणि समावेश सर्वेक्षण",
            ["ta"] = "பன்முகம் மற்றும் உள்ளடக்கம் கணக்கெடுப்பு",
            ["te"] = "వైవిధ్యం మరియు సమావేశ సర్వే",
            ["kn"] = "ವೈವಿಧ್ಯ ಮತ್ತು ಸೇರ್ಪಡುವಿಕೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "വൈവിധ്യവും ഉൾപ്പെടുത്തലും സർവേ"
        },
        ["Performance Review Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "प्रदर्शन समीक्षा सर्वेक्षण",
            ["bn"] = "পারফরম্যান্স পর্যালোচনা জরিপ",
            ["gu"] = "પ્રદર્શન સમીક્ષા સર્વેક્ષણ",
            ["mr"] = "कार्यक्षमता समीक्षा सर्वेक्षण",
            ["ta"] = "செயல்திறன் மதிப்பாய்வு கணக்கெடுப்பு",
            ["te"] = "పనితనం సమీక్ష సర్వే",
            ["kn"] = "ಕಾರ್ಯಕ್ಷಮತೆ ಪರಿಶೀಲನೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "പ്രകടന വിലയിരുത്തൽ സർവേ"
        },
        ["Safety Culture Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "सुरक्षा संस्कृति सर्वेक्षण",
            ["bn"] = "নিরাপত্তা সংস্কৃতি জরিপ",
            ["gu"] = "સુરક્ષા સંસ્કૃતિ સર્વેક્ષણ",
            ["mr"] = "सुरक्षा संस्कृती सर्वेक्षण",
            ["ta"] = "பாதுகாப்பு பண்பாடு கணக்கெடுப்பு",
            ["te"] = "భద్రతా సంస్కృతి సర్వే",
            ["kn"] = "ಸುರಕ್ಷಾ ಸಂಸ್ಕೃತಿ ಸರ್ವೇక్ಷಣ",
            ["ml"] = "സുരക്ഷാ സംസ്കാരം സർവേ"
        },
        ["Innovation Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "नवाचार सर्वेक्षण",
            ["bn"] = "উদ্ভাবন জরিপ",
            ["gu"] = "નવીનતા સર્વેક્ષણ",
            ["mr"] = "नवकल्पना सर्वेक्षण",
            ["ta"] = "புதுமை கணக்கெடுப்பு",
            ["te"] = "నవీకరణ సర్వే",
            ["kn"] = "ಹೊಸತನ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "നവീകരണം സർവേ"
        },
        ["Remote Work Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "रिमोट कार्य सर्वेक्षण",
            ["bn"] = "দূরবর্তী কাজ জরিপ",
            ["gu"] = "રિમોટ કામ સર્વેક્ષણ",
            ["mr"] = "रिमोट काम सर्वेक्षण",
            ["ta"] = "தொலைவு வேலை கணக்கெடுப்பு",
            ["te"] = "రిమోట్ వర్క్ సర్వే",
            ["kn"] = "ದೂರಸ್ಥ ಕೆಲಸ ಸರ್ವೇಕ్ಷಣ",
            ["ml"] = "റിമോട്ട് ജോലി സർവേ"
        },
        ["Wellness Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "कल्याण सर्वेक्षण",
            ["bn"] = "সুস্থতা জরিপ",
            ["gu"] = "સુખાકાર સર્વેક્ષણ",
            ["mr"] = "आरोग्य सर्वेक्षण",
            ["ta"] = "ஆரோக்கியம் கணக்கெடுப்பு",
            ["te"] = "ఆరోగ్య సర్వే",
            ["kn"] = "ಆರೋಗ್ಯ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ആരോഗ്യം സർവേ"
        },
        ["Career Development Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "करियर विकास सर्वेक्षण",
            ["bn"] = "ক্যারিয়ার উন্নয়ন জরিপ",
            ["gu"] = "કારકિર્દી વિકાસ સર્વેક્ષણ",
            ["mr"] = "कारकीर्द विकास सर्वेक्षण",
            ["ta"] = "தொழில் வளர்ச்சி கணக்கெடுப்பு",
            ["te"] = "కెరీర్ అభివృద్ధి సర్వే",
            ["kn"] = "ವೃತ್ತಿ ಅಭಿವೃದ್ಧಿ ಸರ್ವೇಕ్ಷಣ",
            ["ml"] = "കരിയർ വികസനം സർവേ"
        },
        ["Team Collaboration Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "टीम सहयोग सर्वेक्षण",
            ["bn"] = "দল সহযোগ জরিপ",
            ["gu"] = "ટીમ સહયોગ સર્વેક્ષણ",
            ["mr"] = "टीम सहकार्य सर्वेक्षण",
            ["ta"] = "குழு ஒத்துழைப்பு கணக்கெடுப்பு",
            ["te"] = "బృంద సహకార సర్వే",
            ["kn"] = "ತಂಡ ಸಹಯೋಗ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ടീം സഹകരണം സർവേ"
        },
        ["Customer Service Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "ग्राहक सेवा सर्वेक्षण",
            ["bn"] = "গ্রাহক সেবা জরিপ",
            ["gu"] = "ગ્રાહક સેવા સર્વેક્ષણ",
            ["mr"] = "ग्राहक सेवा सर्वेक्षण",
            ["ta"] = "வாடிகர் சேவை கணக்கெடுப்பு",
            ["te"] = "వినియోగదారు సేవ సర్వే",
            ["kn"] = "ಗ್ರಾಹಕ ಸೇವೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ഉപഭോക്തൃ സേവനം സർവേ"
        },
        ["Market Research Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "बाजार अनुसंधान सर्वेक्षण",
            ["bn"] = "বাজার গবেষণা জরিপ",
            ["gu"] = "માર્કેટ રિસર્ચ સર્વેક્ષણ",
            ["mr"] = "बाजार संशोधन सर्वेक्षण",
            ["ta"] = "சந்தை ஆய்வு கணக்கெடுப்பு",
            ["te"] = "మార్కెట్ రిసెర్చ్ సర్వే",
            ["kn"] = "ಮಾರುಕಟ್ಟೆ ಸಂಶೋಧನೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "മാർക്കറ്റ് റിസർച്ച് സർവേ"
        },
        ["User Experience Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "उपयोगकर्ता अनुभव सर्वेक्षण",
            ["bn"] = "ব্যবহারকারী অভিজ্ঞতা জরিপ",
            ["gu"] = "વપરાશકર્તા અનુભવ સર્વેક્ષણ",
            ["mr"] = "वापरकर्ता अनुभव सर्वेक्षण",
            ["ta"] = "பயனர் அனுபவம் கணக்கெடுப்பு",
            ["te"] = "వినియోగదారు అనుభవం సర్వే",
            ["kn"] = "ಬಳಕೆದಾರ ಅನುಭವ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ഉപയോക്തൃ അനുഭവം സർവേ"
        },
        ["Customer Feedback"] = new Dictionary<string, string>
        {
            ["hi"] = "ग्राहक प्रतिक्रिया",
            ["bn"] = "গ্রাহক প্রতিক্রিযা",
            ["gu"] = "ગ્રાહક પ્રતિસાદ",
            ["mr"] = "ग्राहक प्रतिसाद",
            ["ta"] = "வாடிகர் கருத்து",
            ["te"] = "వినియోగదారు అభిప్రాయం",
            ["kn"] = "ಗ್ರಾಹಕ ಪ್ರತಿಕ್ರಿಯೆ",
            ["ml"] = "ഉപഭോക്തൃ അഭിപ്രായം"
        },
        ["Website Experience Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "वेबसाइट अनुभव सर्वेक्षण",
            ["bn"] = "ওয়েবসাইট অভিজ্ঞতা জরিপ",
            ["gu"] = "વેબસાઇટ અનુભવ સર્વેક્ષણ",
            ["mr"] = "वेबसाइट अनुभव सर्वेक्षण",
            ["ta"] = "வலைத்தள அனுபவம் கணக்கெடுப்பு",
            ["te"] = "వెబ్‌సైట్ అనుభవం సర్వే",
            ["kn"] = "ವೆಬ್‌ಸೈಟ್ ಅನುಭವ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "വെബ്സൈറ്റ് അനുഭവം സർവേ"
        },
        ["Training Feedback Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "प्रशिक्षण प्रतिक्रिया सर्वेक्षण",
            ["bn"] = "প্রশিক্ষণ প্রতিক্রিযা জরিপ",
            ["gu"] = "તાલીમ પ્રતિસાદ સર્વેક્ષણ",
            ["mr"] = "प्रशिक्षण प्रतिसाद सर्वेक्षण",
            ["ta"] = "பயிற்சி கருத்து கணக்கெடுப்பு",
            ["te"] = "శిక్షణ అభిప్రాయ సర్వే",
            ["kn"] = "ತರಬೇತಿ ಪ್ರತಿಕ್ರಿಯೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "പരിശീലന ഫീഡ്ബാക്ക് സർവേ"
        },
        ["Market Research Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "बाजार अनुसंधान सर्वेक्षण",
            ["bn"] = "বাজার গবেষণা জরিপ",
            ["gu"] = "માર્કેટ રિસર્ચ સર્વેક્ષણ",
            ["mr"] = "बाजार संशोधन सर्वेक्षण",
            ["ta"] = "சந்தை ஆய்வு கணக்கெடுப்பு",
            ["te"] = "మార్కెట్ రిసెర్చ్ సర్వే",
            ["kn"] = "ಮಾರುಕಟ್ಟೆ ಸಂಶೋಧನೆ ಸರ್ವేಕ್ಷಣ",
            ["ml"] = "മാർക്കറ്റ് റിസർച്ച് സർവേ"
        },
        ["Student Feedback Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "छात्र प्रतिक्रिया सर्वेक्षण",
            ["bn"] = "ছাত্র প্রতিক্রিযা জরিপ",
            ["gu"] = "વિદ્યાર્થી પ્રતિસાદ સર્વેક્ષણ",
            ["mr"] = "विद्यार्थी प्रतिसाद सर्वेक्षण",
            ["ta"] = "மாணவர் கருத்து கணக்கெடுப்பு",
            ["te"] = "విద్యార్థి అభిప్రాయ సర్వే",
            ["kn"] = "ವಿದ್ಯಾರ್ಥಿ ಪ್ರತಿಕ್ರಿಯೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "വിദ്യാർത്ഥി ഫീഡ്ബാക്ക് സർവേ"
        },
        ["Opinion Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "अभिप्राय सर्वेक्षण",
            ["bn"] = "মতামত জরিপ",
            ["gu"] = "અભિપ્રાય સર્વેક્ષણ",
            ["mr"] = "मत सर्वेक्षण",
            ["ta"] = "கருத்து கணக்கெடுப்பு",
            ["te"] = "అభిప్రాయ సర్వే",
            ["kn"] = "ಅಭಿಪ್ರಾಯ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "അഭിപ്രായം സർവേ"
        },
        ["Product Evaluation Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "उत्पाद मूल्यांकन सर्वेक्षण",
            ["bn"] = "পণ্য মূল্যাযন জরিপ",
            ["gu"] = "પ્રોડક્ટ મૂલ્યાંકન સર્વેક્ષણ",
            ["mr"] = "उत्पादन मूल्यांकन सर्वेक्षण",
            ["ta"] = "தயாரிப்பு மதிப்பீடு கணக்கெடுப்பு",
            ["te"] = "ఉత్పత్తి మూల్యాంకన సర్వే",
            ["kn"] = "ಉತ್ಪನ್ನ ಮೌಲ್ಯಮಾಪನ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ഉൽപ്പന്ന മൂല്യനിരൂപണം സർവേ"
        },
        ["Website/App Usability Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "वेबसाइट/ऐप उपयोगिता सर्वेक्षण",
            ["bn"] = "ওয়েবসাইট/অ্যাপ ইউজেবিলিটি জরিপ",
            ["gu"] = "વેબસાઇટ/એપ ઉપયોગિતા સર્વેક્ષણ",
            ["mr"] = "वेबसाइट/अॅप उपयोगिता सर्वेक्षण",
            ["ta"] = "வலைத்தளம்/செயலி பயன்பாட்டு கணக்கெடுப்பு",
            ["te"] = "వెబ్‌సైట్/యాప్ వాడకదారీ సర్వే",
            ["kn"] = "ವೆಬ್‌ಸೈಟ್/ಆ್ಯಪ್ ಬಳಕೆಯೋಗ್ಯತೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "വെബ്സൈറ്റ്/ആപ്പ് ഉപയോഗക്ഷമത സർവേ"
        },
        ["Health Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "स्वास्थ्य सर्वेक्षण",
            ["bn"] = "স্বাস্থ্য জরিপ",
            ["gu"] = "સ્વાસ્થ્ય સર્વેક્ષણ",
            ["mr"] = "आरोग्य सर्वेक्षण",
            ["ta"] = "ஆரோக்கியம் கணக்கெடுப்பு",
            ["te"] = "ఆరోగ్య సర్వే",
            ["kn"] = "ಆರೋಗ್ಯ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ആരോഗ്യം സർവേ"
        },
        ["Needs Assessment Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "आवश्यकता मूल्यांकन सर्वेक्षण",
            ["bn"] = "চাহিদা মূল্যাযন জরিপ",
            ["gu"] = "જરૂરિયાત મૂલ્યાંકન સર્વેક્ષણ",
            ["mr"] = "गरज मूल्यांकन सर्वेक्षण",
            ["ta"] = "தேவைகள் மதிப்பீடு கணக்கெடுப்பு",
            ["te"] = "అవసరాల మూల్యాంకన సర్వే",
            ["kn"] = "ಅಗತ್ಯಗಳ ಮೌಲ್ಯಮಾಪನ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ആവശ്യകതാ മൂല്യനിരൂപണം സർവേ"
        },
        ["Brand Awareness Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "ब्रांड जागरूकता सर्वेक्षण",
            ["bn"] = "ব্র্যান্ড সচেতনতা জরিপ",
            ["gu"] = "બ્રાન્ડ જાગૃતિ સર્વેક્ષણ",
            ["mr"] = "ब्रँड जागरूकता सर्वेक्षण",
            ["ta"] = "பெயர் விழிப்புணர்வு கணக்கெடுப்பு",
            ["te"] = "బ్రాండ్ అవగాహన సర్వే",
            ["kn"] = "ಬ್ರಾಂಡ್ ಅರಿವು ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ബ്രാൻഡ് അവബോധം സർവേ"
        },
        ["Brand Loyalty Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "ब्रांड निष्ठा सर्वेक्षण",
            ["bn"] = "ব্র্যান্ড আনুগত্য জরিপ",
            ["gu"] = "બ્રાન્ડ વફાદારી સર્વેક્ષણ",
            ["mr"] = "ब्रँड निष्ठा सर्वेक्षण",
            ["ta"] = "பெயர் விசுவாசம் கணக்கெடுப்பு",
            ["te"] = "బ్రాండ్ విధేయత సర్వే",
            ["kn"] = "ಬ್ರಾಂಡ್ ನಿಷ್ಠೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ബ്രാൻഡ് വിശ്വാസ്യത സർവേ"
        },
        ["Training Effectiveness Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "प्रशिक्षण प्रभावकारिता सर्वेक्षण",
            ["bn"] = "প্রশিক্ষণ কার্যকারিতা জরিপ",
            ["gu"] = "તાલીમ અસરકારકતા સર્વેક્ષણ",
            ["mr"] = "प्रशिक्षण प्रभावीपणा सर्वेक्षण",
            ["ta"] = "பயிற்சி செயல்திறன் கணக்கெடுப்பு",
            ["te"] = "శిక్షణ ప్రభావవంతమైన సర్వే",
            ["kn"] = "ತರಬೇತಿ ಪರಿಣಾಮಕಾರಿತೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "പരിശീലന ഫലപ്രാചലി സർവേ"
        },
        ["Event Feedback Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "इवेंट प्रतिक्रिया सर्वेक्षण",
            ["bn"] = "ইভেন্ট প্রতিক্রিযা জরিপ",
            ["gu"] = "ઇવેન્ટ પ્રતિસાદ સર્વેક્ષણ",
            ["mr"] = "कार्यक्रम प्रतिसाद सर्वेक्षण",
            ["ta"] = "நிகழ்வு கருத்து கணக்கெடுப்பு",
            ["te"] = "ఈవెంట్ అభిప్రాయ సర్వే",
            ["kn"] = "ಕಾರ್ಯಕ್ರಮ ಪ್ರತಿಕ್ರಿಯೆ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ഇവെന്റ് ഫീഡ്ബാക്ക് സർവേ"
        },
        ["Course Evaluation Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "पाठ्यक्रम मूल्यांकन सर्वेक्षण",
            ["bn"] = "কোর্স মূল্যাযন জরিপ",
            ["gu"] = "કોર્સ મૂલ્યાંકન સર્વેક્ષણ",
            ["mr"] = "अभ्यासक्रम मूल्यांकन सर्वेक्षण",
            ["ta"] = "பாடவழி மதிப்பீடு கணக்கெடுப்பு",
            ["te"] = "కోర్స్ మూల్యాంకన సర్వే",
            ["kn"] = "ಪಠ್ಯಕ್ರಮ ಮೌಲ್ಯಮಾಪನ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "കോഴ്‌സ് മൂല്യനിരൂപണം സർവേ"
        },
        ["Exit Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "निकास सर्वेक्षण",
            ["bn"] = "প্রস্থান জরিপ",
            ["gu"] = "બહાર નીકળવાનું સર્વેક્ષણ",
            ["mr"] = "बाहेर पडणे सर्वेक्षण",
            ["ta"] = "வெளியேறு கணக்கெடுப்பு",
            ["te"] = "నిష్క్రమణ సర్వే",
            ["kn"] = "ನಿರ್ಗಮನ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "പുറത്തിറങ്ങൽ സർവേ"
        },
        ["Workplace Safety Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "कार्यस्थल सुरक्षा सर्वेक्षण",
            ["bn"] = "কর্মস্থল নিরাপত্তা জরিপ",
            ["gu"] = "કાર્યસ્થળ સુરક્ષા સર્વેક્ષણ",
            ["mr"] = "कार्यस्थळ सुरक्षा सर्वेक्षण",
            ["ta"] = "வேலையிடல் பாதுகாப்பு கணக்கெடுப்பு",
            ["te"] = "కార్యాలయ భద్రతా సర్వే",
            ["kn"] = "ಕೆಲಸದ ಸ್ಥಳದ ಸುರಕ್ಷಾ ಸರ್ವೇಕ್ಷಣ",
            ["ml"] = "ജോലിസ്ഥലം സുരക്ഷാ സർവേ"
        },
        
        // Sample Questions
        ["Which of the following are programming paradigms? (Select all that apply)"] = new Dictionary<string, string>
        {
            ["hi"] = "निम्नलिखित में से कौन से प्रोग्रामिंग पैराडाइम हैं? (सभी लागू चुनें)",
            ["bn"] = "নিচের কোনগুলো প্রোগ্রামিং প্যারাডাইম? (প্রযোজ্য সব নির্বাচন করুন)",
            ["gu"] = "નીચેના કયા પ્રોગ્રામિંગ પેરેડાઇમ્સ છે? (લાગુ પડતા બધા પસંદ કરો)",
            ["mr"] = "खालीलपैकी कोणते प्रोग्रामिंग पॅराडाइम आहेत? (लागू असलेले सर्व निवडा)",
            ["ta"] = "பின்வரும் எவை நிரலாக்க முறைகள்? (பொருந்தும் தேர்ந்தெடுக்கவும்)",
            ["te"] = "కింది వాటిలో ఏవి ప్రోగ్రామింగ్ పారడిగ్మ్‌లు? (వర్తించే అన్నింటిని ఎంచుకోండి)",
            ["kn"] = "ಕೆಳಗಿನವುಗಳಲ್ಲಿ ಯಾವುದು ಪ್ರೋಗ್ರಾಮಿಂಗ್ ಪ್ಯಾರಾಡಿಗಂಗಳು? (ಅನ್ವಯವಾಗುವ ಎಲ್ಲವನ್ನು ಆಯಿಸಿ)",
            ["ml"] = "താഴെപ്പറയുന്നവയിൽ ഏതൊക്കെയാണ് പ്രോഗ്രാമിംഗ് പാരഡൈമുകൾ? (ബാധകമായവ എല്ലാം തിരഞ്ഞെടുക്കുക)"
        },
        ["HTTP stands for HyperText Transfer Protocol."] = new Dictionary<string, string>
        {
            ["hi"] = "HTTP का मतलब हाइपरटेक्स्ट ट्रांसफर प्रोटोकॉल है।",
            ["bn"] = "HTTP এর পূর্ণরূপ হাইপারটেক্সট ট্রান্সফার প্রোটোকল।",
            ["gu"] = "HTTP નો અર્થ હાઇપરટેક્સ્ટ ટ્રાન્સફર પ્રોટોકોલ છે.",
            ["mr"] = "HTTP म्हणजे हायपरटेक्स्ट ट्रान्सफर प्रोटोकॉल.",
            ["ta"] = "HTTP என்பது ஹைபர்டெக்ஸ்ட் டிரான்ஸ்பர் புரோட்டோகால்.",
            ["te"] = "HTTP అంటే హైపర్‌టెక్స్ట్ ట్రాన్స్ఫర్ ప్రోటోకాల్.",
            ["kn"] = "HTTP ಎಂದರೆ ಹೈಪರ್‌ಟೆಕ್ಸ್ಟ್ ಟ್ರಾನ್ಸ್ಫರ್ ಪ್ರೋಟೋಕಾಲ್.",
            ["ml"] = "HTTP എന്നാൽ ഹൈപർടെക്സ്റ്റ് ട്രാൻസ്ഫർ പ്രോട്ടോക്കോൾ."
        },
        
        // Sample Options
        ["To improve data security"] = new Dictionary<string, string>
        {
            ["hi"] = "डेटा सुरक्षा में सुधार करने के लिए",
            ["bn"] = "ডেটা নিরাপত্তা উন্নত করার জন্য",
            ["gu"] = "ડેટા સુરક્ષામાં સુધારો કરવા માટે",
            ["mr"] = "डेटा सुरक्षा सुधारण्यासाठी",
            ["ta"] = "தரவு பாதுகாப்பை மேம்படுத்துவதற்கு",
            ["te"] = "డేటా భద్రతను మెరుగుపరచడానికి",
            ["kn"] = "ಡೇಟಾ ಭದ್ರತೆಯನ್ನು ಸುಧಾರಿಸಲು",
            ["ml"] = "ഡാറ്റ സുരക്ഷിതത്വം മെച്ചപ്പെടുത്തുന്നതിന്"
        },
        ["To speed up query performance"] = new Dictionary<string, string>
        {
            ["hi"] = "क्वेरी प्रदर्शन को गति देने के लिए",
            ["bn"] = "কোয়েরি পারফরম্যান্স বাড়ানোর জন্য",
            ["gu"] = "ક્વેરી પ્રદર્શનને ઝડપી કરવા માટે",
            ["mr"] = "क्वेरी कार्यक्षमता वाढवण्यासाठी",
            ["ta"] = "வினவல் செயல்திறனை வேகமாக்குவதற்கு",
            ["te"] = "క్వరీ పనితనాన్ని వేగవంతం చేయడానికి",
            ["kn"] = "ಪ್ರಶ್ನೆ ಪ್ರದರ್ಶನವನ್ನು ವೇಗಗೊಳಿಸಲು",
            ["ml"] = "ക്വറി പെർഫോമൻസ് വേഗത്തിലാക്കാൻ"
        },
        ["To reduce storage space"] = new Dictionary<string, string>
        {
            ["hi"] = "भंडारण स्थान को कम करने के लिए",
            ["bn"] = "স্টোরেজ স্পেস কমানোর জন্য",
            ["gu"] = "સંગ્રહ જગ્યામાં ઘટાડવા માટે",
            ["mr"] = "संचयन जागा कमी करण्यासाठी",
            ["ta"] = "சேமிப்பக இடத்தை குறைப்பதற்கு",
            ["te"] = "నిల్వ స్థలాన్ని తగ్గించడానికి",
            ["kn"] = "ಶೇಖರಣಾ ಸ್ಥಳವನ್ನು ಕಡಿಮೆ ಮಾಡಲು",
            ["ml"] = "സ്റ്റോറേജ് സ്പേസ് കുറയ്ക്കുന്നതിന്"
        },
        ["To encrypt data"] = new Dictionary<string, string>
        {
            ["hi"] = "डेटा को एन्क्रिप्ट करने के लिए",
            ["bn"] = "ডেটা এনক্রিপ্ট করার জন্য",
            ["gu"] = "ડેટાને એન્ક્રિપ્ટ કરવા માટે",
            ["mr"] = "डेटा एन्क्रिप्ट करण्यासाठी",
            ["ta"] = "தரவை மறைகுறித்து வைப்பதற்கு",
            ["te"] = "డేటాను ఎన్క్రిప్ట్ చేయడానికి",
            ["kn"] = "ಡೇಟಾವನ್ನು ಎನ್ಕ್ರಿಪ್ಟ್ ಮಾಡಲು",
            ["ml"] = "ഡാറ്റ എൻക്രിപ്റ്റ് ചെയ്യുന്നതിന്"
        },
        ["Object-Oriented"] = new Dictionary<string, string>
        {
            ["hi"] = "ऑब्जेक्ट-ओरिएंटेड",
            ["bn"] = "অবজেক্ট-ওরিয়েন্টেড",
            ["gu"] = "ઓબ્જેક્ટ-ઓરિએન્ટેડ",
            ["mr"] = "ऑब्जेक्ट-ओरिएंटेड",
            ["ta"] = "பொருள்-சார்ந்த",
            ["te"] = "ఆబ్జెక్ట్-ఓరియెంటెడ్",
            ["kn"] = "ಆಬ್ಜೆಕ್ಟ್-ಓರಿಯೆಂಟೆಡ್",
            ["ml"] = "ഒബ്ജക്റ്റ്-ഓറിയെന്റഡ്"
        },
        ["Functional"] = new Dictionary<string, string>
        {
            ["hi"] = "फंक्शनल",
            ["bn"] = "ফাংশনাল",
            ["gu"] = "ફંક્શનલ",
            ["mr"] = "फंक्शनल",
            ["ta"] = "செயல்பாட்டு",
            ["te"] = "ఫంక్షనల్",
            ["kn"] = "ಕಾರ್ಯಾತ್ಮಕ",
            ["ml"] = "ഫംഗ്ഷണൽ"
        },
        ["Procedural"] = new Dictionary<string, string>
        {
            ["hi"] = "प्रोसीजरल",
            ["bn"] = "প্রসিডিউরাল",
            ["gu"] = "પ્રોસીજરલ",
            ["mr"] = "प्रोसीजरल",
            ["ta"] = "செயல்முறை",
            ["te"] = "ప్రొసీడరల్",
            ["kn"] = "ವಿಧಾನಾತ್ಮಕ",
            ["ml"] = "പ്രൊസീഡറൽ"
        },
        ["Magical"] = new Dictionary<string, string>
        {
            ["hi"] = "जादुई",
            ["bn"] = "জাদুকরী",
            ["gu"] = "જાદુઈ",
            ["mr"] = "जादुई",
            ["ta"] = "மாயாவி",
            ["te"] = "మాంత్రిక",
            ["kn"] = "ಮಾಯೆಯ",
            ["ml"] = "മാന്ത്രികം"
        },
        ["True"] = new Dictionary<string, string>
        {
            ["hi"] = "सच",
            ["bn"] = "সত্য",
            ["gu"] = "સાચું",
            ["mr"] = "खरे",
            ["ta"] = "சரி",
            ["te"] = "నిజం",
            ["kn"] = "ನಿಜ",
            ["ml"] = "ശരി"
        },
        ["False"] = new Dictionary<string, string>
        {
            ["hi"] = "गलत",
            ["bn"] = "মিথ্যা",
            ["gu"] = "ખોટું",
            ["mr"] = "खोटे",
            ["ta"] = "தவறு",
            ["te"] = "తప్పు",
            ["kn"] = "ತಪ್ಪು",
            ["ml"] = "തെറ്റ്"
        },
        ["A"] = new Dictionary<string, string> { ["hi"] = "ए", ["bn"] = "এ", ["gu"] = "એ", ["mr"] = "ए", ["ta"] = "ஏ", ["te"] = "ఎ", ["kn"] = "ಎ", ["ml"] = "എ" },
        ["B"] = new Dictionary<string, string> { ["hi"] = "बी", ["bn"] = "বি", ["gu"] = "બી", ["mr"] = "बी", ["ta"] = "பி", ["te"] = "బి", ["kn"] = "ಬಿ", ["ml"] = "ബി" },
        ["C"] = new Dictionary<string, string> { ["hi"] = "सी", ["bn"] = "সি", ["gu"] = "સી", ["mr"] = "सी", ["ta"] = "சி", ["te"] = "సి", ["kn"] = "ಸಿ", ["ml"] = "സി" },
        ["D"] = new Dictionary<string, string> { ["hi"] = "डी", ["bn"] = "ডি", ["gu"] = "ડી", ["mr"] = "डी", ["ta"] = "டி", ["te"] = "డి", ["kn"] = "ಡಿ", ["ml"] = "ഡി" },
        ["Fully Satisfied"] = new Dictionary<string, string> { ["hi"] = "पूर्ण रूप से संतुष्ट", ["bn"] = "সম্পূর্ণভাবে সন্তুষ্ট", ["gu"] = "સંપૂર્ણપણે સંતુષ્ટ", ["mr"] = "संपूर्णपणे समाधान", ["ta"] = "முழுமையாக திருப்தி", ["te"] = "పూర్తిగా సంతృప్తి", ["kn"] = "ಸಂಪೂರ್ಣವಾಗಿ ಸಂತುಷ್ಟ", ["ml"] = "പൂർണമായി സംതൃപ്തി" },
        ["Less Satisfied"] = new Dictionary<string, string> { ["hi"] = "कम संतुष्ट", ["bn"] = "কম সন্তুষ্ট", ["gu"] = "ઓછું સંતુષ્ટ", ["mr"] = "कमी समाधान", ["ta"] = "குறைவர் திருப்தி", ["te"] = "తక్కవ సంతృప్తి", ["kn"] = "ಕಡಿಮ ಸಂತುಷ್ಟ", ["ml"] = "കുറവും സംതൃപ്തി" },
        ["Not Satisfied"] = new Dictionary<string, string> { ["hi"] = "असंतुष्ट", ["bn"] = "অসন্তুষ্ট", ["gu"] = "અસંતુષ્ટ", ["mr"] = "असमाधान", ["ta"] = "திருப்பியில்லை", ["te"] = "సంతృప్తిగా లేదు", ["kn"] = "ಸಂತುಷ್ಟವಿಲ್ಲ", ["ml"] = "സംതൃപ്തിയില്ല" },
        
        // Survey Description Translations
        ["Test your programming knowledge with this basic quiz."] = new Dictionary<string, string>
        {
            ["hi"] = "इस बेसिक क्विज़ के साथ अपना प्रोग्रामिंग ज्ञान का परीक्षण करें।",
            ["bn"] = "এই বেসিক কুইজের সাথে আপনার প্রোগ্রামিং জ্ঞান পরীক্ষা করুন।",
            ["gu"] = "આ બેસિક ક્વિઝ સાથે તમારા પ્રોગ્રામિંગ જ્ઞાનની ચકાસણી કરો।",
            ["mr"] = "या बेसिक क्विझच्या मदतीने तुमचे प्रोग्रामिंग ज्ञान तपासा।",
            ["ta"] = "இந்த அடிப்படை வினாடியுடன் உங்கள் நிரலாக்க அறிவை சோதிக்கவும்.",
            ["te"] = "ఈ బేసిక్ క్విజ్‌తో మీ ప్రోగ్రామింగ్ జ్ఞానాన్ని పరీక్షించండి.",
            ["kn"] = "ಈ ಸರಳ ಪ್ರಶ್ನೆಯೊಂದಿಗೆ ನಿಮ್ಮ ಪ್ರೋಗ್ರಾಮಿಂಗ್ ಜ್ಞಾನವನ್ನು ಪರೀಕ್ಷಿಸಿ.",
            ["ml"] = "ഈ അടിസ്ഥാന ക്വിസുമായി നിങ്ങളുടെ പ്രോഗ്രാമിംഗ് അറിവ് പരിശോധിക്കുക."
        },
        
        // Question Translations
        ["What is your primary programming language?"] = new Dictionary<string, string>
        {
            ["hi"] = "आपकी प्राथमिक प्रोग्रामिंग भाषा क्या है?",
            ["bn"] = "আপনার প্রাথমিক প্রোগ্রামিংগ ভাষা কী?",
            ["gu"] = "તમારી પ્રાથમિક પ્રોગ્રામિંગ ભાષા શું છે?",
            ["mr"] = "तुमची प्राथमिक प्रोग्रामिंग भाषा कोणती आहे?",
            ["ta"] = "உங்களுடைய முதன்மை நிரலாக்க மொழி எது?",
            ["te"] = "మీ ప్రాథమిక ప్రోగ్రామింగ్ భాష ఏమిటి?",
            ["kn"] = "ನಿಮ್ಮ ಪ್ರಾಥಮಿಕ ಪ್ರೋಗ್ರಾಮಿಂಗ್ ಭಾಷೆ ಯಾವುದು?",
            ["ml"] = "നിങ്ങളുടെ പ്രാഥമിക പ്രോഗ്രാമിംഗ് ഭാഷ ഏതാണ്?"
        },
        ["How many years of programming experience do you have?"] = new Dictionary<string, string>
        {
            ["hi"] = "आपके पास कितने साल का प्रोग्रामिंग अनुभव है?",
            ["bn"] = "আপনার কত বছরের প্রোগ্রামিংগ অভিজ্ঞতা আছে?",
            ["gu"] = "તમારી પાસે કેટલા વર્ષનો પ્રોગ્રામિંગ અનુભવ છે?",
            ["mr"] = "तुमच्याजवळ किती वर्षांचा प्रोग्रामिंग अनुभव आहे?",
            ["ta"] = "உங்களுக்கு எத்தனை ஆண்டுகள் நிரலாக்க அனுபவம் உள்ளது?",
            ["te"] = "మీకు ఎన్ని సంవత్సరాల ప్రోగ్రామింగ్ అనుభవం ఉంది?",
            ["kn"] = "ನಿಮಗೆ ಎಷ್ಟು ವರ್ಷಗಳ ಪ್ರೋಗ್ರಾಮಿಂಗ್ ಅನುಭವವಿದೆ?",
            ["ml"] = "നിങ്ങൾക്ക് എത്ര വർഷത്തെ പ്രോഗ്രാമിംഗ് അനുഭവമുണ്ട്?"
        },
        ["Which programming paradigm do you prefer?"] = new Dictionary<string, string>
        {
            ["hi"] = "आप कौन से प्रोग्रामिंग पैराडाइम पसंद करते हैं?",
            ["bn"] = "আপনি কোন প্রোগ্রামিংগ প্যারাডাইম পছন্দ করেন?",
            ["gu"] = "તમે કયા પ્રોગ્રામિંગ પેરાડાઇમ પસંદ કરો છો?",
            ["mr"] = "तुम्ही कोणते प्रोग्रामिंग पॅराडाइम पसंत करता?",
            ["ta"] = "நீங்கள் எந்த நிரலாக்க முறைமையை விரும்புகிறீர்கள்?",
            ["te"] = "మీరు ఏ ప్రోగ్రామింగ్ పారడిగ్మ్ ఇష్టపడతారు?",
            ["kn"] = "ನೀವು ಯಾವ ಪ್ರೋಗ್ರಾಮಿಂಗ್ ಪ್ಯಾರಡಿಗ್ಮ್ ಇಷ್ಟಪಡುತ್ತೀರಿ?",
            ["ml"] = "നിങ്ങൾ ഏത് പ്രോഗ്രാമിംഗ് പാരഡൈമാണ് ഇഷ്ടപ്പെടുന്നത്?"
        },
        
        // Option Translations
        ["JavaScript"] = new Dictionary<string, string> { ["hi"] = "जावास्क्रिप्ट", ["bn"] = "জাভাস্ক্রিপ্ট", ["gu"] = "જાવાસ્ક્રિપ્ટ", ["mr"] = "जावास्क्रिप्ट", ["ta"] = "ஜாவாஸ்கிரிப்ட்", ["te"] = "జావాస్క్రిప్ట్", ["kn"] = "ಜಾವಾಸ್ಕ್ರಿಪ್ಟ್", ["ml"] = "ജാവാസ്ക്രിപ്റ്റ്" },
        ["Python"] = new Dictionary<string, string> { ["hi"] = "पायथन", ["bn"] = "পাইথন", ["gu"] = "પાયથન", ["mr"] = "पायथन", ["ta"] = "பைதாந்", ["te"] = "పైథన్", ["kn"] = "ಪೈಥಾನ್", ["ml"] = "പൈത്തൺ" },
        ["Java"] = new Dictionary<string, string> { ["hi"] = "जावा", ["bn"] = "জাভা", ["gu"] = "જાવા", ["mr"] = "जावा", ["ta"] = "ஜாவா", ["te"] = "జావా", ["kn"] = "ಜಾವಾ", ["ml"] = "ജാവ" },
        ["C#"] = new Dictionary<string, string> { ["hi"] = "सी# (C#)", ["bn"] = "সি# (C#)", ["gu"] = "સી# (C#)", ["mr"] = "सी# (C#)", ["ta"] = "சி# (C#)", ["te"] = "సీ# (C#)", ["kn"] = "ಸೀ# (C#)", ["ml"] = "സി# (C#)" },
        ["C++"] = new Dictionary<string, string> { ["hi"] = "सी++ (C++)", ["bn"] = "সি++ (C++)", ["gu"] = "સી++ (C++)", ["mr"] = "सी++ (C++)", ["ta"] = "சி++ (C++)", ["te"] = "సీ++ (C++)", ["kn"] = "ಸೀ++ (C++)", ["ml"] = "സി++ (C++)" },
        ["Less than 1 year"] = new Dictionary<string, string>
        {
            ["hi"] = "1 साल से कम",
            ["bn"] = "১ বছরের কম",
            ["gu"] = "૧ વર્ષથી ઓછું",
            ["mr"] = "१ वर्षापेक्षा कमी",
            ["ta"] = "1 ஆண்டுகளுக்கும் குறைவாக",
            ["te"] = "1 సంవత్సరం కంటే తక్కువ",
            ["kn"] = "1 ವರ್ಷಕ್ಕಿಂತ ಕಡಿಮೆ",
            ["ml"] = "1 വർഷത്തിൽ താഴെ"
        },
        ["1-3 years"] = new Dictionary<string, string>
        {
            ["hi"] = "1-3 साल",
            ["bn"] = "১-৩ বছর",
            ["gu"] = "૧-૩ વર્ષ",
            ["mr"] = "१-३ वर्षे",
            ["ta"] = "1-3 ஆண்டுகள்",
            ["te"] = "1-3 సంవత్సరాలు",
            ["kn"] = "1-3 ವರ್ಷಗಳು",
            ["ml"] = "1-3 വർഷം"
        },
        ["3-5 years"] = new Dictionary<string, string>
        {
            ["hi"] = "3-5 साल",
            ["bn"] = "৩-৫ বছর",
            ["gu"] = "૩-૫ વર્ષ",
            ["mr"] = "३-५ वर्षे",
            ["ta"] = "3-5 ஆண்டுகள்",
            ["te"] = "3-5 సంవత్సరాలు",
            ["kn"] = "3-5 ವರ್ಷಗಳು",
            ["ml"] = "3-5 വർഷം"
        },
        ["5-10 years"] = new Dictionary<string, string>
        {
            ["hi"] = "5-10 साल",
            ["bn"] = "৫-১০ বছর",
            ["gu"] = "૫-૧૦ વર્ષ",
            ["mr"] = "५-१० वर्षे",
            ["ta"] = "5-10 ஆண்டுகள்",
            ["te"] = "5-10 సంవత్సరాలు",
            ["kn"] = "5-10 ವರ್ಷಗಳು",
            ["ml"] = "5-10 വർഷം"
        },
        ["More than 10 years"] = new Dictionary<string, string>
        {
            ["hi"] = "10 साल से अधिक",
            ["bn"] = "১০ বছরের বেশি",
            ["gu"] = "૧૦ વર્ષથી વધુ",
            ["mr"] = "१० वर्षांपेक्षा जास्त",
            ["ta"] = "10 ஆண்டுகளுக்கும் மேலாக",
            ["te"] = "10 సంవత్సరాల కంటే ఎక్కువ",
            ["kn"] = "10 ವರ್ಷಗಳಿಂತ ಹೆಚ್ಚು",
            ["ml"] = "10 വർഷത്തിൽ മുകളിൽ"
        },
        ["Object-Oriented Programming"] = new Dictionary<string, string>
        {
            ["hi"] = "ऑब्जेक्ट-ओरिएंटेड प्रोग्रामिंग",
            ["bn"] = "অবজেক্ট-ওরিযেন্টেড প্রোগ্রামিংগ",
            ["gu"] = "ઓબ્જેક્ટ-ઓરિએન્ટેડ પ્રોગ્રામિંગ",
            ["mr"] = "ऑब्जेक्ट-ओरिएंटेड प्रोग्रामिंग",
            ["ta"] = "பொருள்-சார்ந்த நிரலாக்கம்",
            ["te"] = "ఆబ్జెక్ట్-ఓరియెంటెడ్ ప్రోగ్రామింగ్",
            ["kn"] = "ಆಬ್ಜೆಕ್ಟ್-ಓರಿಯೆಂಟೆಡ್ ಪ್ರೋಗ್ರಾಮಿಂಗ್",
            ["ml"] = "ഒബ്ജക്റ്റ്-ഓറിയെന്റഡ് പ്രോഗ്രാമിംഗ്"
        },
        ["Functional Programming"] = new Dictionary<string, string>
        {
            ["hi"] = "फंक्शनल प्रोग्रामिंग",
            ["bn"] = "ফাংশনাল প্রোগ্রামিংগ",
            ["gu"] = "ફંક્શનલ પ્રોગ્રામિંગ",
            ["mr"] = "फंक्शनल प्रोग्रामिंग",
            ["ta"] = "செயல்பாட்டு நிரலாக்கம்",
            ["te"] = "ఫంక్షనల్ ప్రోగ్రామింగ్",
            ["kn"] = "ಫಂಕ್ಷನಲ್ ಪ್ರೋಗ್ರಾಮಿಂಗ್",
            ["ml"] = "ഫംക്ഷണൽ പ്രോഗ്രാമിംഗ്"
        },
        ["Procedural Programming"] = new Dictionary<string, string>
        {
            ["hi"] = "प्रोसीजरल प्रोग्रामिंग",
            ["bn"] = "প্রসিডিউরাল প্রোগ্রামিংগ",
            ["gu"] = "પ્રોસીજરલ પ્રોગ્રામિંગ",
            ["mr"] = "प्रोसीजरल प्रोग्रामिंग",
            ["ta"] = "செயல்முறை நிரலாக்கம்",
            ["te"] = "ప్రొసీడరల్ ప్రోగ్రామింగ్",
            ["kn"] = "ವಿಧಾನಾತ್ಮಕ ಪ್ರೋಗ್ರಾಮಿಂಗ್",
            ["ml"] = "പ്രൊസീഡറൽ പ്രോഗ്രാമിംഗ്"
        }
    };
    
    // For single characters and other text, use character mapping
    if (text.Length == 1)
    {
        return text.ToUpper() switch
        {
            "A" => language switch { "hi" => "ए", "bn" => "এ", "gu" => "એ", "mr" => "ए", "ta" => "ஏ", "te" => "ఎ", "kn" => "ಎ", "ml" => "എ", _ => text },
            "B" => language switch { "hi" => "बी", "bn" => "বি", "gu" => "બી", "mr" => "बी", "ta" => "பি", "te" => "బి", "kn" => "ಬి", "ml" => "ബി", _ => text },
            "C" => language switch { "hi" => "सी", "bn" => "সি", "gu" => "સી", "mr" => "सी", "ta" => "சி", "te" => "సి", "kn" => "ಸി", "ml" => "സി", _ => text },
            "D" => language switch { "hi" => "डी", "bn" => "ডি", "gu" => "ડી", "mr" => "डी", "ta" => "டி", "te" => "డి", "kn" => "ഡి", "ml" => "ഡി", _ => text },
            _ => text
        };
    }
    
    // For questions with question marks, clean and translate
    var cleanText = text.Replace("??", "").Replace("?", "").Trim();
    if (translations.ContainsKey(cleanText) && translations[cleanText].ContainsKey(language))
    {
        return translations[cleanText][language] + (text.Contains("??") ? "??" : text.Contains("?") ? "?" : "");
    }
    
    return text; // Return original if no translation
}

// Helper function to get localized question text with auto-translation
string GetLocalizedQuestionText(SurveyWebApp.Models.Question question, string language)
{
    if (language == "en") return question.Text;
    
    // Check if translation exists in database
    var translation = language switch
    {
        "hi" => !string.IsNullOrEmpty(question.QuestionText_Hi) ? question.QuestionText_Hi : null,
        "bn" => !string.IsNullOrEmpty(question.QuestionText_Bn) ? question.QuestionText_Bn : null,
        "gu" => !string.IsNullOrEmpty(question.QuestionText_Gu) ? question.QuestionText_Gu : null,
        "mr" => !string.IsNullOrEmpty(question.QuestionText_Mr) ? question.QuestionText_Mr : null,
        "ta" => !string.IsNullOrEmpty(question.QuestionText_Ta) ? question.QuestionText_Ta : null,
        "te" => !string.IsNullOrEmpty(question.QuestionText_Te) ? question.QuestionText_Te : null,
        "kn" => !string.IsNullOrEmpty(question.QuestionText_Kn) ? question.QuestionText_Kn : null,
        "ml" => !string.IsNullOrEmpty(question.QuestionText_Ml) ? question.QuestionText_Ml : null,
        _ => null
    };
    
    if (translation != null) return translation;
    
    // Auto-translate ANY question text
    return AutoTranslateAnyText(question.Text, language);
}

// Simple auto-translation dictionary for common questions
string AutoTranslate(string text, string language)
{
    var translations = new Dictionary<string, Dictionary<string, string>>
    {
        ["who are You??"] = new Dictionary<string, string>
        {
            ["hi"] = "आप कौन हैं?",
            ["bn"] = "আপনি কে?",
            ["gu"] = "તમે કોણ?",
            ["mr"] = "तुम्ही कोण?",
            ["ta"] = "நீயார்?",
            ["te"] = "నువ్వు ఎవరు?",
            ["kn"] = "ನೀವು ಯಾರು?",
            ["ml"] = "നീയാര്?"
        },
        ["How was the Experience?"] = new Dictionary<string, string>
        {
            ["hi"] = "अनुभव कैसा रहा?",
            ["bn"] = "অভিজ্ঞতা কেমন ছিল?",
            ["gu"] = "અનુભવ કેવો હતો?",
            ["mr"] = "अनुभव कसा होता?",
            ["ta"] = "அனுபவம் எப்படி இருந்தது?",
            ["te"] = "అనుభవం ఎలా ఉంది?",
            ["kn"] = "ಅನುಭವ ಹೇಗಿತ್ತು?",
            ["ml"] = "അനുഭവം എങ്ങനെയായിരുന്നു?"
        },
        ["How was the Product?"] = new Dictionary<string, string>
        {
            ["hi"] = "उत्पाद कैसा था?",
            ["bn"] = "পণ্যটি কেমন ছিল?",
            ["gu"] = "પ્રોડક્ટ કેવો હતો?",
            ["mr"] = "उत्पादन कसे होते?",
            ["ta"] = "தயாரிப்பு எப்படி இருந்தது?",
            ["te"] = "ఉత్పత్తి ఎలా ఉంది?",
            ["kn"] = "ಉತ್ಪನ್ನ ಹೇಗಿತ್ತು?",
            ["ml"] = "ഉൽപ്പന്നം എങ്ങനെയായിരുന്നു?"
        },
        ["How Satisfied are you with your current role?"] = new Dictionary<string, string>
        {
            ["hi"] = "आप अपनी वर्तमान भूमिका से संतुष्ट हैं?",
            ["bn"] = "আপনি আপনার বর্তমান ভূমিকায় সন্তুষ্ট?",
            ["gu"] = "તમે તમારા વર્તમાન ભૂમિકાથી સંતુષ્ટ છો?",
            ["mr"] = "तुम्ही तुमच्या सध्याच्या भूमिकेतून समाधानी आहात?",
            ["ta"] = "நீங்கள் உங்கள் தற்போதைய பாத்திரத்தில் திருப்தியடைந்துள்ளீர்களா?",
            ["te"] = "మీరు మీ ప్రస్తుత పాత్రతో సంతోషంగా ఉన్నారా?",
            ["kn"] = "ನೀವು ನಿಮ್ಮ ಪ್ರಸ್ತುತ ಪಾತ್ರದಿಂದ ಸಂತುಷ್ಟರಾಗಿದ್ದೀರಾ?",
            ["ml"] = "നിങ്ങൾ നിങ്ങളുടെ നിലവിലെ റോളിൽ സംതുഷ്ടരാണോ?"
        },
        ["Rate the Product out of 5?"] = new Dictionary<string, string>
        {
            ["hi"] = "उत्पाद को 5 में से कितना रेट देंगे?",
            ["bn"] = "পণ্যটি 5-এর মধ্যে কত রেটিং দেবেন?",
            ["gu"] = "પ્રોડક્ટને 5 માંથી કેટલી રેટિંગ આપશો?",
            ["mr"] = "उत्पादनाला 5 पैकी किती रेटिंग द्याल?",
            ["ta"] = "தயாரிப்பை 5-க்குள் எவ்வளவு மதிப்பீடு செய்வீர்கள்?",
            ["te"] = "ఉత్పత్తిని 5లో ఎంత రేటింగ్ ఇస్తారు?",
            ["kn"] = "ಉತ್ಪನ್ನವನ್ನು 5 ರಲ್ಲಿ ಎಷ್ಟು ರೇಟಿಂಗ್ ನೀಡುತ್ತೀರಿ?",
            ["ml"] = "ഉൽപ്പന്നത്തിന് 5-ൽ എത്ര റേറ്റിംഗ് നൽകും?"
        },
        ["How often do you Excercise?"] = new Dictionary<string, string>
        {
            ["hi"] = "आप कितनी बार व्यायाम करते हैं?",
            ["bn"] = "আপি কত ঘন ঘন ব্যায়াম করেন?",
            ["gu"] = "તમે કેટલીવાર વ્યાયામ કરો છો?",
            ["mr"] = "तुम्ही किती वेळा व्यायाम करता?",
            ["ta"] = "நீங்கள் எவ்வளவு உடற்பயிற்சி செய்கிறீர்கள்?",
            ["te"] = "మీరు ఎంత తరచుగా వ్యాయాం చేస్తారు?",
            ["kn"] = "ನೀವು ಎಷ್ಟು ಆಗಾಗ್ಗೆ ವ್ಯಾಯಾಮ ಮಾಡುತ್ತೀರಿ?",
            ["ml"] = "നിങ്ങൾ എത്ര തവണ വ്യായാമം ചെയ്യുന്നു?"
        },
        ["What problems do you face with current products?"] = new Dictionary<string, string>
        {
            ["hi"] = "आपको वर्तमान उत्पादों में क्या समस्याएं हैं?",
            ["bn"] = "আপনি বর্তমান পণ্যগুলির সাথে কী সমস্যার সম্মুখীন হন?",
            ["gu"] = "તમને વર્તમાન ઉત્પાદનો સાથે કઈ સમસ્યાઓ છે?",
            ["mr"] = "तुम्हाला सध्याच्या उत्पादनांमध्ये कोणते अडथळे येतात?",
            ["ta"] = "தற்போதைய தயாரிப்புகளுடன் நீங்கள் என்ன சிக்கல்களை எதிர்கொள்கிறீர்கள்?",
            ["te"] = "మీరు ప్రస్తుత ఉత్పత్తులతో ఏమి సమస్యలు ఎదుర్కొంటున్నారు?",
            ["kn"] = "ನೀವು ಪ್ರಸ್ತುತ ಉತ್ಪನ್ನಗಳೊಂದಿಗೆ ಯಾವ ಸಮಸ್ಯೆಗಳನ್ನು ಎದುರಿಸುತ್ತೀರಿ?",
            ["ml"] = "നിങ്ങൾക്ക് നിലവിലെ ഉൽപ്പന്നങ്ങളുമായി എന്ത് പ്രശ്നങ്ങളുണ്ട്?"
        },
        ["Do you feel motivated to give your best at work?"] = new Dictionary<string, string>
        {
            ["hi"] = "क्या आप काम पर अपना सर्वश्रेष्ठ देने के लिए प्रेरित हैं?",
            ["bn"] = "আপনি কাজে আপনার সেরাটা দেওয়ার জন্য অনুপ্রাণিত?",
            ["gu"] = "શું તમે કામ પર તમારા શ્રેષ્ઠ આપવા માટે પ્રેરિત છો?",
            ["mr"] = "तुम्ही कामावर तुमचे सर्वोत्तम देण्यासाठी प्रेरित आहात का?",
            ["ta"] = "நீங்கள் வேலையில் உங்கள் சிறந்ததைக் கொடுப்பதற்கு ஊக்கமளிக்கப்பட்டீர்களா?",
            ["te"] = "మీరు పనిలో మీ ఉత్తమం ఇవ్వడానికి ప్రేరేపించబడ్డారా?",
            ["kn"] = "ನೀವು ಕೆಲಸದಲ್ಲಿ ನಿಮ್ಮ ಉತ್ತಮವನ್ನು ನೀಡಲು ಪ್ರೇರೇಪಿಸಲ್ಪಟ್ಟಿದ್ದೀರಾ?",
            ["ml"] = "ജോലിയിൽ നിങ്ങൾ നിങ്ങളുടെ മികച്ചത് നൽകാൻ പ്രേരിതരാണോ?"
        },
        ["Do you see growth opportunities here?"] = new Dictionary<string, string>
        {
            ["hi"] = "क्या आपको यहां विकास के अवसर दिखते हैं?",
            ["bn"] = "আপনি কি এখানে বৃদ্ধির সুযোগ দেখতে পান?",
            ["gu"] = "શું તમને અહીંયા વિકાસની તકવો દેખાય છે?",
            ["mr"] = "तुम्हाला इथे वाढीची संधी दिसते का?",
            ["ta"] = "நீங்கள் இங்கே வளர்ச்சி வாய்ப்புகளைக் காண்கிறீர்களா?",
            ["te"] = "మీరు ఇక్కడ అభివృద్ధి అవకాశాలు చూస్తున్నారా?",
            ["kn"] = "ನೀವು ಇಲ್ಲಿ ಬೆಳವಣಿಗೆಯ ಅವಕಾಶಗಳನ್ನು ಕಾಣುತ್ತೀರಾ?",
            ["ml"] = "നിങ്ങൾ ഇവിടെ വളർച്ചാ അവസരങ്ങൾ കാണുന്നുണ്ടോ?"
        },
        ["How clear was the teaching?"] = new Dictionary<string, string>
        {
            ["hi"] = "शिक्षण कितना स्पष्ट था?",
            ["bn"] = "শিক্ষাদান কতটা স্পষ্ট ছিল?",
            ["gu"] = "શિક્ષણ કેટલું સ્પષ્ટ હતું?",
            ["mr"] = "शिक्षण किती स्पष्ट होते?",
            ["ta"] = "கற்பித்தல் எவ்வளவு தெளிவாக இருந்தது?",
            ["te"] = "బోధన ఎంత స్పష్టంగా ఉంది?",
            ["kn"] = "ಬೋಧನೆ ಎಷ್ಟು ಸ್ಪಷ್ಟವಾಗಿತ್ತು?",
            ["ml"] = "പഠനം എത്ര വ്യക്തമായിരുന്നു?"
        },
        ["What is your opinion on this topic?"] = new Dictionary<string, string>
        {
            ["hi"] = "इस विषय पर आपकी क्या राय है?",
            ["bn"] = "এই বিষযে আপনার মতামত কী?",
            ["gu"] = "આ વિષય પર તમારું કેટલું મત છે?",
            ["mr"] = "या विषयावर तुमचे मत काय आहे?",
            ["ta"] = "இந்தத் தலைப்பில் உங்கள் எண்ணம் என்ன?",
            ["te"] = "ఈ అంశంపై మీ అభిప్రాయం ఏమిటి?",
            ["kn"] = "ಈ ವಿಷಯದ ಬಗ್ಗೆ ನಿಮ್ಮ ಅಭಿಪ್ರಾಯ ಯಾವುದು?",
            ["ml"] = "ഈ വിഷയത്തിൽ നിങ്ങളുടെ അഭിപ്രായം എന്ത്?"
        },
        ["How easy was the product to use?"] = new Dictionary<string, string>
        {
            ["hi"] = "उत्पाद का उपयोग करना कितना आसान था?",
            ["bn"] = "পণ্যটি ব্যবহার করা কতটা সহজ ছিল?",
            ["gu"] = "પ્રોડક્ટ વાપરવું કેટલું સરળ હતું?",
            ["mr"] = "उत्पादन वापरणे किती सोपे होते?",
            ["ta"] = "தயாரிப்பைப் பயன்படுத்துவது எவ்வளவு சுல஭மாக இருந்தது?",
            ["te"] = "ఉత్పత్తిని ఉపయోగించడం ఎంత సులభంగా ఉంది?",
            ["kn"] = "ಉತ್ಪನ್ನವನ್ನು ಬಳಸಲು ಎಷ್ಟು ಸುಲಭವಾಗಿತ್ತು?",
            ["ml"] = "ഉൽപ്പന്നം ഉപയോഗിക്കുക എത്ര എളുപ്പമായിരുന്നു?"
        },
        ["Was the website/app easy to navigate?"] = new Dictionary<string, string>
        {
            ["hi"] = "वेबसाइट/ऐप नेविगेट करना आसान था?",
            ["bn"] = "ওয়েবসাইট/অ্যাপ নেভিগেট করা সহজ ছিল?",
            ["gu"] = "વેબસાઇટ/એપ નેવિગેટ કરવું સરળ હતું?",
            ["mr"] = "वेबसाइट/अॅप नेव्हिगेट करणे सोपे होते?",
            ["ta"] = "வலைத்தளம்/செயலியை உலாவுவது எளிதாக இருந்ததா?",
            ["te"] = "వెబ్‌సైట్/యాప్‌ను నావిగేట్ చేయడం సులభంగా ఉందా?",
            ["kn"] = "ವೆಬ್‌ಸೈಟ್/ಆ್ಯಪ್ ಅನ್ನು ನ್ಯಾವಿಗೇಟ್ ಮಾಡಲು ಸುಲಭವಾಗಿತ್ತೇ?",
            ["ml"] = "വെബ്സൈറ്റ്/ആപ്പ് നാവിഗേറ്റ് ചെയ്യുക എളുപ്പമായിരുന്നോ?"
        },
        ["How often do you exercise?"] = new Dictionary<string, string>
        {
            ["hi"] = "आप कितनी बार व्यायाम करते हैं?",
            ["bn"] = "আপি কত ঘন ঘন ব্যায়াম করেন?",
            ["gu"] = "તમે કેટલીવાર વ્યાયામ કરો છો?",
            ["mr"] = "तुम्ही किती वेळा व्यायाम करता?",
            ["ta"] = "நீங்கள் எவ்வளவு உடற்பயிற்சி செய்கிறீர்கள்?",
            ["te"] = "మీరు ఎంత తరచుగా వ్యాయాం చేస్తారు?",
            ["kn"] = "ನೀವು ಎಷ್ಟು ಆಗಾಗ್ಗೆ ವ್ಯಾಯಾಮ ಮಾಡುತ್ತೀರಿ?",
            ["ml"] = "നിങ്ങൾ എത്ര തവണ വ്യായാമം ചെയ്യുന്നു?"
        },
        ["What problem do you face most often?"] = new Dictionary<string, string>
        {
            ["hi"] = "आपको सबसे ज्यादा कौन सी समस्या होती है?",
            ["bn"] = "আপনি সবচেয়ে বেশি কোন সমস্যার সম্মুখীন হন?",
            ["gu"] = "તમને સૌથી વધુ કઈ સમસ્યા થાય છે?",
            ["mr"] = "तुम्हाला सर्वाधिक कोणती समस्या येते?",
            ["ta"] = "நீங்கள் அதிகமாக எந்தச் சிக்கலை எதிர்கொள்கிறீர்கள்?",
            ["te"] = "మీరు ఎక్కువగా ఏ సమస్యను ఎదుర్కొంటున్నారు?",
            ["kn"] = "ನೀವು ಅತ್ಯಧಿಕವಾಗಿ ಯಾವ ಸಮಸ್ಯೆಯನ್ನು ಎದುರಿಸುತ್ತೀರಿ?",
            ["ml"] = "നിങ്ങൾക്ക് ഏറ്റവും കൂടുതൽ ഏത് പ്രശ്നമുണ്ട്?"
        },
        ["who is thisss"] = new Dictionary<string, string>
        {
            ["hi"] = "यह कौन है?",
            ["bn"] = "এটা কে?",
            ["gu"] = "આ કોણ?",
            ["mr"] = "हे कोण आहे?",
            ["ta"] = "இது யார்?",
            ["te"] = "ఇది ఎవరు?",
            ["kn"] = "ಇದು ಯಾರು?",
            ["ml"] = "ഇത് ആരാ?"
        }
    };
    
    if (translations.ContainsKey(text) && translations[text].ContainsKey(language))
    {
        return translations[text][language];
    }
    
    // Return original text if no translation found
    return text;
}

// Helper function to get localized option text
string GetLocalizedOptionText(SurveyWebApp.Models.QuestionOption option, string language)
{
    if (language == "en") return option.Text;
    
    // Check if translation exists in database
    var translation = language switch
    {
        "hi" => !string.IsNullOrEmpty(option.OptionText_Hi) ? option.OptionText_Hi : null,
        "bn" => !string.IsNullOrEmpty(option.OptionText_Bn) ? option.OptionText_Bn : null,
        "gu" => !string.IsNullOrEmpty(option.OptionText_Gu) ? option.OptionText_Gu : null,
        "mr" => !string.IsNullOrEmpty(option.OptionText_Mr) ? option.OptionText_Mr : null,
        "ta" => !string.IsNullOrEmpty(option.OptionText_Ta) ? option.OptionText_Ta : null,
        "te" => !string.IsNullOrEmpty(option.OptionText_Te) ? option.OptionText_Te : null,
        "kn" => !string.IsNullOrEmpty(option.OptionText_Kn) ? option.OptionText_Kn : null,
        "ml" => !string.IsNullOrEmpty(option.OptionText_Ml) ? option.OptionText_Ml : null,
        _ => null
    };
    
    if (translation != null) return translation;
    
    // Auto-translate ANY option text
    return AutoTranslateAnyText(option.Text, language);
}

// Simple auto-translation for common options
string AutoTranslateOption(string text, string language)
{
    var optionTranslations = new Dictionary<string, Dictionary<string, string>>
    {
        ["Yes"] = new Dictionary<string, string>
        {
            ["hi"] = "हाँ", ["bn"] = "হ্যাঁ", ["gu"] = "હા", ["mr"] = "होय",
            ["ta"] = "ஆம்", ["te"] = "అవును", ["kn"] = "ಹೌದು", ["ml"] = "അതെ"
        },
        ["No"] = new Dictionary<string, string>
        {
            ["hi"] = "नहीं", ["bn"] = "না", ["gu"] = "ના", ["mr"] = "नाही",
            ["ta"] = "இல்லை", ["te"] = "లేదు", ["kn"] = "ಇಲ್ಲ", ["ml"] = "അല്ല"
        },
        ["True"] = new Dictionary<string, string>
        {
            ["hi"] = "सही", ["bn"] = "সত্যি", ["gu"] = "સાચું", ["mr"] = "खरे",
            ["ta"] = "உண்மை", ["te"] = "నిజం", ["kn"] = "ನಿಜ", ["ml"] = "ശരി"
        },
        ["False"] = new Dictionary<string, string>
        {
            ["hi"] = "गलत", ["bn"] = "মিথ্যা", ["gu"] = "ખોટું", ["mr"] = "खोटे",
            ["ta"] = "தவறு", ["te"] = "తప్పు", ["kn"] = "ತಪ್ಪು", ["ml"] = "തെറ്റ്"
        },
        ["Excellent"] = new Dictionary<string, string>
        {
            ["hi"] = "उत्कृष्ट", ["bn"] = "অসাধারণ", ["gu"] = "ઉત્તમ", ["mr"] = "अतिशय चांगले",
            ["ta"] = "சிறப்பானது", ["te"] = "అద్భుతం", ["kn"] = "ಅತ್ಯುತ್ತಮ", ["ml"] = "അത്യുത്തമം"
        },
        ["Good"] = new Dictionary<string, string>
        {
            ["hi"] = "अच्छा", ["bn"] = "ভালো", ["gu"] = "સારું", ["mr"] = "चांगले",
            ["ta"] = "நல்லது", ["te"] = "మంచి", ["kn"] = "ಉತ್ತಮ", ["ml"] = "നല്ലത്"
        },
        ["Average"] = new Dictionary<string, string>
        {
            ["hi"] = "औसत", ["bn"] = "গড়পড়তা", ["gu"] = "સરેરાશ", ["mr"] = "सरासरी",
            ["ta"] = "சராசரி", ["te"] = "సగటు", ["kn"] = "ಸರಾಸರಿ", ["ml"] = "ശരാശരി"
        },
        ["Poor"] = new Dictionary<string, string>
        {
            ["hi"] = "खराब", ["bn"] = "খারাপ", ["gu"] = "ખરાબ", ["mr"] = "वाईट",
            ["ta"] = "மோசமானது", ["te"] = "చెడు", ["kn"] = "ಕಳಪೆ", ["ml"] = "മോശം"
        },
        ["Hero"] = new Dictionary<string, string>
        {
            ["hi"] = "हीरो", ["bn"] = "হিরো", ["gu"] = "હીરો", ["mr"] = "हीरो",
            ["ta"] = "ஹீரோ", ["te"] = "హీరో", ["kn"] = "ಹೀರೋ", ["ml"] = "ഹീരോ"
        },
        ["Zero"] = new Dictionary<string, string>
        {
            ["hi"] = "शून्य", ["bn"] = "শূন্য", ["gu"] = "શૂન્ય", ["mr"] = "शून्य",
            ["ta"] = "பூஜ்யம்", ["te"] = "సున్న", ["kn"] = "ಶೂನ್ಯ", ["ml"] = "പൂജ്യം"
        },
        ["Superman"] = new Dictionary<string, string>
        {
            ["hi"] = "सुपरमैन", ["bn"] = "সুপারম্যান", ["gu"] = "સુપરમેન", ["mr"] = "सुपरमॅन",
            ["ta"] = "சூப்பர்மேன்", ["te"] = "సూపర్‌మ్యాన్", ["kn"] = "ಸೂಪರ್‌ಮ್ಯಾನ್", ["ml"] = "സൂപ്പർമാൻ"
        },
        ["Batman"] = new Dictionary<string, string>
        {
            ["hi"] = "बैटमैन", ["bn"] = "ব্যাটম্যান", ["gu"] = "બેટમેન", ["mr"] = "बॅटमॅन",
            ["ta"] = "பேட்மேன்", ["te"] = "బాట్‌మ్యాన్", ["kn"] = "ಬ್ಯಾಟ్‌ಮ್ಯಾನ್", ["ml"] = "ബാറ്റ്മാൻ"
        }
    };
    
    if (optionTranslations.ContainsKey(text) && optionTranslations[text].ContainsKey(language))
    {
        return optionTranslations[text][language];
    }
    
    return text; // Return original if no translation
}

// Helper function to get localized UI text
string GetLocalizedUIText(string key, string language)
{
    var uiTranslations = new Dictionary<string, Dictionary<string, string>>
    {
        ["Your Name:"] = new Dictionary<string, string>
        {
            ["hi"] = "आपका नाम:",
            ["bn"] = "আপনার নাম:",
            ["gu"] = "તમારું નામ:",
            ["mr"] = "तुमचे नाव:",
            ["ta"] = "உங்கள் பெயர்:",
            ["te"] = "మీ పేరు:",
            ["kn"] = "ನಿಮ್ಮ ಹೆಸರು:",
            ["ml"] = "നിങ്ങളുടെ പേര്:"
        },
        ["Your State:"] = new Dictionary<string, string>
        {
            ["hi"] = "आपका राज्य:",
            ["bn"] = "আপনার রাজ্য:",
            ["gu"] = "તમારું રાજ્ય:",
            ["mr"] = "तुमचे राज्य:",
            ["ta"] = "உங்கள் மாநிலம்:",
            ["te"] = "మీ రాష్ట్రం:",
            ["kn"] = "ನಿಮ್ಮ ರಾಜ್ಯ:",
            ["ml"] = "നിങ്ങളുടെ സംസ്ഥാനം:"
        },
        ["Enter your full name"] = new Dictionary<string, string>
        {
            ["hi"] = "अपना पूरा नाम दर्ज करें",
            ["bn"] = "আপনার পূর্ণ নাম লিখুন",
            ["gu"] = "તમારું પૂરું નામ દાખલ કરો",
            ["mr"] = "तुमचे संपूर्ण नाव नोंदवा",
            ["ta"] = "உங்கள் முழு பெயரை உள்ளிடவும்",
            ["te"] = "మీ పూర్తి పేరు నమోదు చేయండి",
            ["kn"] = "ನಿಮ್ಮ ಪೂರ್ಣ ಹೆಸರನ್ನು ನಮೂದಿಸಿ",
            ["ml"] = "നിങ്ങളുടെ പൂർണ്ണനാമം നൽകുക"
        },
        ["Select your state"] = new Dictionary<string, string>
        {
            ["hi"] = "अपना राज्य चुनें",
            ["bn"] = "আপনার রাজ্য নির্বাচন করুন",
            ["gu"] = "તમારું રાજ્ય પસંદ કરો",
            ["mr"] = "तुमचे राज्य निवडा",
            ["ta"] = "உங்கள் மாநிலத்தை தேர்ந்தெடுக்கவும்",
            ["te"] = "మీ రాష్ట్రాన్ని ఎంచుకోండి",
            ["kn"] = "ನಿಮ್ಮ ರಾಜ್ಯವನ್ನು ಆಯ್ಕೆಮಾಡಿ",
            ["ml"] = "നിങ്ങളുടെ സംസ്ഥാനം തിരഞ്ഞെടുക്കുക"
        },
        ["Language:"] = new Dictionary<string, string>
        {
            ["hi"] = "भाषा:",
            ["bn"] = "ভাষা:",
            ["gu"] = "ભાષા:",
            ["mr"] = "भाषा:",
            ["ta"] = "மொழி:",
            ["te"] = "భాష:",
            ["kn"] = "ಭಾಷೆ:",
            ["ml"] = "ഭാഷ:"
        },
        ["All Surveys"] = new Dictionary<string, string>
        {
            ["en"] = "All Surveys",
            ["hi"] = "सभी सर्वेक्षण",
            ["bn"] = "সব জরিপ",
            ["gu"] = "બધા સર્વેક્ષણો",
            ["mr"] = "सर्वे सर्वेक्षणे",
            ["ta"] = "அனைத்து கணக்கெடுப்புகள்",
            ["te"] = "అన్ని సర్వేలు",
            ["kn"] = "ಎಲ್ಲಾ ಸರ್ವೇಗಳು",
            ["ml"] = "എല്ലാ സർവേകളും"
        },
        ["Pending"] = new Dictionary<string, string>
        {
            ["en"] = "Pending",
            ["hi"] = "बाकी",
            ["bn"] = "মুলতুবি",
            ["gu"] = "બાકી",
            ["mr"] = "उर्वरित",
            ["ta"] = "நிலுவையில் உள்ளது",
            ["te"] = "పెండింగ్",
            ["kn"] = "ಬಾಕಿ ಇರುವ",
            ["ml"] = "നിലവിലുള്ളത്"
        },
        ["Completed"] = new Dictionary<string, string>
        {
            ["en"] = "Completed",
            ["hi"] = "पूर्ण",
            ["bn"] = "সম্পন্ন",
            ["gu"] = "પૂર્ણ",
            ["mr"] = "पूर्ण",
            ["ta"] = "முடிந்தது",
            ["te"] = "పూర్తి",
            ["kn"] = "ಪೂರ್ಣಗೊಂಡಿದೆ",
            ["ml"] = "പൂർത്തിയായി"
        },
        ["New"] = new Dictionary<string, string>
        {
            ["en"] = "New",
            ["hi"] = "नया",
            ["bn"] = "নতুন",
            ["gu"] = "નવું",
            ["mr"] = "नवीन",
            ["ta"] = "புதிய",
            ["te"] = "కొత్త",
            ["kn"] = "ಹೊಸದು",
            ["ml"] = "പുതിയത്"
        },
        ["Select a survey..."] = new Dictionary<string, string>
        {
            ["en"] = "Select a survey...",
            ["hi"] = "एक सर्वेक्षण चुनें...",
            ["bn"] = "একটি জরিপ নির্বাচন করুন...",
            ["gu"] = "સર્વેક્ષણ પસંદ કરો...",
            ["mr"] = "सर्वेक्षण निवडा...",
            ["ta"] = "ஒரு கணக்கெடுப்பைத் தேர்ந்தெடுக்கவும்...",
            ["te"] = "ఒక సర్వేను ఎంచుకోండి...",
            ["kn"] = "ಒಂದು ಸರ್ವೇಯನ್ನು ಆಯ್ಕೆಮಾಡಿ...",
            ["ml"] = "ഒരു സർവേ തിരഞ്ഞെടുക്കുക..."
        },
        ["Submit Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "सर्वेक्षण जमा करें",
            ["bn"] = "জরিপ জমা দিন",
            ["gu"] = "સર્વેક્ષણ જમા કરો",
            ["mr"] = "सर्वेक्षण जमा करा",
            ["ta"] = "கணக்கெடுப்பை சமர்ப்பிக்கவும்",
            ["te"] = "సర్వేను సమర్పించండి",
            ["kn"] = "ಸರ್ವೇಯನ್ನು ಸಲ್ಲಿಸಿ",
            ["ml"] = "സർവേ സമർപ്പിക്കുക"
        },
        ["Enter your answer here..."] = new Dictionary<string, string>
        {
            ["hi"] = "यहाँ अपना उत्तर लिखें...",
            ["bn"] = "এখানে আপনার উত্তর লিখুন...",
            ["gu"] = "અહીં તમારો જવાબ લખો...",
            ["mr"] = "येथे तुमचे उत्तर लिहा...",
            ["ta"] = "உங்கள் பதிலை இங்கே எழுதவும்...",
            ["te"] = "మీ సమాధానాన్ని ఇక్కడ రాయండి...",
            ["kn"] = "ನಿಮ್ಮ ಉತ್ತರವನ್ನು ಇಲ್ಲಿ ಬರೆಯಿರಿ...",
            ["ml"] = "നിങ്ങളുടെ ഉത്തരം ഇവിടെ എഴുതുക..."
        },
        ["Select rating"] = new Dictionary<string, string>
        {
            ["hi"] = "रेटिंग चुनें",
            ["bn"] = "রেটিং নির্বাচন করুন",
            ["gu"] = "રેટિંગ પસંદ કરો",
            ["mr"] = "रेटिंग निवडा",
            ["ta"] = "மதிப்பீட்டைத் தேர்ந்தெடுக்கவும்",
            ["te"] = "రేటింగ్ ఎంచుకోండి",
            ["kn"] = "ರೇಟಿಂಗ್ ಆಯ್ಕೆಮಾಡಿ",
            ["ml"] = "റേറ്റിംഗ് തിരഞ്ഞെടുക്കുക"
        },
        ["5 - Excellent"] = new Dictionary<string, string>
        {
            ["hi"] = "5 - उत्कृष्ट",
            ["bn"] = "5 - অসাধারণ",
            ["gu"] = "5 - ઉત્તમ",
            ["mr"] = "5 - अतिशय चांगले",
            ["ta"] = "5 - சிறப்பானது",
            ["te"] = "5 - అద్భుతం",
            ["kn"] = "5 - ಅತ್ಯುತ್ತಮ",
            ["ml"] = "5 - അത്യുത്തമം"
        },
        ["4 - Good"] = new Dictionary<string, string>
        {
            ["hi"] = "4 - अच्छा",
            ["bn"] = "4 - ভালো",
            ["gu"] = "4 - સારું",
            ["mr"] = "4 - चांगले",
            ["ta"] = "4 - நல்லது",
            ["te"] = "4 - మంచి",
            ["kn"] = "4 - ಉತ್ತಮ",
            ["ml"] = "4 - നല്ലത്"
        },
        ["3 - Average"] = new Dictionary<string, string>
        {
            ["hi"] = "3 - औसत",
            ["bn"] = "4 - গড়পড়তা",
            ["gu"] = "3 - સરેરાશ",
            ["mr"] = "3 - सरासरी",
            ["ta"] = "3 - சராசரி",
            ["te"] = "3 - సగటు",
            ["kn"] = "3 - ಸರಾಸರಿ",
            ["ml"] = "3 - ശരാശരി"
        },
        ["2 - Poor"] = new Dictionary<string, string>
        {
            ["hi"] = "2 - खराब",
            ["bn"] = "2 - খারাপ",
            ["gu"] = "2 - ખરાબ",
            ["mr"] = "2 - वाईट",
            ["ta"] = "2 - மோசமானது",
            ["te"] = "2 - చెడు",
            ["kn"] = "2 - ಕಳಪೆ",
            ["ml"] = "2 - മോശം"
        },
        ["1 - Very Poor"] = new Dictionary<string, string>
        {
            ["hi"] = "1 - बहुत खराब",
            ["bn"] = "1 - খুবই খারাপ",
            ["gu"] = "1 - ખૂબ ખરાબ",
            ["mr"] = "1 - अत्यंत वाईट",
            ["ta"] = "1 - மிகவும் மோசமானது",
            ["te"] = "1 - చాలా చెడు",
            ["kn"] = "1 - ತುಂಬಾ ಕಳಪೆ",
            ["ml"] = "1 - വളരെ മോശം"
        },
        ["Select an option"] = new Dictionary<string, string>
        {
            ["hi"] = "एक विकल्प चुनें",
            ["bn"] = "একটি বিকল্প নির্বাচন করুন",
            ["gu"] = "એક વિકલ્પ પસંદ કરો",
            ["mr"] = "एक पर्याय निवडा",
            ["ta"] = "ஒரு விருப்பத்தைத் தேர்ந்தெடுக்கவும்",
            ["te"] = "ఒక ఎంపికను ఎంచుకోండి",
            ["kn"] = "ಒಂದು ಆಯ್ಕೆಯನ್ನು ಆಯ್ಕೆಮಾಡಿ",
            ["ml"] = "ഒരു ഓപ്ഷൻ തിരഞ്ഞെടുക്കുക"
        },
        ["Welcome"] = new Dictionary<string, string>
        {
            ["hi"] = "स्वागत है",
            ["bn"] = "স্বাগতম",
            ["gu"] = "સ્વાગત",
            ["mr"] = "स्वागत",
            ["ta"] = "வரவேற்பு",
            ["te"] = "స్వాగతం",
            ["kn"] = "ಸ್ವಾಗತ",
            ["ml"] = "സ്വാഗതം"
        },
        ["Logout"] = new Dictionary<string, string>
        {
            ["hi"] = "लॉगआउट",
            ["bn"] = "লগআউট",
            ["gu"] = "લૉગઆઉટ",
            ["mr"] = "लॉगआउट",
            ["ta"] = "வெளியேறு",
            ["te"] = "లాగ్అవుట్",
            ["kn"] = "ಲಾಗ್‌ಔಟ್",
            ["ml"] = "ലോഗൗട്ട്"
        },
        ["Available Surveys"] = new Dictionary<string, string>
        {
            ["en"] = "List of Surveys",
            ["hi"] = "सर्वेक्षण सूची",
            ["bn"] = "জরিপ তালিকা",
            ["gu"] = "સર્વેક્ષણ સૂચિ",
            ["mr"] = "सर्वेक्षण सूची",
            ["ta"] = "கணக்கெடுப்பு பட்டியல்",
            ["te"] = "సర్వేల జాబితా",
            ["kn"] = "ಸರ್ವೇಗಳ ಪಟ್ಟಿ",
            ["ml"] = "സർവേ പട്ടിക"
        },
        ["Start Survey"] = new Dictionary<string, string>
        {
            ["hi"] = "सर्वेक्षण शुरू करें",
            ["bn"] = "জরিপ শুরু করুন",
            ["gu"] = "સર્વેક્ષણ શરૂ કરો",
            ["mr"] = "सर्वेक्षण सुरू करा",
            ["ta"] = "கணக்கெடுப்பைத் தொடங்கு",
            ["te"] = "సర్వేను ప్రారంభించండి",
            ["kn"] = "ಸರ್ವೇಯನ್ನು ಪ್ರಾರಂಭಿಸಿ",
            ["ml"] = "സർവേ ആരംഭിക്കുക"
        },
        ["No surveys available at the moment."] = new Dictionary<string, string>
        {
            ["hi"] = "वर्तमान में कोई सर्वेक्षण उपलब्ध नहीं है।",
            ["bn"] = "বর্তমানে কোনো জরিপ উপলব্ধ নেই।",
            ["gu"] = "હાલમાં કોઈ સર્વેક્ષણ ઉપલબ્ધ નથી.",
            ["mr"] = "सध्या कोणतेही सर्वेक्षण उपलब्ध नाही.",
            ["ta"] = "தற்போது எந்தக் கணக்கெடுப்பும் கிடைக்கவில்லை.",
            ["te"] = "ప్రస్తుతం ఏ సర్వేలు అందుబాటులో లేవు.",
            ["kn"] = "ಪ್ರಸ್ತುತ ಯಾವುದೇ ಸರ್ವೇಗಳು ಲಭ್ಯವಿಲ್ಲ.",
            ["ml"] = "നിലവിൽ ഒരു സർവേയും ലഭ്യമല്ല."
        },
        ["Please check back later."] = new Dictionary<string, string>
        {
            ["hi"] = "कृपया बाद में फिर से जांच करें।",
            ["bn"] = "দয়া করে পরে আবার চেক করুন।",
            ["gu"] = "મહેરબાની કરીને પછીથી ફરીથી ચકાસો.",
            ["mr"] = "कृपया नंतर पुन्हा तपासा.",
            ["ta"] = "தயவுசெய்து பின்னர் மீண்டும் சரிபார்க்கவும்.",
            ["te"] = "దయచేసి తర్వాత తిరిగి తనిఖీ చేయండి.",
            ["kn"] = "ದಯವಿಟ್ಟು ನಂತರ ಮತ್ತೆ ಪರಿಶೀಲಿಸಿ.",
            ["ml"] = "ദയവായി പിന്നീട് വീണ്ടും പരിശോധിക്കുക."
        },
        ["Search surveys..."] = new Dictionary<string, string>
        {
            ["hi"] = "सर्वेक्षण खोजें...",
            ["bn"] = "জরিপ খুঁজুন...",
            ["gu"] = "સર્વેક્ષણ શોધો...",
            ["mr"] = "सर्वेक्षण शोधा...",
            ["ta"] = "கணக்கெடுப்புகளைத் தேடுங்கள்...",
            ["te"] = "సర్వేలను శోధించండి...",
            ["kn"] = "ಸರ್ವೇಗಳನ್ನು ಹುಡುಕಿ...",
            ["ml"] = "സർവേകൾ തിരയുക..."
        },
        ["Start Voice"] = new Dictionary<string, string>
        {
            ["hi"] = "आवाज शुरू करें",
            ["bn"] = "ভয়েস শুরু করুন",
            ["gu"] = "અવાજ શરૂ કરો",
            ["mr"] = "आवाज सुरू करा",
            ["ta"] = "குரல் தொடங்கு",
            ["te"] = "వాయిస్ ప్రారంభించండి",
            ["kn"] = "ಧ್ವನಿ ಪ್ರಾರಂಭಿಸಿ",
            ["ml"] = "ശബ്ദം ആരംഭിക്കുക"
        },
        ["Stop Voice"] = new Dictionary<string, string>
        {
            ["hi"] = "आवाज रोकें",
            ["bn"] = "ভয়েস বন্ধ করুন",
            ["gu"] = "અવાજ બંધ કરો",
            ["mr"] = "आवाज थांबा",
            ["ta"] = "குரல் நிறுத்து",
            ["te"] = "వాయిస్ ఆపండి",
            ["kn"] = "ಧ್ವನಿ ನಿಲುಪಿಸಿ",
            ["ml"] = "ശബ്ദം നിർത്തുക"
        },
        ["Speak Question"] = new Dictionary<string, string>
        {
            ["hi"] = "प्रश्न सुनाएं",
            ["bn"] = "প্রশ্ন শুনিয়ে দিন",
            ["gu"] = "પ્રશ્ન સાંભળો",
            ["mr"] = "प्रश्न ऐकून द्या",
            ["ta"] = "கேள்வியை சொல்லு",
            ["te"] = "ప్రశ్నను చదవండి",
            ["kn"] = "ಪ್ರಶ್ನವನ್ನು ಓದಿಸಿ",
            ["ml"] = "ചോദ്യം വായിക്കുക"
        },
        ["Voice: Ready"] = new Dictionary<string, string>
        {
            ["hi"] = "आवाज: तैयार",
            ["bn"] = "ভয়েস: প্রস্তুত",
            ["gu"] = "અવાજ: તૈયાર",
            ["mr"] = "आवाज: तयार",
            ["ta"] = "குரல்: தயார்",
            ["te"] = "వాయిస్: సిద్ధం",
            ["kn"] = "ಧ್ವನಿ: ಸಿದ್ಧ",
            ["ml"] = "ശബ്ദം: തയ്യാറായി"
        },
        ["Voice Not Supported"] = new Dictionary<string, string>
        {
            ["hi"] = "आवाज समर्थित नहीं",
            ["bn"] = "ভয়েস সমর্থিত নয়",
            ["gu"] = "અવાજ સમર્થિત નથી",
            ["mr"] = "आवाज समर्थित नाही",
            ["ta"] = "குரல் ஆதரவில்லை",
            ["te"] = "వాయిస్ మద్దత్ లేదు",
            ["kn"] = "ಧ್ವನಿ ಬೆಂಬಲಿಸುವುದಿಲ್ಲ",
            ["ml"] = "ശബ്ദം പിന്തുണിക്കുന്നില്ല"
        }
    };
    
    if (uiTranslations.ContainsKey(key) && uiTranslations[key].ContainsKey(language))
    {
        return uiTranslations[key][language];
    }
    
    return key; // Return original if no translation
}

// Login page
app.MapGet("/login", (HttpContext http) =>
{
    var error = http.Request.Query["error"].ToString();
    var errorMessage = "";
    
    if (error == "1")
    {
        errorMessage = "Invalid username or password. Please try again.";
    }

    // Get language preference from query parameter or session
    var language = http.Request.Query["lang"].ToString();
    if (string.IsNullOrEmpty(language))
        language = http.Session.GetString("Language") ?? "en";
    else
        http.Session.SetString("Language", language); // Save to session

    return Results.Content($@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Login - Survey System</title>
<link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css' rel='stylesheet'>
<style>
html, body {{
    background: linear-gradient(135deg, #001A33 0%, #00205B 100%);
    color:#fff;
    min-height:100vh;
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    display: flex;
    align-items: center;
    justify-content: center;
    max-width: 100vw;
    overflow-x: hidden;
    width: 100%;
    position: relative;
}}
* {{
    box-sizing: border-box;
}}
.auth-card {{
    background: rgba(255,255,255,0.1);
    backdrop-filter: blur(20px);
    border-radius: 20px;
    padding: 40px;
    box-shadow: 0 20px 40px rgba(0,0,0,0.2);
    border: 1px solid rgba(255,255,255,0.2);
    width: 100%;
    max-width: 400px;
}}
.auth-card h3 {{
    text-align: center;
    margin-bottom: 30px;
    font-weight: 700;
}}
.form-control {{
    background: rgba(255,255,255,0.1);
    border: 1px solid rgba(255,255,255,0.3);
    color: white;
    margin-bottom: 15px;
}}
.form-control::placeholder {{
    color: rgba(255,255,255,0.7);
}}
.btn-auth {{
    background: linear-gradient(135deg, #00205B 0%, #001A4D 100%);
    border: 2px solid #003366;
    color: white;
    padding: 12px 30px;
    border-radius: 8px;
    width: 100%;
    font-weight: 600;
    cursor: pointer;
    transition: all 0.3s ease;
}}
.btn-auth:hover {{
    background: linear-gradient(135deg, #003366 0%, #002652 100%);
    transform: translateY(-2px);
}}
.link-text {{
    text-align: center;
    margin-top: 20px;
}}
.link-text a {{
    color: #4a9eff;
    text-decoration: none;
}}
.link-text a:hover {{
    text-decoration: underline;
}}
.auth-card .logo {{
    height: 100px !important;
    margin: 10px !important;
    max-width: 180px !important;
}}

/* Mobile logo adjustments */
@media (max-width: 768px) {{
    .auth-card .logo {{
        height: 90px !important;
        max-width: 160px !important;
    }}
}}

@media (max-width: 480px) {{
    .auth-card .logo {{
        height: 80px !important;
        max-width: 140px !important;
        margin: 8px !important;
    }}
}}
</style>
</head>
<body>
<div class='auth-card'>
    <div class='text-center mb-4'>
        <img src='/assets/apptad-technologies-pvt-ltd-original-removebg-preview.png' class='logo' style='max-width: 180px; height: auto;'>
    </div>
    <h3>{GetLocalizedUIText("Login", language)}</h3>
    {(string.IsNullOrEmpty(errorMessage) ? "" : $@"<div class='alert alert-danger'>{errorMessage}</div>")}
    <form method='post' action='/login'>
        <div class='mb-3'>
            <input type='text' class='form-control' name='username' placeholder='{GetLocalizedUIText("Username", language)}' required>
        </div>
        <div class='mb-3'>
            <input type='password' class='form-control' name='password' placeholder='{GetLocalizedUIText("Password", language)}' required>
        </div>
        <button type='submit' class='btn btn-auth w-100'>{GetLocalizedUIText("Login", language)}</button>
    </form>
    <div class='link-text'>
        {GetLocalizedUIText("Don't have an account?", language)} <a href='/signup'>{GetLocalizedUIText("Sign Up", language)}</a>
    </div>
</div>
</body>
</html>", "text/html");
});

// Login post
app.MapPost("/login", async (HttpContext http, ApplicationDbContext db) =>
{
    var form = await http.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        return Results.Redirect("/login?error=1");
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    
    if (user != null)
    {
        var hashedPassword = HashPassword(password);
        if (user.PasswordHash == hashedPassword)
        {
            http.Session.SetString("UserId", user.Id.ToString());
            http.Session.SetString("Username", user.Username);
            return Results.Redirect("/");
        }
    }

    return Results.Redirect("/login?error=1");
});

// Signup page
app.MapGet("/signup", (HttpContext http) =>
{
    var error = http.Request.Query["error"].ToString();
    var errorMessage = "";
    
    if (error == "1")
    {
        errorMessage = "Please fill in all fields and ensure passwords match.";
    }
    else if (error == "2")
    {
        errorMessage = "Username already exists. Please choose a different username.";
    }

    return Results.Content($@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Sign Up - Survey System</title>
<link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css' rel='stylesheet'>
<style>
html, body {{
    background: linear-gradient(135deg, #001A33 0%, #00205B 100%);
    color:#fff;
    min-height:100vh;
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
    display: flex;
    align-items: center;
    justify-content: center;
    max-width: 100vw;
    overflow-x: hidden;
    width: 100%;
    position: relative;
}}
* {{
    box-sizing: border-box;
}}
.auth-card {{
    background: rgba(255,255,255,0.1);
    backdrop-filter: blur(20px);
    border-radius: 20px;
    padding: 40px;
    box-shadow: 0 20px 40px rgba(0,0,0,0.3);
    border: 1px solid rgba(255,255,255,0.2);
    max-width: 400px;
    width: 100%;
}}
.auth-card h3 {{
    text-align: center;
    margin-bottom: 30px;
    font-weight: 700;
}}
.form-control {{
    background: rgba(255,255,255,0.1);
    border: 1px solid rgba(255,255,255,0.3);
    color: white;
    margin-bottom: 15px;
}}
.form-control::placeholder {{
    color: rgba(255,255,255,0.7);
}}
.btn-auth {{
    background: linear-gradient(135deg, #00205B 0%, #001A4D 100%);
    border: 2px solid #003366;
    color: white;
    padding: 12px 30px;
    border-radius: 8px;
    width: 100%;
    font-weight: 600;
    transition: all 0.3s ease;
}}
.btn-auth:hover {{
    background: linear-gradient(135deg, #003366 0%, #002652 100%);
    transform: translateY(-2px);
}}
.link-text {{
    text-align: center;
    margin-top: 20px;
}}
.link-text a {{
    color: #4a9eff;
    text-decoration: none;
}}
.link-text a:hover {{
    text-decoration: underline;
}}
</style>
</head>
<body>
<div class='auth-card'>
    <h3>Sign Up</h3>
    {(string.IsNullOrEmpty(errorMessage) ? "" : $@"<div class='alert alert-danger'>{errorMessage}</div>")}
    <form method='post' action='/signup'>
        <div class='mb-3'>
            <label for='username' class='form-label'>Username</label>
            <input type='text' class='form-control' id='username' name='username' required 
                   placeholder='Choose a username'>
        </div>
        <div class='mb-3'>
            <label for='password' class='form-label'>Password</label>
            <input type='password' class='form-control' id='password' name='password' required 
                   placeholder='Choose a password'>
        </div>
        <div class='mb-3'>
            <label for='confirmPassword' class='form-label'>Confirm Password</label>
            <input type='password' class='form-control' id='confirmPassword' name='confirmPassword' required 
                   placeholder='Confirm your password'>
        </div>
        <button type='submit' class='btn btn-auth'>Sign Up</button>
    </form>
    <div class='link-text'>
        <p>Already have an account? <a href='/login'>Login</a></p>
    </div>
</div>
</body>
</html>", "text/html");
});

// Signup post
app.MapPost("/signup", async (HttpContext http, ApplicationDbContext db) =>
{
    var form = await http.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();

    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || password != confirmPassword)
    {
        return Results.Redirect("/signup?error=1");
    }

    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (existingUser != null)
    {
        return Results.Redirect("/signup?error=2");
    }

    var newUser = new UserModel
    {
        Username = username,
        PasswordHash = HashPassword(password),
        IsActive = true
    };

    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    http.Session.SetString("UserId", newUser.Id.ToString());
    http.Session.SetString("Username", newUser.Username);
    
    return Results.Redirect("/");
});

// Logout
app.MapGet("/logout", (HttpContext http) =>
{
    http.Session.Clear();
    return Results.Redirect("/login");
});

// ================= HOME =================
app.MapGet("/", async (HttpContext http, ApplicationDbContext db) =>
{
    // Check if user is authenticated
    var userId = http.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/login");
    }

    var surveys = await db.Surveys
        .Where(s => s.IsActive && s.PublishedAt.HasValue && s.PublishedAt <= DateTime.UtcNow)
        .ToListAsync();

    // Get language preference from query parameter or session
    var language = http.Request.Query["lang"].ToString();
    if (string.IsNullOrEmpty(language))
        language = http.Session.GetString("Language") ?? "en";
    else
        http.Session.SetString("Language", language); // Save to session

    var sb = new StringBuilder();
    sb.Append($@"
<!DOCTYPE html>
<html lang='{language}'>
<head>
<meta charset='utf-8'>
<meta http-equiv='Content-Type' content='text/html; charset=UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Surveys</title>
<link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css' rel='stylesheet'>
<style>
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+Devanagari:wght@400;500;600;700&family=Noto+Sans+Bengali:wght@400;500;600;700&family=Noto+Sans+Gujarati:wght@400;500;600;700&family=Noto+Sans+Gurmukhi:wght@400;500;600;700&family=Noto+Sans+Tamil:wght@400;500;600;700&family=Noto+Sans+Telugu:wght@400;500;600;700&family=Noto+Sans+Kannada:wght@400;500;600;700&family=Noto+Sans+Malayalam:wght@400;500;600;700&family=Noto+Sans:wght@400;500;600;700&display=swap');
html, body {{
    background: linear-gradient(135deg, #001A33 0%, #00205B 100%);
    color:#fff;
    min-height:100vh;
    font-family: {(language == "hi" ? "'Noto Sans Devanagari', 'Segoe UI'" : language == "bn" ? "'Noto Sans Bengali', 'Segoe UI'" : language == "gu" ? "'Noto Sans Gujarati', 'Segoe UI'" : language == "mr" ? "'Noto Sans Gurmukhi', 'Segoe UI'" : language == "ta" ? "'Noto Sans Tamil', 'Segoe UI'" : language == "te" ? "'Noto Sans Telugu', 'Segoe UI'" : language == "kn" ? "'Noto Sans Kannada', 'Segoe UI'" : language == "ml" ? "'Noto Sans Malayalam', 'Segoe UI'" : "'Segoe UI'")}, Tahoma, Geneva, Verdana, sans-serif;
    overflow-x: hidden;
    position: relative;
    max-width: 100vw;
    width: 100%;
}}
* {{
    box-sizing: border-box;
}}
body::before {{
    content: '';
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-image: 
        radial-gradient(circle at 20% 80%, rgba(74, 158, 255, 0.3) 0%, transparent 50%),
        radial-gradient(circle at 80% 20%, rgba(168, 208, 255, 0.3) 0%, transparent 50%),
        radial-gradient(circle at 40% 40%, rgba(255, 255, 255, 0.1) 0%, transparent 50%);
    animation: bgFloat 20s ease-in-out infinite;
    pointer-events: none;
    z-index: -1;
}}
@keyframes bgFloat {{
    0%, 100% {{ transform: translate(0, 0) rotate(0deg); }}
    33% {{ transform: translate(-20px, -20px) rotate(1deg); }}
    66% {{ transform: translate(20px, -10px) rotate(-1deg); }}
}}
.particles {{
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    pointer-events: none;
    z-index: -1;
}}
.particle {{
    position: absolute;
    background: rgba(255, 255, 255, 0.5);
    border-radius: 50%;
    animation: particleFloat 15s infinite;
}}
.particle:nth-child(1) {{ width: 4px; height: 4px; left: 10%; animation-delay: 0s; animation-duration: 12s; }}
.particle:nth-child(2) {{ width: 3px; height: 3px; left: 30%; animation-delay: 2s; animation-duration: 15s; }}
.particle:nth-child(3) {{ width: 5px; height: 5px; left: 50%; animation-delay: 4s; animation-duration: 10s; }}
.particle:nth-child(4) {{ width: 2px; height: 2px; left: 70%; animation-delay: 6s; animation-duration: 18s; }}
.particle:nth-child(5) {{ width: 3px; height: 3px; left: 90%; animation-delay: 8s; animation-duration: 14s; }}
.particle:nth-child(6) {{ width: 4px; height: 4px; left: 20%; animation-delay: 10s; animation-duration: 16s; }}
.particle:nth-child(7) {{ width: 2px; height: 2px; left: 40%; animation-delay: 12s; animation-duration: 13s; }}
.particle:nth-child(8) {{ width: 5px; height: 5px; left: 60%; animation-delay: 14s; animation-duration: 11s; }}
.particle:nth-child(9) {{ width: 3px; height: 3px; left: 80%; animation-delay: 16s; animation-duration: 17s; }}
.particle:nth-child(10) {{ width: 4px; height: 4px; left: 25%; animation-delay: 18s; animation-duration: 19s; }}
@keyframes particleFloat {{
    0% {{ transform: translateY(100vh) translateX(0); opacity: 0; }}
    10% {{ opacity: 1; }}
    90% {{ opacity: 1; }}
    100% {{ transform: translateY(-100vh) translateX(100px); opacity: 0; }}
}}
.logo {{
    height:120px;
    margin: 20px;
    animation: float 6s ease-in-out infinite;
}}
.logo:nth-child(2) {{
    animation-delay: 3s;
}}
@keyframes float {{
    0%, 100% {{ transform: translateY(0px); }}
    50% {{ transform: translateY(-20px); }}
}}
.survey-card {{
    background: rgba(255,255,255,0.1);
    backdrop-filter: blur(20px);
    border-radius: 20px;
    padding: 30px;
    margin: 20px;
    border: 1px solid rgba(255,255,255,0.2);
    transition: all 0.3s ease;
    position: relative;
    overflow: hidden;
    height: 280px;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
}}
.survey-card::before {{
    content: '';
    position: absolute;
    top: 0;
    left: -100%;
    width: 100%;
    height: 100%;
    background: linear-gradient(90deg, transparent, rgba(255,255,255,0.1), transparent);
    transition: left 0.5s;
}}
.survey-card:hover::before {{
    left: 100%;
}}
.survey-card:hover {{
    transform: translateY(-5px) scale(1.02);
    box-shadow: 0 25px 50px rgba(0,0,0,0.3);
    border-color: rgba(255,255,255,0.3);
}}
.survey-card h5 {{
    color: #fff;
    font-weight: 600;
    font-size: 1.3rem;
    margin-bottom: 15px;
    flex-grow: 0;
}}
.survey-card p {{
    color: rgba(255,255,255,0.8);
    font-size: 0.95rem;
    line-height: 1.6;
    flex-grow: 1;
    margin-bottom: 20px;
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
}}
.survey-card .btn {{
    background: linear-gradient(135deg, #00205B 0%, #001A4D 100%);
    border: 2px solid #003366;
    color: white;
    padding: 12px 30px;
    border-radius: 8px;
    font-weight: 600;
    transition: all 0.3s ease;
    position: relative;
    overflow: hidden;
    text-decoration: none;
    display: inline-block;
    width: 100%;
    text-align: center;
    min-width: 140px;
    box-sizing: border-box;
}}
.survey-card .btn::before {{
    content: '';
    position: absolute;
    top: 0;
    left: -100%;
    width: 100%;
    height: 100%;
    background: linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent);
    transition: left 0.5s;
}}
.survey-card .btn:hover::before {{
    left: 100%;
}}
.survey-card .btn:hover {{
    background: linear-gradient(135deg, #003366 0%, #002652 100%);
    transform: translateY(-2px);
    box-shadow: 0 10px 30px rgba(74,158,255,0.4);
    color: white;
}}
.logout-btn {{
    background: rgba(255,255,255,0.2);
    border: 1px solid rgba(255,255,255,0.3);
    color: white;
    padding: 10px 25px;
    border-radius: 25px;
    text-decoration: none;
    transition: all 0.3s ease;
    backdrop-filter: blur(10px);
    font-weight: 500;
}}
.logout-btn:hover {{
    background: rgba(255,255,255,0.3);
    transform: translateY(-2px);
    box-shadow: 0 10px 25px rgba(255,255,255,0.2);
    color: white;
}}
.welcome-section {{
    text-align: center;
    margin-bottom: 50px;
    position: relative;
}}
.welcome-section h4 {{
    font-size: 2rem;
    font-weight: 700;
    background: linear-gradient(135deg, #ffffff, #a8d0ff);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    margin-bottom: 20px;
    animation: glow 2s ease-in-out infinite alternate;
}}
@keyframes glow {{
    from {{ text-shadow: 0 0 10px rgba(168,208,255,0.5); }}
    to {{ text-shadow: 0 0 20px rgba(168,208,255,0.8), 0 0 30px rgba(168,208,255,0.6); }}
}}
.main-title {{
    font-size: 3rem;
    font-weight: 800;
    text-align: center;
    margin-bottom: 60px;
    background: linear-gradient(135deg, #ffffff, #4a9eff);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    animation: slideInDown 1s ease-out;
}}

/* Mobile Responsive Styles */
@media (max-width: 768px) {{
    body {{
        background: linear-gradient(135deg, #001A33 0%, #00205B 100%) !important;
        color: #fff !important;
        font-size: 16px;
    }}
    
    html {{
        background: linear-gradient(135deg, #001A33 0%, #00205B 100%) !important;
    }}
    
    .container {{
        padding: 15px !important;
        max-width: 100%;
        background: transparent !important;
    }}
    
    .logo {{
        height: 80px !important;
        margin: 10px !important;
    }}
    
    .welcome-section {{
        margin-bottom: 30px !important;
        text-align: center;
    }}
    
    .welcome-section h4 {{
        font-size: 1.5rem !important;
        margin-bottom: 15px !important;
        line-height: 1.2;
    }}
    
    .logout-btn {{
        padding: 8px 20px !important;
        font-size: 0.9rem !important;
        margin-top: 10px;
    }}
    
    .main-title {{
        font-size: 2rem !important;
        margin-bottom: 40px !important;
        line-height: 1.1;
    }}
    
    .survey-grid {{
        grid-template-columns: 1fr !important;
        gap: 15px !important;
    }}
    
    .survey-card {{
        margin: 10px 0 !important;
        padding: 20px !important;
        height: auto !important;
        min-height: 250px;
        border-radius: 15px !important;
    }}
    
    .survey-card h5 {{
        font-size: 1.1rem !important;
        margin-bottom: 10px !important;
    }}
    
    .survey-card p {{
        font-size: 0.9rem !important;
        margin-bottom: 15px !important;
        -webkit-line-clamp: 2 !important;
    }}
    
    .survey-card .btn {{
        padding: 10px 20px !important;
        font-size: 0.9rem !important;
        min-width: 120px !important;
    }}
    
    .d-inline-flex {{
        flex-direction: column !important;
        align-items: center !important;
        gap: 10px !important;
    }}
    
    .form-label {{
        margin-bottom: 5px !important;
        font-size: 0.9rem !important;
    }}
    
    .form-control-sm {{
        width: 100% !important;
        max-width: 200px !important;
        font-size: 0.9rem !important;
    }}
    
    /* Fix whitish background issue */
    .survey-card, .logout-btn, .btn {{
        background: rgba(0, 26, 51, 0.9) !important;
        backdrop-filter: blur(10px) !important;
        -webkit-backdrop-filter: blur(10px) !important;
    }}
    
    .survey-card {{
        border: 1px solid rgba(74, 158, 255, 0.3) !important;
    }}
    
    /* Ensure text remains visible */
    .survey-card h5, .survey-card p, .survey-card .btn, .logout-btn {{
        color: #ffffff !important;
        text-shadow: 0 1px 2px rgba(0, 0, 0, 0.5) !important;
    }}
}}

@media (max-width: 480px) {{
    html {{
        background: linear-gradient(135deg, #001A33 0%, #00205B 100%) !important;
    }}
    body {{
        background: linear-gradient(135deg, #001A33 0%, #00205B 100%) !important;
        color: #fff !important;
    }}
    .container {{
        padding: 10px !important;
        background: transparent !important;
    }}
    
    .logo {{
        height: 60px !important;
        margin: 5px !important;
    }}
    
    .welcome-section h4 {{
        font-size: 1.3rem !important;
    }}
    
    .main-title {{
        font-size: 1.8rem !important;
        margin-bottom: 30px !important;
    }}
    
    .survey-card {{
        padding: 15px !important;
        min-height: 220px;
    }}
    
    .survey-card h5 {{
        font-size: 1rem !important;
    }}
    
    .survey-card p {{
        font-size: 0.85rem !important;
    }}
    
    .survey-card .btn {{
        padding: 8px 15px !important;
        font-size: 0.85rem !important;
    }}
    
    .logout-btn {{
        padding: 6px 15px !important;
        font-size: 0.85rem !important;
    }}
}}
.survey-grid {{
    animation: fadeInUp 1s ease-out;
}}
@keyframes fadeInUp {{
    from {{ opacity: 0; transform: translateY(30px); }}
    to {{ opacity: 1; transform: translateY(0); }}
}}
</style>
</head>
<body>
<div class='particles'>
    <div class='particle'></div>
    <div class='particle'></div>
    <div class='particle'></div>
    <div class='particle'></div>
    <div class='particle'></div>
    <div class='particle'></div>
    <div class='particle'></div>
    <div class='particle'></div>
    <div class='particle'></div>
    <div class='particle'></div>
</div>");
    
    // Mobile HTML generated by MobileHtmlGenerator
    var mobileHtml = SurveyWebApp.MobileHtmlGenerator.GenerateMobileHtml(language, http.Session.GetString("Username"), surveys);
    sb.Append(mobileHtml);

return Results.Content(sb.ToString(), "text/html; charset=utf-8");
});

// ================= ALL SURVEYS PAGE =================
app.MapGet("/surveys", async (HttpContext http, ApplicationDbContext db) =>
{
    // Check if user is authenticated
    var userId = http.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/login");
    }

    var surveys = await db.Surveys
        .Where(s => s.IsActive && s.PublishedAt.HasValue && s.PublishedAt <= DateTime.UtcNow)
        .ToListAsync();

    // Get language preference from query parameter or session
    var language = http.Request.Query["lang"].ToString();
    if (string.IsNullOrEmpty(language))
        language = http.Session.GetString("Language") ?? "en";
    else
        http.Session.SetString("Language", language);

    // Generate HTML using SurveysHtmlGenerator
    var surveysHtml = SurveyWebApp.SurveysHtmlGenerator.GenerateSurveysHtml(language, http.Session.GetString("Username"), surveys);
    return Results.Content(surveysHtml, "text/html; charset=utf-8");
});


// ================= SURVEY PAGE =================
app.MapGet("/survey/{id:int}", async (int id, HttpContext http, ApplicationDbContext db) =>
{
    // Check if user is authenticated
    var userId = http.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/login");
    }

    var survey = await db.Surveys
        .Include(s => s.Questions)
        .ThenInclude(q => q.Options)
        .FirstOrDefaultAsync(s => s.Id == id);

    if (survey == null) return Results.Redirect("/");
    
    // DEBUG: Add debug info
    Console.WriteLine($"DEBUG: Survey {id} found with {survey.Questions.Count} questions");
    foreach (var q in survey.Questions)
    {
        Console.WriteLine($"DEBUG: Question {q.Id}: {q.Text} ({q.Type}) with {q.Options.Count} options");
    }
    
    // Get language preference from query parameter or session
    var language = http.Request.Query["lang"].ToString();
    if (string.IsNullOrEmpty(language))
        language = http.Session.GetString("Language") ?? "en";
    else
        http.Session.SetString("Language", language); // Save to session
    
    // Get localized title
    var localizedTitle = GetLocalizedTitle(survey, language);

    var sb = new StringBuilder();
    sb.Append($@"
<!DOCTYPE html>
<html lang='{language}'>
<head>
<meta charset='utf-8'>
<meta http-equiv='Content-Type' content='text/html; charset=UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>{System.Net.WebUtility.HtmlEncode(localizedTitle)}</title>
<link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css' rel='stylesheet'>
<link href='/css/voice-controls.css' rel='stylesheet'>
<link href='/css/mobile.css?v={DateTime.Now.Ticks}' rel='stylesheet'>
<style>
@import url('https://fonts.googleapis.com/css2?family=Noto+Sans+Devanagari:wght@400;500;600;700&family=Noto+Sans:wght@400;500;600;700&display=swap');

html, body {{
    background: linear-gradient(135deg, #001A33 0%, #00205B 100%);
    color:#fff;
    min-height:100vh;
    font-family: 'Noto Sans Devanagari', 'Noto Sans', 'Segoe UI', 'Arial Unicode MS', 'Lucida Sans Unicode', 'Arial', sans-serif;
    max-width: 100vw;
    overflow-x: hidden;
    width: 100%;
}}
* {{
    box-sizing: border-box;
}}

* {{
    font-family: inherit;
}}

h1, h2, h3, h4, h5, h6 {{
    font-family: 'Noto Sans Devanagari', 'Noto Sans', 'Segoe UI', 'Arial Unicode MS', 'Lucida Sans Unicode', 'Arial', sans-serif;
}}

strong, b {{
    font-family: 'Noto Sans Devanagari', 'Noto Sans', 'Segoe UI', 'Arial Unicode MS', 'Lucida Sans Unicode', 'Arial', sans-serif;
    letter-spacing: normal;
    word-spacing: normal;
}}

.card-glass {{
    background: rgba(255,255,255,0.1);
    backdrop-filter: blur(20px);
    border-radius: 20px;
    padding: 30px;
    margin: 20px 0;
    border: 1px solid rgba(255,255,255,0.2);
    line-height: 1.6;
}}
.logo {{
    height:100px;
    margin: 20px;
    animation: float 6s ease-in-out infinite;
}}
.logo:nth-child(2) {{
    animation-delay: 3s;
}}
@keyframes float {{
    0%, 100% {{ transform: translateY(0px); }}
    50% {{ transform: translateY(-20px); }}
}}
.card-glass {{
    background: rgba(255,255,255,0.1);
    backdrop-filter: blur(20px);
    border-radius: 20px;
    padding: 30px;
    margin: 20px 0;
    border: 1px solid rgba(255,255,255,0.2);
}}
.card-glass strong {{
    color: #fff;
    font-weight: 600;
}}
.card-glass .form-label {{
    color: #fff;
    font-weight: 500;
}}
.form-control {{
    background: rgba(255,255,255,0.1);
    border: 1px solid rgba(255,255,255,0.3);
    color: white;
}}
.form-control::placeholder {{
    color: rgba(255,255,255,0.7);
}}
.form-check-input {{
    background-color: rgba(255,255,255,0.9);
    border: 2px solid rgba(255,255,255,0.4);
}}
.form-check-input:checked {{
    background-color:#4a9eff;
    border-color:#4a9eff;
}}
.form-check-label {{
    color:#fff;
    margin-left: 8px;
}}
.submit-btn {{
    background: linear-gradient(135deg, #00205B 0%, #001A4D 100%);
    border: 2px solid #003366;
    color: white;
    padding: 15px 30px;
    border-radius: 8px;
    font-weight: 600;
    transition: all 0.3s ease;
}}
.submit-btn:hover {{
    background: linear-gradient(135deg, #003366 0%, #002652 100%);
    transform: translateY(-2px);
}}

/* Mobile Responsive Styles for Survey Page */
@media (max-width: 768px) {{
    body {{
        background: linear-gradient(135deg, #001A33 0%, #00205B 100%) !important;
        color: #fff !important;
        font-size: 16px;
    }}
    
    .container {{
        padding: 15px !important;
        max-width: 100%;
    }}
    
    .logo {{
        height: 80px !important;
        margin: 10px !important;
    }}
    
    h3 {{
        font-size: 1.8rem !important;
        margin-bottom: 30px !important;
        line-height: 1.2;
    }}
    
    .card-glass {{
        padding: 20px !important;
        margin: 15px 0 !important;
        border-radius: 15px !important;
    }}
    
    .form-label {{
        font-size: 0.9rem !important;
        margin-bottom: 8px !important;
    }}
    
    .form-control {{
        padding: 12px !important;
        font-size: 0.9rem !important;
        background: rgba(0, 26, 51, 0.9) !important;
        backdrop-filter: blur(10px) !important;
        -webkit-backdrop-filter: blur(10px) !important;
    }}
    
    .form-control::placeholder {{
        color: rgba(255, 255, 255, 0.7) !important;
    }}
    
    .form-check-label {{
        font-size: 0.9rem !important;
        margin-left: 6px !important;
    }}
    
    .submit-btn {{
        padding: 12px 25px !important;
        font-size: 0.9rem !important;
        width: 100% !important;
        background: rgba(0, 32, 77, 0.9) !important;
        backdrop-filter: blur(10px) !important;
        -webkit-backdrop-filter: blur(10px) !important;
    }}
    
    .d-inline-flex {{
        flex-direction: column !important;
        align-items: center !important;
        gap: 10px !important;
    }}
    
    /* Ensure text visibility */
    .card-glass, .form-control, .submit-btn {{
        color: #ffffff !important;
        text-shadow: 0 1px 2px rgba(0, 0, 0, 0.5) !important;
    }}
    
    .form-check-input {{
        background-color: rgba(255, 255, 255, 0.9) !important;
        border: 2px solid rgba(255, 255, 255, 0.4) !important;
    }}
    
    .form-check-input:checked {{
        background-color: #4a9eff !important;
        border-color: #4a9eff !important;
    }}
}}

@media (max-width: 480px) {{
    .container {{
        padding: 10px !important;
    }}
    
    .logo {{
        height: 60px !important;
        margin: 5px !important;
    }}
    
    h3 {{
        font-size: 1.5rem !important;
        margin-bottom: 25px !important;
    }}
    
    .card-glass {{
        padding: 15px !important;
        margin: 10px 0 !important;
    }}
    
    .form-control {{
        padding: 10px !important;
        font-size: 0.85rem !important;
    }}
    
    .form-label {{
        font-size: 0.85rem !important;
    }}
    
    .submit-btn {{
        padding: 10px 20px !important;
        font-size: 0.85rem !important;
    }}
    
    .form-check-label {{
        font-size: 0.85rem !important;
    }}
}}
</style>
</head>
<body>
<div class='container py-4'>
    <div class='text-center mb-4'>
        <img src='/assets/apptad-technologies-pvt-ltd-original-removebg-preview.png' class='logo'>
    </div>

    <h3 class='text-center mb-4'>{System.Net.WebUtility.HtmlEncode(localizedTitle)}</h3>

    <!-- Language Selector -->
    <div class='text-center mb-3'>
        <div class='d-inline-flex'>
            <label class='form-label me-2'>Language:</label>
            <select id='languageSelector' class='form-control form-control-sm' style='width: auto;' onchange='changeLanguage()'>
                <option value='en' {(language == "en" ? "selected" : "")}>English</option>
                <option value='hi' {(language == "hi" ? "selected" : "")}>हिन्दी (Hindi)</option>
                <option value='bn' {(language == "bn" ? "selected" : "")}>বাংলা (Bengali)</option>
                <option value='gu' {(language == "gu" ? "selected" : "")}>ગુજરાતી (Gujarati)</option>
                <option value='mr' {(language == "mr" ? "selected" : "")}>मराठी (Marathi)</option>
                <option value='ta' {(language == "ta" ? "selected" : "")}>தமிழ் (Tamil)</option>
                <option value='te' {(language == "te" ? "selected" : "")}>తెలుగు (Telugu)</option>
                <option value='kn' {(language == "kn" ? "selected" : "")}>ಕನ್ನಡ (Kannada)</option>
                <option value='ml' {(language == "ml" ? "selected" : "")}>മലയാളം (Malayalam)</option>
            </select>
        </div>
    </div>

    <form method='post' action='/survey/{id}/submit'>
");

    // Add name field
    sb.Append($@"
<div class='card-glass'>
    <label for='userName' class='form-label'><strong>{GetLocalizedUIText("Your Name:", language)}</strong></label>
    <input type='text' id='userName' name='userName' class='form-control' required 
           placeholder='{GetLocalizedUIText("Enter your full name", language)}'>
</div>");

    // Add state field
    sb.Append($@"
<div class='card-glass'>
    <label for='userState' class='form-label'><strong>{GetLocalizedUIText("Your State:", language)}</strong></label>
    <select id='userState' name='userState' class='form-control' required>
        <option value=''>{GetLocalizedUIText("Select your state", language)}</option>
        <option value='Andhra Pradesh'>Andhra Pradesh</option>
        <option value='Arunachal Pradesh'>Arunachal Pradesh</option>
        <option value='Assam'>Assam</option>
        <option value='Bihar'>Bihar</option>
        <option value='Chhattisgarh'>Chhattisgarh</option>
        <option value='Goa'>Goa</option>
        <option value='Gujarat'>Gujarat</option>
        <option value='Haryana'>Haryana</option>
        <option value='Himachal Pradesh'>Himachal Pradesh</option>
        <option value='Jharkhand'>Jharkhand</option>
        <option value='Karnataka'>Karnataka</option>
        <option value='Kerala'>Kerala</option>
        <option value='Madhya Pradesh'>Madhya Pradesh</option>
        <option value='Maharashtra'>Maharashtra</option>
        <option value='Manipur'>Manipur</option>
        <option value='Meghalaya'>Meghalaya</option>
        <option value='Mizoram'>Mizoram</option>
        <option value='Nagaland'>Nagaland</option>
        <option value='Odisha'>Odisha</option>
        <option value='Punjab'>Punjab</option>
        <option value='Rajasthan'>Rajasthan</option>
        <option value='Sikkim'>Sikkim</option>
        <option value='Tamil Nadu'>Tamil Nadu</option>
        <option value='Telangana'>Telangana</option>
        <option value='Tripura'>Tripura</option>
        <option value='Uttar Pradesh'>Uttar Pradesh</option>
        <option value='Uttarakhand'>Uttarakhand</option>
        <option value='West Bengal'>West Bengal</option>
        <option value='Jammu and Kashmir'>Jammu and Kashmir</option>
        <option value='Ladakh'>Ladakh</option>
        <option value='Lakshadweep'>Lakshadweep</option>
        <option value='Puducherry'>Puducherry</option>
    </select>
</div>");

    int i = 1;
    foreach (var q in survey.Questions.OrderBy(q => q.Order))
    {
        var localizedQuestionText = GetLocalizedQuestionText(q, language);
        sb.Append($"<div class='card-glass'><strong>{i}. {System.Net.WebUtility.HtmlEncode(localizedQuestionText)}</strong>");
        
        // Handle different question types
        switch (q.Type)
        {
            case QuestionType.Descriptive:
                sb.Append($@"
<div class='mt-3'>
    <textarea name='q{q.Id}' class='form-control' rows='4' placeholder='{GetLocalizedUIText("Enter your answer here...", language)}' {(q.IsRequired ? "required" : "")}></textarea>
</div>");
                break;
                
            case QuestionType.LongText:
                sb.Append($@"
<div class='mt-3'>
    <textarea name='q{q.Id}' class='form-control' rows='6' placeholder='Enter your detailed answer here...' {(q.IsRequired ? "required" : "")}></textarea>
</div>");
                break;
                
            case QuestionType.SingleChoice:
                foreach (var o in q.Options)
                {
                    sb.Append($@"
<div class='form-check mt-2'>
<input class='form-check-input' type='radio' name='q{q.Id}' value='{o.Id}' {(q.IsRequired ? "required" : "")}>
<label class='form-check-label'>{System.Net.WebUtility.HtmlEncode(GetLocalizedOptionText(o, language))}</label>
</div>");
                }
                break;
                
            case QuestionType.MultipleChoice:
                foreach (var o in q.Options)
                {
                    sb.Append($@"
<div class='form-check mt-2'>
<input class='form-check-input' type='checkbox' name='q{q.Id}_{o.Id}' value='{o.Id}'>
<label class='form-check-label'>{System.Net.WebUtility.HtmlEncode(GetLocalizedOptionText(o, language))}</label>
</div>");
                }
                break;
                
            case QuestionType.TrueFalse:
                sb.Append($@"
<div class='form-check mt-2'>
<input class='form-check-input' type='radio' name='q{q.Id}' value='true' {(q.IsRequired ? "required" : "")}>
<label class='form-check-label'>True</label>
</div>
<div class='form-check mt-2'>
<input class='form-check-input' type='radio' name='q{q.Id}' value='false'>
<label class='form-check-label'>False</label>
</div>");
                break;
                
            case QuestionType.Rating:
                sb.Append($@"
<div class='mt-3'>
    <select name='q{q.Id}' class='form-control' {(q.IsRequired ? "required" : "")}>
        <option value=''>{GetLocalizedUIText("Select rating", language)}</option>
        <option value='5'>{GetLocalizedUIText("5 - Excellent", language)}</option>
        <option value='4'>{GetLocalizedUIText("4 - Good", language)}</option>
        <option value='3'>{GetLocalizedUIText("3 - Average", language)}</option>
        <option value='2'>{GetLocalizedUIText("2 - Poor", language)}</option>
        <option value='1'>{GetLocalizedUIText("1 - Very Poor", language)}</option>
    </select>
</div>");
                break;
                
            case QuestionType.Scale:
                sb.Append($@"
<div class='mt-3'>
    <select name='q{q.Id}' class='form-control' {(q.IsRequired ? "required" : "")}>
        <option value=''>Select rating</option>
        <option value='10'>10 - Excellent</option>
        <option value='9'>9 - Very Good</option>
        <option value='8'>8 - Good</option>
        <option value='7'>7 - Above Average</option>
        <option value='6'>6 - Average</option>
        <option value='5'>5 - Below Average</option>
        <option value='4'>4 - Poor</option>
        <option value='3'>3 - Very Poor</option>
        <option value='2'>2 - Extremely Poor</option>
        <option value='1'>1 - Worst</option>
    </select>
</div>");
                break;
                
            case QuestionType.Dropdown:
                sb.Append($@"
<div class='mt-3'>
    <select name='q{q.Id}' class='form-control' {(q.IsRequired ? "required" : "")}>
        <option value=''>{GetLocalizedUIText("Select an option", language)}</option>");
                foreach (var o in q.Options)
                {
                    sb.Append($"<option value='{o.Id}'>{System.Net.WebUtility.HtmlEncode(o.Text)}</option>");
                }
                sb.Append("</select></div>");
                break;
                
            default:
                // Default to radio buttons for unknown types
                foreach (var o in q.Options)
                {
                    sb.Append($@"
<div class='form-check mt-2'>
<input class='form-check-input' type='radio' name='q{q.Id}' value='{o.Id}' {(q.IsRequired ? "required" : "")}>
<label class='form-check-label'>{System.Net.WebUtility.HtmlEncode(GetLocalizedOptionText(o, language))}</label>
</div>");
                }
                break;
        }
        
        sb.Append("</div>");
        i++;
    }

    sb.Append($@"
<button class='submit-btn'>{GetLocalizedUIText("Submit Survey", language)}</button>
</form>

<!-- Voice Recognition Controls -->
<div class='voice-controls'>
    <div class='voice-panel'>
        <button id='voiceToggle' class='voice-btn' onclick='toggleVoiceRecognition()'>
            🎤 {GetLocalizedUIText("Start Voice", language)}
        </button>
        <button id='speakQuestion' class='voice-btn' onclick='speakCurrentQuestion()'>
            🔊 {GetLocalizedUIText("Speak Question", language)}
        </button>
        <div id='voiceStatus' class='voice-status'>
            {GetLocalizedUIText("Voice: Ready", language)}
        </div>
    </div>
</div>

<script src='/js/voice-recognition-final.js?v=2'></script>
<script>
// Initialize voice service when page loads
function initVoiceService() {{
    if (typeof VoiceRecognitionService !== 'undefined' && !window.voiceService) {{
        window.voiceService = new VoiceRecognitionService();
        window.voiceService.setLanguage('{language}');
        
        // Set up status callback
        window.voiceService.onStatusChanged = function(status) {{
            const statusEl = document.getElementById('voiceStatus');
            if (statusEl) {{
                statusEl.textContent = '{GetLocalizedUIText("Voice", language)}: ' + status;
            }}
        }};
        
        // Check browser support
        const toggleBtn = document.getElementById('voiceToggle');
        if (!window.voiceService.isSupported()) {{
            if (toggleBtn) {{
                toggleBtn.disabled = true;
                toggleBtn.textContent = '{GetLocalizedUIText("Voice Not Supported", language)}';
            }}
        }}
    }}
}}

// Try initialization immediately
initVoiceService();

// Also try after DOM is ready
if (document.readyState === 'loading') {{
    document.addEventListener('DOMContentLoaded', initVoiceService);
}} else {{
    initVoiceService();
}}

// Fallback initialization
setTimeout(initVoiceService, 500);

// Global functions for button clicks
function toggleVoiceRecognition() {{
    if (window.voiceService) {{
        if (window.voiceService.isListening) {{
            window.voiceService.stopListening();
        }} else {{
            window.voiceService.startListening();
        }}
    }}
}}

function speakCurrentQuestion() {{
    if (window.voiceService && window.voiceService.speakCurrentQuestion) {{
        window.voiceService.speakCurrentQuestion();
    }} else {{
        console.log('Voice service not ready or speakCurrentQuestion not available');
    }}
}}

// Keyboard shortcuts
document.addEventListener('keydown', function(e) {{
    if (e.ctrlKey && e.key === 'v') {{
        e.preventDefault();
        toggleVoiceRecognition();
    }} else if (e.ctrlKey && e.key === 's') {{
        e.preventDefault();
        speakCurrentQuestion();
    }}
}});
</script>

</div>

<script>
function changeLanguage() {{
    var selector = document.getElementById('languageSelector');
    var selectedLang = selector.value;
    var currentUrl = window.location.href;
    var url = new URL(currentUrl);
    url.searchParams.set('lang', selectedLang);
    window.location.href = url.toString();
}}
</script>

</body>
</html>");

    return Results.Content(sb.ToString(), "text/html; charset=utf-8");
});

// ================= SUBMIT =================
app.MapPost("/survey/{id:int}/submit", async (int id, HttpContext http, ApplicationDbContext db) =>
{
    // Check if user is authenticated
    var userId = http.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/login");
    }

    var form = await http.Request.ReadFormAsync();
    
    // Extract user name and state from form
    var userName = form["userName"].ToString() ?? "";
    var userState = form["userState"].ToString() ?? "";

    using var conn = db.Database.GetDbConnection();
    await conn.OpenAsync();

    using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO Responses (SurveyId, SubmittedAt, State, UserName)
        VALUES (@SurveyId, @SubmittedAt, @State, @UserName);
        SELECT SCOPE_IDENTITY();";

    var p1 = cmd.CreateParameter(); p1.ParameterName = "@SurveyId"; p1.Value = id;
    var p2 = cmd.CreateParameter(); p2.ParameterName = "@SubmittedAt"; p2.Value = DateTime.UtcNow;
    var p3 = cmd.CreateParameter(); p3.ParameterName = "@State"; p3.Value = userState;
    var p4 = cmd.CreateParameter(); p4.ParameterName = "@UserName"; p4.Value = userName;

    cmd.Parameters.Add(p1);
    cmd.Parameters.Add(p2);
    cmd.Parameters.Add(p3);
    cmd.Parameters.Add(p4);
    
    var responseId = Convert.ToInt32(cmd.ExecuteScalar());

    // Save answers
    var survey = await db.Surveys
        .Include(s => s.Questions)
        .FirstOrDefaultAsync(s => s.Id == id);
        
    foreach (var question in survey.Questions.OrderBy(q => q.Order))
    {
        string formKey = $"q{question.Id}";
        var answerValue = form[formKey].ToString();
        
        if (!string.IsNullOrEmpty(answerValue))
        {
            using var answerCmd = conn.CreateCommand();
            
            // Handle different question types
            if (question.Type == QuestionType.Descriptive || question.Type == QuestionType.LongText)
            {
                // Save text answers
                answerCmd.CommandText = @"
                    INSERT INTO ResponseAnswers (ResponseId, QuestionId, AnswerText)
                    VALUES (@ResponseId, @QuestionId, @AnswerText)";

                var a1 = answerCmd.CreateParameter(); a1.ParameterName = "@ResponseId"; a1.Value = responseId;
                var a2 = answerCmd.CreateParameter(); a2.ParameterName = "@QuestionId"; a2.Value = question.Id;
                var a3 = answerCmd.CreateParameter(); a3.ParameterName = "@AnswerText"; a3.Value = answerValue;

                answerCmd.Parameters.Add(a1);
                answerCmd.Parameters.Add(a2);
                answerCmd.Parameters.Add(a3);
            }
            else if (question.Type == QuestionType.TrueFalse || question.Type == QuestionType.Rating)
            {
                // Save scale/rating values
                answerCmd.CommandText = @"
                    INSERT INTO ResponseAnswers (ResponseId, QuestionId, ScaleValue)
                    VALUES (@ResponseId, @QuestionId, @ScaleValue)";

                var a1 = answerCmd.CreateParameter(); a1.ParameterName = "@ResponseId"; a1.Value = responseId;
                var a2 = answerCmd.CreateParameter(); a2.ParameterName = "@QuestionId"; a2.Value = question.Id;
                var a3 = answerCmd.CreateParameter(); a3.ParameterName = "@ScaleValue"; a3.Value = int.Parse(answerValue);

                answerCmd.Parameters.Add(a1);
                answerCmd.Parameters.Add(a2);
                answerCmd.Parameters.Add(a3);
            }
            else if (question.Type == QuestionType.MultipleChoice)
            {
                // Handle multiple choice checkboxes
                foreach (var key in form.Keys)
                {
                    if (key.StartsWith($"q{question.Id}_") && int.TryParse(key.Substring(key.IndexOf('_') + 1), out int optionId))
                    {
                        using var multiCmd = conn.CreateCommand();
                        multiCmd.CommandText = @"
                            INSERT INTO ResponseAnswers (ResponseId, QuestionId, AnswerText)
                            VALUES (@ResponseId, @QuestionId, @AnswerText)";

                        var m1 = multiCmd.CreateParameter(); m1.ParameterName = "@ResponseId"; m1.Value = responseId;
                        var m2 = multiCmd.CreateParameter(); m2.ParameterName = "@QuestionId"; m2.Value = question.Id;
                        var m3 = multiCmd.CreateParameter(); m3.ParameterName = "@AnswerText"; m3.Value = optionId.ToString();

                        multiCmd.Parameters.Add(m1);
                        multiCmd.Parameters.Add(m2);
                        multiCmd.Parameters.Add(m3);
                        
                        await multiCmd.ExecuteNonQueryAsync();
                    }
                }
                continue; // Skip the single execution for multiple choice
            }
            else
            {
                // Save option-based answers (SingleChoice, Dropdown)
                answerCmd.CommandText = @"
                    INSERT INTO ResponseAnswers (ResponseId, QuestionId, AnswerText)
                    VALUES (@ResponseId, @QuestionId, @AnswerText)";

                var a1 = answerCmd.CreateParameter(); a1.ParameterName = "@ResponseId"; a1.Value = responseId;
                var a2 = answerCmd.CreateParameter(); a2.ParameterName = "@QuestionId"; a2.Value = question.Id;
                var a3 = answerCmd.CreateParameter(); a3.ParameterName = "@AnswerText"; a3.Value = int.Parse(answerValue);

                answerCmd.Parameters.Add(a1);
                answerCmd.Parameters.Add(a2);
                answerCmd.Parameters.Add(a3);
            }
            
            await answerCmd.ExecuteNonQueryAsync();
        }
    }

    return Results.Redirect("/thankyou");
});

app.MapGet("/thankyou", () =>
{
    return Results.Content(@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Thank You</title>
<link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css' rel='stylesheet'>
<style>
html, body {
    background: linear-gradient(135deg, #001A33 0%, #00205B 100%);
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    font-family: 'Segoe UI', sans-serif;
    color: white;
    max-width: 100vw;
    overflow-x: hidden;
    width: 100%;
}
* {
    box-sizing: border-box;
}
.card {
    background: rgba(255,255,255,0.1);
    backdrop-filter: blur(22px) saturate(180%);
    border-radius: 26px;
    padding: 56px;
    max-width: 540px;
    width: 100%;
    text-align: center;
    box-shadow: 0 32px 64px rgba(0,0,0,0.4);
    border: 1px solid rgba(255,255,255,0.15);
}
.check {
    width: 90px;
    height: 90px;
    margin: 0 auto 26px;
    border-radius: 50%;
    background: linear-gradient(135deg, #4aff9e, #22c55e);
    display: flex;
    align-items: center;
    justify-content: center;
}
.check svg {
    width: 42px;
    height: 42px;
    fill: #0b1a12;
}
h1 {
    font-size: 42px;
    font-weight: 800;
    background: linear-gradient(135deg, #ffffff, #a8d0ff);
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
    margin-bottom: 12px;
}
p {
    color: rgba(255,255,255,0.85);
    font-size: 18px;
    margin-bottom: 26px;
}
.btn {
    background: linear-gradient(135deg, #00205B 0%, #001A4D 100%);
    border: 2px solid #003366;
    color: white;
    padding: 16px 32px;
    border-radius: 14px;
    font-weight: 700;
    font-size: 16px;
    letter-spacing: 1px;
    text-transform: uppercase;
    box-shadow: 0 12px 30px rgba(74,158,255,.45);
    cursor: pointer;
    transition: .3s;
    text-decoration: none;
    display: inline-block;
}
.btn:hover {
    transform: translateY(-3px);
    box-shadow: 0 20px 44px rgba(74,158,255,.6);
    color: white;
}
</style>
</head>
<body>
<div class='card'>
    <div class='check'>
        <svg viewBox='0 0 24 24'>
            <path d='M9 16.2l-3.5-3.5L4 14.2l5 5 11-11-1.5-1.5z'/>
        </svg>
    </div>
    <h1>Thank you!</h1>
    <p>Your response has been submitted successfully.</p>
    <a href='/' class='btn'>Back to Home</a>
</div>
</body>
</html>", "text/html");
});

// ================= TRANSLATION ENDPOINT =================
app.MapPost("/api/translate-all", async (HttpContext http, ApplicationDbContext db, WebTranslationService translationService) =>
{
    try
    {
        await translationService.GenerateAllTranslationsAsync();
        
        return Results.Json(new { 
            success = true, 
            message = "🌐 All survey data translated successfully for WebApp!",
            languages = new[] { "hi", "bn", "gu", "mr", "ta", "te", "kn", "ml" }
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { 
            success = false, 
            message = $"❌ Translation failed: {ex.Message}" 
        });
    }
});

// ================= TEST TRANSLATION ENDPOINT =================
app.MapGet("/test-translations", async (ApplicationDbContext db) =>
{
    var surveys = await db.Surveys
        .Include(s => s.Questions)
        .ThenInclude(q => q.Options)
        .Take(3)
        .ToListAsync();

    var html = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Translation Test</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .survey { margin: 20px 0; padding: 15px; border: 1px solid #ccc; }
        .question { margin: 10px 0; padding: 10px; background: #f5f5f5; }
        .option { margin: 5px 0; padding: 5px; margin-left: 20px; }
        .translation { color: #0066cc; font-weight: bold; }
    </style>
</head>
<body>
    <h1>🌐 Translation Test - WebApp</h1>";

    foreach (var survey in surveys)
    {
        html += $@"
    <div class='survey'>
        <h2>Survey: {survey.Title}</h2>
        <div class='translation'>Hindi: {survey.Title_Hi}</div>
        <div class='translation'>Bengali: {survey.Title_Bn}</div>";

        foreach (var question in survey.Questions.Take(2))
        {
            html += $@"
        <div class='question'>
            <strong>Q: {question.Text}</strong>
            <div class='translation'>Hindi: {question.QuestionText_Hi}</div>
            <div class='translation'>Bengali: {question.QuestionText_Bn}</div>";

            foreach (var option in question.Options.Take(2))
            {
                html += $@"
            <div class='option'>
                - {option.Text}
                <div class='translation'>Hindi: {option.OptionText_Hi}</div>
                <div class='translation'>Bengali: {option.OptionText_Bn}</div>
            </div>";
            }

            html += "</div>";
        }

        html += "</div>";
    }

    html += @"
</body>
</html>";

    return Results.Content(html, "text/html; charset=utf-8");
});

// ================= FEATURE PAGES =================

app.MapGet("/secure-private", async (HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/login");
    }

    var language = http.Session.GetString("Language") ?? "en";
    var username = http.Session.GetString("Username");
    
    var html = SurveyWebApp.MobileHtmlGenerator.GenerateSecurePrivateHtml(language, username);
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapGet("/quick-easy", async (HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/login");
    }

    var language = http.Session.GetString("Language") ?? "en";
    var username = http.Session.GetString("Username");
    
    var html = SurveyWebApp.MobileHtmlGenerator.GenerateQuickEasyHtml(language, username);
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapGet("/rewards-program", async (HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/login");
    }

    var language = http.Session.GetString("Language") ?? "en";
    var username = http.Session.GetString("Username");
    
    var html = SurveyWebApp.MobileHtmlGenerator.GenerateRewardsProgramHtml(language, username);
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapGet("/average-time", async (HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/login");
    }

    var language = http.Session.GetString("Language") ?? "en";
    var username = http.Session.GetString("Username");
    
    var html = SurveyWebApp.MobileHtmlGenerator.GenerateAverageTimeHtml(language, username);
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapGet("/security-platform", async (HttpContext http) =>
{
    var userId = http.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Redirect("/login");
    }

    var language = http.Session.GetString("Language") ?? "en";
    var username = http.Session.GetString("Username");
    
    var html = SurveyWebApp.MobileHtmlGenerator.GenerateSecurityPlatformHtml(language, username);
    return Results.Content(html, "text/html; charset=utf-8");
});

app.Run();
