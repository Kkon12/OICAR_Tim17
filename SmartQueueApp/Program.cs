using Microsoft.AspNetCore.Authentication.Cookies;
using SmartQueueApp.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ── Croatian Culture ──────────────────────────────────────────────────────────
var croatianCulture = new CultureInfo("hr-HR");
CultureInfo.DefaultThreadCurrentCulture = croatianCulture;
CultureInfo.DefaultThreadCurrentUICulture = croatianCulture;

// ── HttpClient → SmartQueueAPI ────────────────────────────────────────────────
builder.Services.AddHttpClient("SmartQueueAPI", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiSettings:BaseUrl"]
        ?? "http://localhost:5179/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<LanguageService>();
builder.Services.AddHttpContextAccessor();

// ── Cookie Authentication ─────────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Denied";
        options.Cookie.Name = "SmartQueue.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax; // ← was Strict, caused redirect loop
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
        {
            // Prevent redirect loop — return 401 for API calls 
            OnRedirectToLogin = ctx =>
            {
                // Only redirect browser requests, not API/ajax
                if (!ctx.Request.Path.StartsWithSegments("/api"))
                    ctx.Response.Redirect(ctx.RedirectUri);
                else
                    ctx.Response.StatusCode = 401;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(); 

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews(options =>
{
    // Global antiforgery on all POST/PUT/PATCH/DELETE
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// ── Session (for flash messages) ──────────────────────────────────────────────
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();




/*
 * Why HttpOnly cookie: JavaScript cannot read or steal the token
 * — eliminates the most common XSS attack vector against JWT auth.


Why SameSite = Strict: Prevents CSRF attacks — the cookie is only sent on same-site requests,
* malicious cross-site forms cannot use it.

Why AutoValidateAntiforgeryToken globally:
* Every POST/PATCH/DELETE form must include a valid antiforgery token
* — protects all forms automatically without needing [ValidateAntiForgeryToken] on every action.

Why 8-hour session: Matches a full working shift — Djelatnik doesn't get logged out mid-day.

Why AddSession: Used for flash messages 
* — success/error notifications that survive a redirect (Post-Redirect-Get pattern).*/