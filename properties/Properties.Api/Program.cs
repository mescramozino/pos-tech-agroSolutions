using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Prometheus;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Properties.Api.Services;
using Properties.Application;
using Properties.Domain;
using Properties.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Testing"))
{
    var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
    connection.Open();
    builder.Services.AddDbContext<PropertiesDbContext>(options => options.UseSqlite(connection));
}
else
{
    builder.Services.AddDbContext<PropertiesDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=properties_db;Username=agro;Password=secret"));
}

builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IPlotRepository, PlotRepository>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IPlotService, PlotService>();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "AgroSolutionsIdentitySecretKeyMin32Chars!";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Identity.Api",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AgroSolutions",
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IProducerIdAccessor, ProducerIdAccessor>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AllowJwtOrProducerId", policy =>
        policy.Requirements.Add(new Properties.Api.Authorization.ProducerIdOrJwtRequirement()));
});
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Properties.Api.Authorization.ProducerIdOrJwtHandler>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Properties API", Version = "v1", Description = "Propriedades e talhões (AgroSolutions IoT)." });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

await EnsureDatabaseAsync(app);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Properties API v1"));

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpMetrics();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();

public partial class Program
{
    static async Task EnsureDatabaseAsync(WebApplication app)
    {
        const int maxAttempts = 15;
        const int delayMs = 2000;
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Properties.Infrastructure.PropertiesDbContext>();
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.Database.EnsureCreatedAsync();
                await SeedPropertiesAndPlotsAsync(db);
                return;
            }
            catch (Exception)
            {
                if (attempt == maxAttempts) throw;
                await Task.Delay(delayMs);
            }
        }
    }

    static readonly Guid SeededProducerId = new("A1000000-1000-1000-1000-000000000001");

    static async Task SeedPropertiesAndPlotsAsync(Properties.Infrastructure.PropertiesDbContext db)
    {
        if (await db.Properties.AnyAsync()) return;

        var propertyId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var property = new Property
        {
            Id = propertyId,
            ProducerId = SeededProducerId,
            Name = "Fazenda Modelo",
            Location = "Região Sul",
            CreatedAt = now
        };
        db.Properties.Add(property);

        var plots = new[]
        {
            new Plot { Id = Guid.NewGuid(), PropertyId = propertyId, Name = "Talhão Norte", Culture = "Soja", CreatedAt = now },
            new Plot { Id = Guid.NewGuid(), PropertyId = propertyId, Name = "Talhão Sul", Culture = "Milho", CreatedAt = now },
            new Plot { Id = Guid.NewGuid(), PropertyId = propertyId, Name = "Talhão Leste", Culture = "Soja", CreatedAt = now }
        };
        foreach (var plot in plots) db.Plots.Add(plot);

        await db.SaveChangesAsync();
    }
}
