using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SAMTEK.Application.Interfaces;
using SAMTEK.Infrastructure;
using SAMTEK.Infrastructure.Persistence;
using SAMTEK.Web.Services;
using System.Text;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

// Session state per Blazor circuit (scoped = per browser tab in Blazor Server)
builder.Services.AddScoped<SamtekSession>();

// Named HttpClient for calling APPA API
var appaApiUrl = builder.Configuration["Appa:ApiUrl"] ?? "http://localhost:5008";
builder.Services.AddHttpClient("appa", c =>
{
    c.BaseAddress = new Uri(appaApiUrl);
    c.Timeout = TimeSpan.FromSeconds(10);
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required in appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SAMTEK",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SAMTEK",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Auto-migrate + seed default admin if none exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SamtekDbContext>();
    db.Database.Migrate();

    var adminAuth = scope.ServiceProvider.GetRequiredService<IAdminAuthService>();
    if (!await adminAuth.HasAnyAdminAsync())
        await adminAuth.CreateAdminAsync("admin", "admin123");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
