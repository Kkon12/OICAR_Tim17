using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartQueue.Core.Data;
using SmartQueue.Core.Models;
using SmartQueueAPI;
using SmartQueueAPI.Helpers;
using System.Text;
using System.Threading.RateLimiting;
using SmartQueue.Core.Interfaces;
using SmartQueueAPI.Services;
using SmartQueueAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── JWT ───────────────────────────────────────────────────────────────────────
// IMPORTANT: Must come AFTER AddIdentity to override its default schemes
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey))
    };
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
// Protects POST api/ticket/take from being flooded.
// A single IP can take at most 10 tickets per minute.
// SlidingWindow prevents boundary bursts — someone firing 10 at 0:59 and 10
// at 1:01 would normally bypass a fixed window but not a sliding one.
// Applied via [EnableRateLimiting("kiosk")] on the TakeTicket action only.
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("kiosk", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.PermitLimit = 10;
        limiterOptions.SegmentsPerWindow = 6;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    // Return 429 with a readable JSON message instead of an empty response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"message\":\"Too many requests. Please wait before taking another ticket.\"}",
            cancellationToken);
    };
});

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Estimation Service ────────────────────────────────────────────────────────
builder.Services.AddScoped<IEstimationService, EstimationService>();

/*Why AddScoped: A new instance of EstimationService is created per HTTP request.
 * This is correct because it uses AppDbContext which- 
 * is also scoped — they share the same DB connection within a request.*/

// ── Controllers + Swagger ─────────────────────────────────────────────────────

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Output dates as "dd/MM/yyyy HH:mm:ss" in JSON responses
        options.JsonSerializerOptions.Converters.Add(
            new CroatianDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(
           new CroatianNullableDateTimeConverter());
    });

/*This ensures every API response formats DateTime as Croatian format automatically 
 * — no need to format manually in each controller.*/

//ZA NULLABEDATETIMECONVERTER
/*Without registering both, nullable DateTime fields like CalledAt? would still output in default ISO format.*/

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "SmartQueue API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });
    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Seed Data ─────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(userManager, roleManager, context);
}

/*Why pass AppDbContext to seeder: The seeder now creates Queues and Counters directly in the database — 
 * it needs the DbContext to do that. Previously it only used Identity (UserManager/RoleManager) so context wasn't needed.*/

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Rate limiter must be AFTER auth and BEFORE MapControllers so the policy
// is enforced on every matching request before the controller action runs.
app.UseRateLimiter();

app.MapControllers();

// ── SignalR Hub endpoint ──────────────────────────────────────────────────────
app.MapHub<QueueHub>("/hubs/queue");

/*Why /hubs/queue: This is the WebSocket URL clients connect to.
 * Mobile app and web frontend will connect to wss://yourserver.com/hubs/queue
 * to establish the persistent connection.*/

app.Run();