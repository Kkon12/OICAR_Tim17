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
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
        {
            
            OnRedirectToLogin = ctx =>
            {
                
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

// ── MVC
builder.Services.AddControllersWithViews(options =>
{
    
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// Flash poruke
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// MiddleWare
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

