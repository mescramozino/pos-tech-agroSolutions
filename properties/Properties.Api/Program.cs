using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Prometheus;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Properties.Api.Services;
using Properties.Application;
using Properties.Domain;
using Properties.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

string? connectionString = null;
if (builder.Environment.IsEnvironment("Testing"))
{
    var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
    connection.Open();
    builder.Services.AddDbContext<PropertiesDbContext>(options => options.UseSqlite(connection));
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=properties_db;Username=agro;Password=secret";
    builder.Services.AddDbContext<PropertiesDbContext>(options =>
        options.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(PropertiesDbContext).Assembly.GetName().Name)));
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
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Properties API", Version = "v1", Description = "Propriedades e talhões (AgroSolutions IoT)." });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

if (connectionString != null)
    await EnsureDatabaseExistsAsync(connectionString, app.Logger);
await EnsureDatabaseAsync(app);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Properties API v1"));
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpMetrics();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();

public partial class Program
{
    /// <summary>
    /// Cria o banco properties_db se não existir (o init do Postgres no Docker só roda quando o volume é novo).
    /// </summary>
    static async Task EnsureDatabaseExistsAsync(string connectionString, ILogger logger)
    {
        try
        {
            var csb = new NpgsqlConnectionStringBuilder(connectionString);
            var dbToCreate = csb.Database ?? "properties_db";
            csb.Database = "postgres";
            await using var conn = new NpgsqlConnection(csb.ConnectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
            cmd.Parameters.AddWithValue("name", dbToCreate);
            var exists = await cmd.ExecuteScalarAsync() != null;
            if (!exists)
            {
                logger.LogInformation("Banco {Database} não existe. Criando...", dbToCreate);
                cmd.CommandText = $"CREATE DATABASE \"{dbToCreate.Replace("\"", "\"\"")}\"";
                cmd.Parameters.Clear();
                await cmd.ExecuteNonQueryAsync();
                logger.LogInformation("Banco {Database} criado.", dbToCreate);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Não foi possível garantir o banco (pode já existir ou o usuário não tem permissão): {Message}", ex.Message);
        }
    }

    static async Task EnsureDatabaseAsync(WebApplication app)
    {
        const int maxAttempts = 15;
        const int delayMs = 2000;
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Properties.Infrastructure.PropertiesDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (env.IsEnvironment("Testing"))
                {
                    await db.Database.EnsureCreatedAsync();
                }
                else
                {
                    await db.Database.MigrateAsync();
                    await VerifySchemaAndRecoverIfNeededAsync(db, logger);
                }
                await SeedPropertiesAndPlotsAsync(db, logger);
                return;
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
                logger.LogWarning(ex, "Tabela não encontrada (42P01). Reaplicando migrations (limpando __EFMigrationsHistory).");
                await ClearMigrationsHistoryAndReapplyAsync(db, logger);
                continue;
            }
            catch (Exception)
            {
                if (attempt == maxAttempts) throw;
                await Task.Delay(delayMs);
            }
        }
    }

    /// <summary>
    /// Remove histórico, reaplica MigrateAsync e, se as tabelas ainda não existirem, aplica esquema manualmente (fallback).
    /// </summary>
    static async Task ClearMigrationsHistoryAndReapplyAsync(Properties.Infrastructure.PropertiesDbContext db, ILogger logger)
    {
        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"__EFMigrationsHistory\" CASCADE");
        await db.Database.MigrateAsync();

        var countResult = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Properties'").ToListAsync();
        var tableExists = countResult.FirstOrDefault() > 0;
        if (!tableExists)
        {
            logger.LogWarning("Tabela Properties ainda ausente após MigrateAsync. Aplicando esquema manualmente (fallback).");
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Properties"" (
                    ""Id"" uuid NOT NULL,
                    ""ProducerId"" uuid NOT NULL,
                    ""Name"" character varying(256) NOT NULL,
                    ""Location"" character varying(512) NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Properties"" PRIMARY KEY (""Id"")
                );");
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Plots"" (
                    ""Id"" uuid NOT NULL,
                    ""PropertyId"" uuid NOT NULL,
                    ""Name"" character varying(256) NOT NULL,
                    ""Culture"" character varying(256) NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Plots"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Plots_Properties_PropertyId"" FOREIGN KEY (""PropertyId"") REFERENCES ""Properties"" (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ""IX_Plots_PropertyId"" ON ""Plots"" (""PropertyId"");");
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                    ""MigrationId"" character varying(150) NOT NULL,
                    ""ProductVersion"" character varying(32) NOT NULL,
                    CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
                );
                INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                VALUES ('20250215160000_InitialCreate', '8.0.11')
                ON CONFLICT (""MigrationId"") DO NOTHING;");
        }
    }

    /// <summary>
    /// Se MigrateAsync considerou "up to date" mas a tabela Properties não existe (histórico inconsistente), reaplica e aplica fallback se necessário.
    /// Usa information_schema para evitar exceção 42P01 e ruído nos logs.
    /// </summary>
    static async Task VerifySchemaAndRecoverIfNeededAsync(Properties.Infrastructure.PropertiesDbContext db, ILogger logger)
    {
        var countResult = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Properties'").ToListAsync();
        if (countResult.FirstOrDefault() > 0) return;
        logger.LogWarning("Tabela Properties não encontrada. Reaplicando migrations e aplicando fallback se necessário.");
        await ClearMigrationsHistoryAndReapplyAsync(db, logger);
    }

    static readonly Guid SeededProducerId = new("A1000000-1000-1000-1000-000000000001");
    /// <summary>Id fixo do primeiro talhão (Talhão Norte) para o seed da Analysis poder referenciar.</summary>
    public static readonly Guid SeededFirstPlotId = new("B2000000-2000-2000-2000-000000000001");

    static async Task SeedPropertiesAndPlotsAsync(Properties.Infrastructure.PropertiesDbContext db, ILogger? logger = null)
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
            new Plot { Id = SeededFirstPlotId, PropertyId = propertyId, Name = "Talhão Norte", Culture = "Soja", CreatedAt = now },
            new Plot { Id = Guid.NewGuid(), PropertyId = propertyId, Name = "Talhão Sul", Culture = "Milho", CreatedAt = now },
            new Plot { Id = Guid.NewGuid(), PropertyId = propertyId, Name = "Talhão Leste", Culture = "Soja", CreatedAt = now }
        };
        foreach (var plot in plots) db.Plots.Add(plot);

        await db.SaveChangesAsync();
    }
}
