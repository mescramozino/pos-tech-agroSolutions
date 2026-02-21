# Cria os bancos identity_db, properties_db e analysis_db no Postgres do Docker.
# Use quando as tabelas não forem criadas (ex.: volume do Postgres já existia e o init não rodou).
#
# Uso (na raiz do projeto):
#   .\scripts\docker-create-databases.ps1
#
# Ou, se o nome do container for outro:
#   $env:POSTGRES_CONTAINER="nome-do-container"; .\scripts\docker-create-databases.ps1

$ErrorActionPreference = "Stop"
$container = $env:POSTGRES_CONTAINER
if (-not $container) {
    $container = (docker compose ps -q postgres 2>$null)
    if (-not $container) {
        $container = (docker ps -q -f "ancestor=postgres:16-alpine" 2>$null)
    }
    if (-not $container) {
        Write-Host "Container do Postgres nao encontrado. Suba o stack antes: docker compose up -d" -ForegroundColor Red
        exit 1
    }
}
$container = $container.Trim()
Write-Host "Usando container Postgres: $container"

$dbs = @("identity_db", "properties_db", "analysis_db")
foreach ($db in $dbs) {
    Write-Host "Criando banco $db (se nao existir)..."
    $exists = docker exec $container psql -U agro -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$db'" 2>$null
    if ($exists -match "1") {
        Write-Host "  Banco $db ja existe." -ForegroundColor Gray
    } else {
        docker exec $container psql -U agro -d postgres -c "CREATE DATABASE $db;"
        Write-Host "  Banco $db criado." -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Reiniciando as APIs para aplicar as migrations..." -ForegroundColor Cyan
$null = docker compose restart identity-api properties-api analysis-api 2>$null
if ($LASTEXITCODE -ne 0) {
    docker compose restart identity-api properties-api analysis-api
}

Write-Host ""
Write-Host "Pronto. Aguarde alguns segundos e verifique os logs:" -ForegroundColor Green
Write-Host "  docker compose logs -f analysis-api" -ForegroundColor Gray
Write-Host "  (deve aparecer 'Migrations aplicadas com sucesso')" -ForegroundColor Gray
