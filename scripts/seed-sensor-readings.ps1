<#
.SYNOPSIS
    Popula a tabela de leituras de sensores via API de Ingestão (RabbitMQ -> Analysis -> PostgreSQL).

.DESCRIPTION
    Faz login no Identity, obtém propriedades e talhões no Properties, e envia leituras de
    umidade e temperatura para cada talhão via POST /api/ingestion/sensors.
    O Analysis consome a fila e persiste em PostgreSQL (tabela SensorReadings).

.PARAMETER IdentityUrl
    Base URL do serviço Identity (default: http://localhost:5001).

.PARAMETER PropertiesUrl
    Base URL do serviço Properties (default: http://localhost:5002).

.PARAMETER IngestionUrl
    Base URL do serviço Ingestion (default: http://localhost:5003).

.PARAMETER Email
    E-mail do produtor para login (default: produtor@agro.local).

.PARAMETER Password
    Senha do produtor (default: Senha123!).

.PARAMETER DaysBack
    Quantidade de dias de histórico a gerar (default: 30).

.PARAMETER ReadingsPerDay
    Leituras por dia por tipo (moisture, temperature) por talhão (default: 6).

.EXAMPLE
    .\scripts\seed-sensor-readings.ps1
    .\scripts\seed-sensor-readings.ps1 -DaysBack 7 -ReadingsPerDay 12
#>

param(
    [string] $IdentityUrl   = "http://localhost:5001",
    [string] $PropertiesUrl  = "http://localhost:5002",
    [string] $IngestionUrl   = "http://localhost:5003",
    [string] $Email          = "produtor@agro.local",
    [string] $Password       = "Senha123!",
    [int]    $DaysBack       = 30,
    [int]    $ReadingsPerDay = 6
)

$ErrorActionPreference = "Stop"

# Login
Write-Host "Fazendo login em $IdentityUrl ..."
$loginBody = @{ email = $Email; password = $Password } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod -Uri "$IdentityUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
} catch {
    Write-Error "Falha no login. Verifique se o Identity está rodando e se o usuário $Email existe (senha: $Password). Erro: $_"
}
$token = $loginResp.token
if (-not $token) { Write-Error "Resposta de login sem token." }
Write-Host "Login OK."

# Listar propriedades
Write-Host "Obtendo propriedades em $PropertiesUrl ..."
$headers = @{ Authorization = "Bearer $token" }
$properties = Invoke-RestMethod -Uri "$PropertiesUrl/api/Properties" -Headers $headers -Method Get
if (-not $properties -or $properties.Count -eq 0) {
    Write-Warning "Nenhuma propriedade encontrada. Crie uma propriedade e talhões pelo dashboard ou API antes de popular sensores."
    exit 0
}

# Coletar todos os talhões
$allPlots = @()
foreach ($prop in $properties) {
    $plots = Invoke-RestMethod -Uri "$PropertiesUrl/api/Properties/$($prop.id)/plots" -Headers $headers -Method Get
    foreach ($p in $plots) {
        $allPlots += [PSCustomObject]@{ id = $p.id; name = $p.name; propertyName = $prop.name }
    }
}

if ($allPlots.Count -eq 0) {
    Write-Warning "Nenhum talhão encontrado. Crie talhões nas propriedades antes de popular sensores."
    exit 0
}

Write-Host "Encontrados $($allPlots.Count) talhão(ões). Gerando $DaysBack dias com ~$ReadingsPerDay leituras/dia/tipo por talhão..."

$random = New-Object System.Random
$totalSent = 0
$baseTime = [DateTime]::UtcNow.AddDays(-$DaysBack)

foreach ($plot in $allPlots) {
    $plotId = $plot.id
    $readings = [System.Collections.Generic.List[object]]::new()

    for ($d = 0; $d -lt $DaysBack; $d++) {
        for ($r = 0; $r -lt $ReadingsPerDay; $r++) {
            $ts = $baseTime.AddDays($d).AddHours($r * (24 / $ReadingsPerDay)).AddMinutes($random.Next(0, 30))
            $moisture = [Math]::Round($random.NextDouble() * 60 + 20, 1)   # 20–80%
            $temp     = [Math]::Round($random.NextDouble() * 15 + 18, 1)   # 18–33 °C
            $readings.Add(@{ type = "moisture";    value = $moisture; timestamp = $ts.ToString("o") })
            $readings.Add(@{ type = "temperature"; value = $temp;     timestamp = $ts.ToString("o") })
        }
    }

    # Enviar em lotes para não sobrecarregar
    $batchSize = 50
    for ($i = 0; $i -lt $readings.Count; $i += $batchSize) {
        $batch = $readings.GetRange($i, [Math]::Min($batchSize, $readings.Count - $i))
        $body = @{ plotId = $plotId; readings = $batch } | ConvertTo-Json -Depth 4
        try {
            Invoke-RestMethod -Uri "$IngestionUrl/api/Ingestion/sensors" -Method Post -Body $body -ContentType "application/json; charset=utf-8" | Out-Null
            $totalSent += $batch.Count
        } catch {
            Write-Warning "Falha ao enviar lote para talhão $($plot.name): $_"
        }
    }
    Write-Host "  Talhão '$($plot.name)' ($($plot.propertyName)): $($readings.Count) leituras enviadas."
}

Write-Host "Concluído. Total de leituras enviadas: $totalSent. Aguarde alguns segundos para o Analysis processar a fila e persistir no PostgreSQL."
Write-Host "Verifique os gráficos em http://localhost:4200 (ou no dashboard) para o talhão desejado."
