namespace SmartQueueApp.Services
{
    public class LanguageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public const string CookieName = "sq_lang";

        public LanguageService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string CurrentLanguage
        {
            get
            {
                var cookie = _httpContextAccessor.HttpContext?
                    .Request.Cookies[CookieName];
                return cookie == "hr" ? "hr" : "en";
            }
        }

        public bool IsHr => CurrentLanguage == "hr";

        // ── Generic translator ────────────────────────────────────────────────
        public string T(string en, string hr)
            => IsHr ? hr : en;
    }
}