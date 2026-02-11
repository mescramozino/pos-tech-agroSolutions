<#
.SYNOPSIS
    Cria um repositório no GitHub e faz o primeiro push (ou só configura o remote e envia).
.DESCRIPTION
    Usa GitHub CLI (gh) se instalado; caso contrário, usa a API do GitHub com a variável GITHUB_TOKEN.
    Executar na pasta raiz do projeto (onde está o .git ou onde deseja inicializar).
.PARAMETER RepoName
    Nome do repositório no GitHub. Se omitido, usa o nome da pasta atual.
.PARAMETER Description
    Descrição do repositório (opcional).
.PARAMETER Visibility
    "public" ou "private". Padrão: public.
.PARAMETER SkipPush
    Se definido, apenas cria o repositório remoto; não faz git add/commit/push.
.EXAMPLE
    .\create-repo.ps1 -RepoName agrosolutions-iot -Description "MVP IoT Hackathon 8NETT"
.EXAMPLE
    .\create-repo.ps1 -RepoName hacakton -Visibility private
.NOTES
    Pré-requisitos: Git instalado. Para criar o repo: GitHub CLI (gh) OU variável GITHUB_TOKEN.
    Instalar GitHub CLI: winget install GitHub.cli  ou  https://cli.github.com/
#>

param(
    [string] $RepoName = (Split-Path -Leaf (Get-Location)),
    [string] $Description = "MVP Plataforma IoT AgroSolutions - Hackathon 8NETT",
    [ValidateSet("public", "private")]
    [string] $Visibility = "public",
    [switch] $SkipPush
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Error "Git nao encontrado. Instale em https://git-scm.com"
    exit 1
}

$repoUrl = $null
$publicArg = if ($Visibility -eq "public") { "--public" } else { "--private" }

# --- Opção 1: GitHub CLI (gh) ---
if (Get-Command gh -ErrorAction SilentlyContinue) {
    Write-Host "Usando GitHub CLI (gh)..." -ForegroundColor Cyan
    if (-not $SkipPush) {
        $created = $false
        try {
            gh repo create $RepoName $publicArg --description $Description --source . --remote origin 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) { $created = $true }
        } catch { }
        if ($created) {
            Write-Host "Repositorio criado e codigo enviado com sucesso." -ForegroundColor Green
            exit 0
        }
    } else {
        gh repo create $RepoName $publicArg --description $Description 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            $existing = gh repo view $RepoName 2>$null
            if (-not $existing) { Write-Error "Falha ao criar repositorio. Verifique: gh auth status"; exit 1 }
        }
    }
    $repoUrl = gh repo view $RepoName --json url -q ".url" 2>$null
    if (-not $repoUrl) { Write-Error "Nao foi possivel obter a URL do repositorio."; exit 1 }
    if (-not $repoUrl.EndsWith(".git")) { $repoUrl = $repoUrl + ".git" }
} else {
    # --- Opção 2: API do GitHub com GITHUB_TOKEN ---
    $token = $env:GITHUB_TOKEN
    if (-not $token) {
        Write-Error "GitHub CLI (gh) nao encontrado e GITHUB_TOKEN nao definido. Instale 'gh' (https://cli.github.com) ou defina GITHUB_TOKEN."
        exit 1
    }
    Write-Host "Usando API do GitHub com GITHUB_TOKEN..." -ForegroundColor Cyan
    $body = @{
        name        = $RepoName
        description = $Description
        private     = ($Visibility -eq "private")
    } | ConvertTo-Json
    $headers = @{
        "Authorization" = "token $token"
        "Accept"        = "application/vnd.github.v3+json"
    }
    try {
        $resp = Invoke-RestMethod -Uri "https://api.github.com/user/repos" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        $repoUrl = $resp.clone_url
        Write-Host "Repositorio criado: $repoUrl" -ForegroundColor Green
    } catch {
        if ($_.Exception.Response.StatusCode -eq 422) {
            $login = (Invoke-RestMethod -Uri "https://api.github.com/user" -Headers $headers).login
            $repoUrl = "https://github.com/$login/$RepoName.git"
            Write-Host "Repositorio '$RepoName' ja existe. URL: $repoUrl" -ForegroundColor Yellow
        } else { throw }
    }
}

if ($SkipPush) {
    Write-Host "SkipPush ativado. Nenhum push realizado." -ForegroundColor Cyan
    Write-Host "Para conectar e enviar manualmente: git remote add origin $repoUrl && git push -u origin main" -ForegroundColor Cyan
    exit 0
}

# --- Configurar Git local e enviar ---
if (-not (Test-Path .git)) {
    Write-Host "Inicializando repositorio Git local..." -ForegroundColor Cyan
    git init
    git branch -M main
    git add .
    git commit -m "Initial commit: AgroSolutions IoT Hackathon 8NETT"
}

git remote remove origin 2>$null
git remote add origin $repoUrl
git add .
$status = git status --short
if ($status) {
    git commit -m "Initial commit: AgroSolutions IoT Hackathon 8NETT"
}
Write-Host "Enviando para origin main..." -ForegroundColor Cyan
git push -u origin main
Write-Host "Concluido. Repositorio: $repoUrl" -ForegroundColor Green
