using Microsoft.EntityFrameworkCore;
using Npgsql;
using Prometheus;
using Identity.Application.Interfaces;
using Identity.Application.Services;
using Identity.Domain;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Repositories;
using Identity.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=identity_db;Username=agro;Password=secret";
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(IdentityDbContext).Assembly.GetName().Name)));
builder.Services.AddScoped<IProducerRepository, ProducerRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Identity API", Version = "v1", Description = "Autenticação e registro de produtores (AgroSolutions IoT)." });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

await EnsureDatabaseExistsAsync(connectionString, app.Logger);
await EnsureDatabaseAndSeedAsync(app);

// Swagger no início do pipeline para servir /swagger/v1/swagger.json e a UI
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity API v1");
    c.RoutePrefix = "swagger";
});
app.UseHttpMetrics();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();

/// <summary>
/// Cria o banco identity_db se não existir (o init do Postgres no Docker só roda quando o volume é novo).
/// </summary>
static async Task EnsureDatabaseExistsAsync(string connectionString, ILogger logger)
{
    try
    {
        var csb = new NpgsqlConnectionStringBuilder(connectionString);
        var dbToCreate = csb.Database ?? "identity_db";
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

static async Task EnsureDatabaseAndSeedAsync(WebApplication app)
{
    const int maxAttempts = 15;
    const int delayMs = 2000;
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            await VerifySchemaAndRecoverIfNeededAsync(db, logger);
            await SeedProducersAsync(db);
            return;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
        {
            logger.LogWarning(ex, "Tabela não encontrada (42P01). Reaplicando migrations (limpando __EFMigrationsHistory).");
            await ClearMigrationsHistoryAndReapplyAsync(db, logger);
            // Não chamar Seed aqui; deixar o retry do loop executar Migrate + Verify + Seed de novo.
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
/// Se MigrateAsync considerou "up to date" mas a tabela Producers não existe (histórico inconsistente), reaplica e aplica fallback se necessário.
/// Usa information_schema para evitar exceção 42P01 e ruído nos logs.
/// </summary>
static async Task VerifySchemaAndRecoverIfNeededAsync(IdentityDbContext db, ILogger logger)
{
    var countResult = await db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Producers'").ToListAsync();
    if (countResult.FirstOrDefault() > 0) return;
    logger.LogWarning("Tabela Producers não encontrada. Reaplicando migrations e aplicando fallback se necessário.");
    await ClearMigrationsHistoryAndReapplyAsync(db, logger);
}

static async Task ClearMigrationsHistoryAndReapplyAsync(IdentityDbContext db, ILogger logger)
{
    // Remove completamente o histórico para forçar reaplicação (DROP garante que MigrateAsync veja "pendentes").
    await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"__EFMigrationsHistory\" CASCADE");
    await db.Database.MigrateAsync();

    // Se mesmo assim a tabela Producers não existir (EF não aplicou migrações, ex.: assembly em publish),
    // cria o esquema manualmente como fallback.
    var countResult = await db.Database.SqlQueryRaw<int>(
        "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Producers'").ToListAsync();
    var tableExists = countResult.FirstOrDefault() > 0;
    if (!tableExists)
    {
        logger.LogWarning("Tabela Producers ainda ausente após MigrateAsync. Aplicando esquema manualmente (fallback).");
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Producers"" (
                ""Id"" uuid NOT NULL,
                ""Email"" character varying(256) NOT NULL,
                ""PasswordHash"" character varying(256) NOT NULL,
                ""CreatedAt"" timestamp with time zone NOT NULL,
                CONSTRAINT ""PK_Producers"" PRIMARY KEY (""Id"")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Producers_Email"" ON ""Producers"" (""Email"");");
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                ""MigrationId"" character varying(150) NOT NULL,
                ""ProductVersion"" character varying(32) NOT NULL,
                CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
            );");
        await db.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
            VALUES ('20250215160000_InitialCreate', '8.0.11')
            ON CONFLICT (""MigrationId"") DO NOTHING;");
    }
}

static async Task SeedProducersAsync(IdentityDbContext db)
{
    if (await db.Producers.AnyAsync()) return;
    var producerId = new Guid("A1000000-1000-1000-1000-000000000001");
    var producer = new Producer
    {
        Id = producerId,
        Email = "produtor@agro.local",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Senha123!"),
        CreatedAt = DateTime.UtcNow
    };
    db.Producers.Add(producer);
    await db.SaveChangesAsync();
}
