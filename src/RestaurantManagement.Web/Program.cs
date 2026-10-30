using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.Text;
using Sentry;

// Shared
using RestaurantManagement.Shared;
using RestaurantManagement.Shared.Auth;
using RestaurantManagement.Shared.Data.AuditLog;
using RestaurantManagement.Shared.Data.Tenancy;
using RestaurantManagement.Shared.Middleware;
using RestaurantManagement.Shared.Options;
using RestaurantManagement.Shared.Services.AuditLog;
using RestaurantManagement.Shared.Services.Email;
using RestaurantManagement.Shared.Services.Idempotency;
using RestaurantManagement.Shared.Services.Identity;
using RestaurantManagement.Shared.Services.Organization;
using RestaurantManagement.Shared.Utils.Exceptions;
using RestaurantManagement.Shared.Utils.Localization;
using RestaurantManagement.Web.Auth;
using RestaurantManagement.Web.Hosting;
using RestaurantManagement.Web.RateLimiting;

// Modules
using RestaurantManagement.Modules.Identity.Data;
using RestaurantManagement.Modules.Identity.Services.Languages;
using RestaurantManagement.Modules.Identity.Services.Lockout;
using RestaurantManagement.Modules.Identity.Services.Passwords;
using RestaurantManagement.Modules.Identity.Services.Registration;
using RestaurantManagement.Modules.Identity.Services.Sessions;
using RestaurantManagement.Modules.Menu.Data;
using RestaurantManagement.Modules.Menu.Services;
using RestaurantManagement.Modules.Organization.Data;
using RestaurantManagement.Modules.Organization.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------
// Azure Key Vault (before any configuration is read)
// ---------------------------------------------
builder.AddKeyVaultSecrets();

// Outside Development: HTTPS only, and startup fails without a certificate
builder.UseHttpsOnlyOutsideDevelopment();

// ---------------------------------------------
// Add Localization
// ---------------------------------------------
builder.Services.AddLocalization(options => options.ResourcesPath = "Localization");

var supportedCultures = RegionalCultureProvider.SupportedCultures;

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new RegionalCultureProvider());
});

// ---------------------------------------------
// JWT Authentication
// ---------------------------------------------
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt section is missing");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
        // Only the algorithm the app signs with, so a token cannot pick a different one.
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        // The framework default gives 5 minutes past expiry; access tokens are short-lived, so none.
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        // Forged and expired tokens answer alike, so the body never says why.
        OnAuthenticationFailed = async ctx =>
        {
            ctx.NoResult();
            await ctx.HttpContext.WriteProblemAsync(ErrorCodes.InvalidToken, "InvalidToken");
        },
        OnChallenge = async ctx =>
        {
            ctx.HandleResponse();
            await ctx.HttpContext.WriteProblemAsync(ErrorCodes.Unauthorized, "UnauthorizedMessage");
        },
        OnForbidden = ctx => ctx.HttpContext.WriteProblemAsync(ErrorCodes.AccessDenied, "AccessDenied")
    };
});

builder.Services.AddPermissionPolicies();
builder.Services.AddFallbackProblemDetails();
builder.Services.AddLocalizedValidationTitle();

// ---------------------------------------------
// Controllers
// ---------------------------------------------
builder.Services.AddControllers(options => options.Filters.Add<IdempotencyKeyFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    })
    .AddApplicationPart(typeof(RestaurantManagement.Shared.Controllers.AuditLogController).Assembly)
    .AddApplicationPart(typeof(RestaurantManagement.Modules.Identity.Controllers.AuthController).Assembly)
    .AddApplicationPart(typeof(RestaurantManagement.Modules.Menu.Controllers.MenuController).Assembly)
    .AddApplicationPart(typeof(RestaurantManagement.Modules.Organization.Controllers.OrganizationController).Assembly);

// Error answers are written outside MVC (ProblemResponse); they must name enums the same way.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ---------------------------------------------
// Options Configuration
// ---------------------------------------------
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
builder.Services.AddOptions<VerificationCodeOptions>()
    .Bind(builder.Configuration.GetSection("VerificationCode"))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<VerificationCodeOptions>, VerificationCodeOptionsValidator>();
