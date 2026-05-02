using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MultiTenant.Api.Application.Interfaces;
using MultiTenant.Api.Identity;
using MultiTenant.Api.Infrastructure.Persistence;
using MultiTenant.Api.Infrastructure.Security;
using MultiTenant.Api.Middleware;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    };
});
// ✅ SERVICES (ALL BEFORE Build)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:8080"];

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Configuration Jwt:Key is missing or empty. Add Jwt settings to appsettings.json.");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MultiTenant API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// EF (example)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// JWT (example)
var jwt = builder.Configuration.GetSection("Jwt");
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
          ValidIssuer = jwt["Issuer"],

          ValidateAudience = true,
          ValidAudience = jwt["Audience"],

          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

          ValidateLifetime = true,
          ClockSkew = TimeSpan.FromSeconds(30)
      };
  });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PlatformAdminOnly", policy =>
        policy.RequireClaim("is_platform_admin", "true"));
});
// Your scoped services/middleware
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<TenantResolverMiddleware>();

// ✅ BUILD (after all AddX)
var app = builder.Build();

await SeedPlatformAdminAsync(app);

// ✅ MIDDLEWARE (after Build)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.UseAuthentication();
app.UseMiddleware<TenantResolverMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task SeedPlatformAdminAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    const string adminEmail = "admin@expertlinx.com";
    const string adminPassword = "Admin@123";

    var user = await userManager.Users.FirstOrDefaultAsync(x => x.Email == adminEmail);

    if (user is null)
    {
        user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Platform Admin",
            EmailConfirmed = true,
            IsPlatformAdmin = true
        };

        var createResult = await userManager.CreateAsync(user, adminPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Failed to seed platform admin user: {errors}");
        }

        return;
    }

    user.UserName = adminEmail;
    user.Email = adminEmail;
    user.FullName ??= "Platform Admin";
    user.EmailConfirmed = true;
    user.IsPlatformAdmin = true;

    var updateResult = await userManager.UpdateAsync(user);
    if (!updateResult.Succeeded)
    {
        var errors = string.Join("; ", updateResult.Errors.Select(x => x.Description));
        throw new InvalidOperationException($"Failed to update seeded platform admin user: {errors}");
    }

    var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
    var resetResult = await userManager.ResetPasswordAsync(user, resetToken, adminPassword);
    if (!resetResult.Succeeded)
    {
        var errors = string.Join("; ", resetResult.Errors.Select(x => x.Description));
        throw new InvalidOperationException($"Failed to reset seeded platform admin password: {errors}");
    }
}
