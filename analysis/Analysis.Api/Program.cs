using Analysis.Api.Data;
using Analysis.Api.Entities;
using Analysis.Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Port=5432;Database=analysis_db;Username=agro;Password=secret";
builder.Services.AddDbContext<AnalysisDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly(typeof(AnalysisDbContext).Assembly.GetName().Name)));

builder.Services.AddScoped<ISensorReadingsTimeSeriesStore, PostgresSensorReadingsStore>();

builder.Services.AddHttpClient();
builder.Services.AddHostedService<SensorReadingsConsumer>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Analysis API", Version = "v1", Description = "Análise e alertas (AgroSolutions IoT)." });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

await EnsureDatabaseExistsAsync(connectionString, app.Logger);
await EnsureDatabaseAsync(app);
await SeedAlertsAndReadingsAsync(app);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Analysis API v1"));
app.UseHttpMetrics();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();

public partial class Program
{
    /// <summary>
    /// Cria o banco analysis_db se não existir (o init do Postgres no Docker só roda quando o volume é novo).
    /// </summary>
    static async Task EnsureDatabaseExistsAsync(string connectionString, ILogger logger)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var dbToCreate = builder.Database ?? "analysis_db";
            builder.Database = "postgres";
            await using var conn = new NpgsqlConnection(builder.ConnectionString);
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
        var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                logger.LogInformation("Aplicando migrations (tentativa {Attempt}/{Max})...", attempt, maxAttempts);
                await db.Database.MigrateAsync();
                await VerifySchemaAndRecoverIfNeededAsync(db, logger);
                logger.LogInformation("Migrations aplicadas com sucesso. Tabelas Alerts e SensorReadings disponíveis.");
                return;
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
                logger.LogWarning(ex, "Tabela não encontrada (42P01). Reaplicando migrations (limpando __EFMigrationsHistory).");
                await ClearMigrationsHistoryAndReapplyAsync(db, logger);
                continue;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Falha ao aplicar migrations (tentativa {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                if (attempt == maxAttempts)
                {
                    logger.LogError("Migrations não aplicadas após {Max} tentativas. Verifique conexão com PostgreSQL e se o banco analysis_db existe.", maxAttempts);
                    throw;
                }
                await Task.Delay(delayMs);
            }
        }
    }

    /// <summary>
    /// Se MigrateAsync disse "already up to date" mas a tabela Alerts não existe, reaplica e aplica fallback se necessário.
    /// Usa information_schema para evitar exceção 42P01 e ruído nos logs.
    /// </summary>
    static async Task VerifySchemaAndRecoverIfNeededAsync(AnalysisDbContext db, ILogger logger)
    {
        var countResult = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Alerts'").ToListAsync();
        if (countResult.FirstOrDefault() > 0) return;
        logger.LogWarning("Tabela Alerts não encontrada. Reaplicando migrations e aplicando fallback se necessário.");
        await ClearMigrationsHistoryAndReapplyAsync(db, logger);
    }

    /// <summary>
    /// Remove histórico, reaplica MigrateAsync e, se as tabelas ainda não existirem, aplica esquema manualmente (fallback).
    /// </summary>
    static async Task ClearMigrationsHistoryAndReapplyAsync(AnalysisDbContext db, ILogger logger)
    {
        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"__EFMigrationsHistory\" CASCADE");
        await db.Database.MigrateAsync();

        var countResult = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Alerts'").ToListAsync();
        var tableExists = countResult.FirstOrDefault() > 0;
        if (!tableExists)
        {
            logger.LogWarning("Tabela Alerts ainda ausente após MigrateAsync. Aplicando esquema manualmente (fallback).");
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""Alerts"" (
                    ""Id"" uuid NOT NULL,
                    ""PlotId"" uuid NOT NULL,
                    ""Type"" character varying(64) NOT NULL,
                    ""Message"" character varying(512) NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Alerts"" PRIMARY KEY (""Id"")
                );
                CREATE INDEX IF NOT EXISTS ""IX_Alerts_PlotId_CreatedAt"" ON ""Alerts"" (""PlotId"", ""CreatedAt"");");
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""SensorReadings"" (
                    ""Id"" uuid NOT NULL,
                    ""PlotId"" uuid NOT NULL,
                    ""Type"" character varying(64) NOT NULL,
                    ""Value"" double precision NOT NULL,
                    ""Timestamp"" timestamp with time zone NOT NULL,
                    ""IngestedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_SensorReadings"" PRIMARY KEY (""Id"")
                );
                CREATE INDEX IF NOT EXISTS ""IX_SensorReadings_PlotId_Timestamp"" ON ""SensorReadings"" (""PlotId"", ""Timestamp"");
                CREATE INDEX IF NOT EXISTS ""IX_SensorReadings_PlotId_Type_Timestamp"" ON ""SensorReadings"" (""PlotId"", ""Type"", ""Timestamp"");");
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
    /// PlotId do primeiro talhão seedado na Properties API (Talhão Norte). Deve ser o mesmo que Properties.Program.SeededFirstPlotId.
    /// </summary>
    static readonly Guid SeededPlotId = new("B2000000-2000-2000-2000-000000000001");

    /// <summary>
    /// Insere leituras e alertas de demonstração quando as tabelas estão vazias (para o dashboard exibir dados iniciais).
    /// </summary>
    static async Task SeedAlertsAndReadingsAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AnalysisDbContext>();
        if (await db.SensorReadings.AnyAsync()) return;

        var now = DateTime.UtcNow;
        var baseTime = now.AddDays(-7);

        for (var d = 0; d < 7; d++)
        {
            var ts = baseTime.AddDays(d);
            db.SensorReadings.Add(new SensorReading
            {
                Id = Guid.NewGuid(),
                PlotId = SeededPlotId,
                Type = "moisture",
                Value = 45 + (d % 25),
                Timestamp = ts,
                IngestedAt = ts
            });
            db.SensorReadings.Add(new SensorReading
            {
                Id = Guid.NewGuid(),
                PlotId = SeededPlotId,
                Type = "temperature",
                Value = 22 + (d % 8),
                Timestamp = ts,
                IngestedAt = ts
            });
        }

        db.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            PlotId = SeededPlotId,
            Type = "Drought",
            Message = "Alerta de Seca: umidade abaixo de 30% por mais de 24h.",
            CreatedAt = now.AddDays(-2)
        });
        db.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            PlotId = SeededPlotId,
            Type = "Info",
            Message = "Dados iniciais de demonstração. Execute o seed de leituras para mais dados.",
            CreatedAt = now
        });

        await db.SaveChangesAsync();
    }
}