builder.Services.AddOptions<RestaurantManagement.Modules.Identity.Services.Invitations.InvitationOptions>()
    .Bind(builder.Configuration.GetSection(RestaurantManagement.Modules.Identity.Services.Invitations.InvitationOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<RestaurantManagement.Modules.Identity.Services.Invitations.InvitationOptions>, RestaurantManagement.Modules.Identity.Services.Invitations.InvitationOptionsValidator>();
builder.Services.AddOptions<IdempotencyOptions>()
    .Bind(builder.Configuration.GetSection(IdempotencyOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<IdempotencyOptions>, IdempotencyOptionsValidator>();
if (builder.Configuration.GetValue("Idempotency:CleanupEnabled", true))
{
    builder.Services.AddHostedService<IdempotencyKeyCleanupWorker>();
}
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));
builder.Services.AddAuthRateLimiting(builder.Configuration);

// ---------------------------------------------
// Database Contexts (Per Module)
// ---------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITenantScope, TenantScope>();
builder.Services.AddScoped<ICurrentUser, ClaimsCurrentUser>();
builder.Services.AddScoped<SelectedLocation>();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();

builder.Services.AddDbContext<IdentityDbContext>((serviceProvider, options) =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention()
        .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

builder.Services.AddDbContext<MenuDbContext>((serviceProvider, options) =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention()
        .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

builder.Services.AddDbContext<OrganizationDbContext>((serviceProvider, options) =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention()
        .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));

builder.Services.AddAuditLog((sp, options) => options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention()
    .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

// ---------------------------------------------
// Sentry for error tracking
// ---------------------------------------------
builder.AddSentryTracking();

// ---------------------------------------------
// Swagger/OpenAPI
// ---------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    foreach (var version in RestaurantManagement.Web.OpenApi.ApiVersions.Supported)
    {
        c.SwaggerDoc(version, new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Restaurant Management API",
            Version = version,
            Description = "Modular Monolith API for Restaurant Management. "
                + "Inside a version, changes are additive only (new endpoints, new optional fields); a breaking change ships as a new version. "
                + "An old version stays served for at least 30 days after the next one ships."
        });
    }

    c.DocInclusionPredicate((version, api) =>
        RestaurantManagement.Web.OpenApi.ApiVersions.IncludesRoute(version, api.RelativePath ?? string.Empty));
    c.OperationFilter<RestaurantManagement.Web.OpenApi.AppVersionHeaderOperationFilter>();
    c.OperationFilter<RestaurantManagement.Web.OpenApi.IdempotencyKeyHeaderOperationFilter>();
    c.SchemaFilter<RestaurantManagement.Web.OpenApi.VersionRequiredSchemaFilter>();

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Paste the access token from /api/v1/auth/login (without the 'Bearer' prefix).",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Every code of every catalog, read from the registered modules' assemblies
    c.DocumentFilter<RestaurantManagement.Web.OpenApi.ErrorCodesDocumentFilter>(new object[] { new System.Reflection.Assembly[]
    {
        typeof(ErrorCodes).Assembly,
        typeof(RestaurantManagement.Modules.Identity.Controllers.AuthController).Assembly,
        typeof(RestaurantManagement.Modules.Menu.Controllers.MenuController).Assembly,
        typeof(RestaurantManagement.Modules.Organization.Controllers.OrganizationController).Assembly
    } });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ---------------------------------------------
// Dependency Injection: Register Services
// ---------------------------------------------
// Shared Services
builder.Services.AddScoped<IEmailService, EmailService>();

// Identity Module Services
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IRefreshTokenCleanupService, RefreshTokenCleanupService>();
if (builder.Configuration.GetValue("RefreshTokenCleanup:Enabled", true))
{
    builder.Services.AddHostedService<RefreshTokenCleanupWorker>();
}
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IPasswordResetEmailProcessor, PasswordResetEmailProcessor>();
builder.Services.AddScoped<IVerificationEmailProcessor, VerificationEmailProcessor>();
builder.Services.AddScoped<IAccountLockedEmailProcessor, AccountLockedEmailProcessor>();
if (builder.Configuration.GetValue("PendingEmails:Enabled", true))
{
    var pendingEmailIntervalSeconds = builder.Configuration.GetValue("PendingEmails:IntervalSeconds", PendingEmailWorker.DefaultInterval.TotalSeconds);
    // A zero or negative interval makes the worker's timer throw, which stops the whole app.
    if (pendingEmailIntervalSeconds <= 0)
        throw new InvalidOperationException("PendingEmails:IntervalSeconds must be greater than 0");
    var pendingEmailInterval = TimeSpan.FromSeconds(pendingEmailIntervalSeconds);
    builder.Services.AddHostedService(sp => new PendingEmailWorker(
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<ILogger<PendingEmailWorker>>(),
        pendingEmailInterval));
}
builder.Services.AddScoped<IPasswordChangeService, PasswordChangeService>();
builder.Services.AddScoped<IUserLanguageService, UserLanguageService>();
builder.Services.AddScoped<IPasswordAuditRecorder, PasswordAuditRecorder>();
builder.Services.AddSingleton<IVerificationCodeHasher, VerificationCodeHasher>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

// Organization Module Services
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ILocationService, LocationService>();

// Menu Module Services
builder.Services.AddScoped<RestaurantManagement.Modules.Menu.Services.IMenuService, RestaurantManagement.Modules.Menu.Services.MenuService>();
builder.Services.AddScoped<RestaurantManagement.Modules.Menu.Services.IMenuCategoryService, RestaurantManagement.Modules.Menu.Services.MenuCategoryService>();

// Cross-Module Contracts (Shared interface, implemented by the owning module)
builder.Services.AddScoped<IOrganizationLookup, RestaurantManagement.Modules.Organization.Services.OrganizationLookup>();
builder.Services.AddScoped<IMemberAccessLookup, RestaurantManagement.Modules.Identity.Services.Members.MemberAccessLookup>();
builder.Services.AddScoped<IOrganizationAccessSetup, RestaurantManagement.Modules.Identity.Services.Members.OrganizationAccessSetup>();
builder.Services.AddScoped<RestaurantManagement.Modules.Identity.Services.Members.IMemberAccessService, RestaurantManagement.Modules.Identity.Services.Members.MemberAccessService>();
builder.Services.AddScoped<RestaurantManagement.Modules.Identity.Services.Members.IMemberGridBuilder, RestaurantManagement.Modules.Identity.Services.Members.MemberGridBuilder>();
builder.Services.AddScoped<RestaurantManagement.Modules.Identity.Services.Members.IMemberDelegationRules, RestaurantManagement.Modules.Identity.Services.Members.MemberDelegationRules>();
builder.Services.AddScoped<RestaurantManagement.Modules.Identity.Services.Roles.IRoleManagementRules, RestaurantManagement.Modules.Identity.Services.Roles.RoleManagementRules>();
builder.Services.AddScoped<RestaurantManagement.Modules.Identity.Services.Roles.IRoleService, RestaurantManagement.Modules.Identity.Services.Roles.RoleService>();
builder.Services.AddScoped<RestaurantManagement.Modules.Identity.Services.Invitations.IInvitationService, RestaurantManagement.Modules.Identity.Services.Invitations.InvitationService>();
builder.Services.AddScoped<IAccessGuard, AccessGuard>();

// ---------------------------------------------
// CORS
// ---------------------------------------------
builder.Services.AddCors(options =>
{
    var corsOptions = builder.Configuration.GetSection("Cors").Get<CorsOptions>()
                      ?? new CorsOptions();

    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(corsOptions.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Retry-After")
              .AllowCredentials();
    });
});

// ---------------------------------------------
// Build the app
// ---------------------------------------------
var app = builder.Build();

// ---------------------------------------------
// Configure the HTTP request pipeline
// ---------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    // Keep the token across page reloads so developers don't re-authorize every time
    app.UseSwaggerUI(options =>
    {
        foreach (var version in RestaurantManagement.Web.OpenApi.ApiVersions.Supported)
            options.SwaggerEndpoint($"/swagger/{version}/swagger.json", $"Restaurant Management API {version}");
        options.ConfigObject.PersistAuthorization = true;
    });
}

app.UseForwardedHeaders();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseMiddleware<AppVersionMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<LocationAccessMiddleware>();
app.MapControllers();
app.Run();

public partial class Program { }
