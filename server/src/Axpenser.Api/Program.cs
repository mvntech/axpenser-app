using Axpenser.Api.Auth;
using Axpenser.Application.Auth.Dtos;
using Axpenser.Infrastructure.Auth;
using Axpenser.Infrastructure.Identity;
using Axpenser.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("AppConnection")));

// Identity
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(opt =>
{
    opt.Password.RequiredLength = 8;
    opt.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.Configure<CookieAuthenticationOptions>(IdentityConstants.ExternalScheme, options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// JWT options + token service
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// AuthN
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(CookieTokenWriter.AccessTokenCookieName, out var token))
                    context.Token = token;

                return Task.CompletedTask;
            }
        };
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["OAuth:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"]!;
        options.SaveTokens = true;
        options.CallbackPath = "/api/auth/external/google/callback";
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/api/auth/external/error?reason=" + System.Net.WebUtility.UrlEncode(context.Failure?.Message ?? "google_remote_failure"));
            context.HandleResponse();
            return Task.CompletedTask;
        };
    })
    .AddOAuth("GitHub", options =>
    {
        options.ClientId = builder.Configuration["OAuth:GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["OAuth:GitHub:ClientSecret"]!;
        options.CallbackPath = "/api/auth/external/github/callback";

        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";
        options.SaveTokens = true;

        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Scope.Add("user:email");

        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

        options.Events.OnCreatingTicket = async context =>
        {
            var req = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
            req.Headers.Add("User-Agent", "Axpenser-App");

            var res = await context.Backchannel.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var error = await res.Content.ReadAsStringAsync();
                throw new Exception("Failed to fetch GitHub profile.");
            }

            var json = await res.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            context.RunClaimActions(doc.RootElement);

            // Fallback for private emails
            if (!context.Identity!.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                var emailReq = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
                emailReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);
                emailReq.Headers.Add("User-Agent", "Axpenser-App");

                var emailRes = await context.Backchannel.SendAsync(emailReq);
                if (emailRes.IsSuccessStatusCode)
                {
                    var emailsJson = await emailRes.Content.ReadAsStringAsync();
                    using var emailsDoc = System.Text.Json.JsonDocument.Parse(emailsJson);
                    var primaryEmail = emailsDoc.RootElement.EnumerateArray()
                        .FirstOrDefault(e => e.GetProperty("primary").GetBoolean()).GetProperty("email").GetString();
                    
                    if (!string.IsNullOrEmpty(primaryEmail))
                    {
                        context.Identity.AddClaim(new Claim(ClaimTypes.Email, primaryEmail));
                    }
                }
            }
        };

        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/api/auth/external/error?reason=" + System.Net.WebUtility.UrlEncode(context.Failure?.Message ?? "github_remote_failure"));
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// CORS (Angular -> API with cookies)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("axpenser", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.Secure = CookieSecurePolicy.Always;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("axpenser");
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
}

app.MapControllers();

app.Run();
