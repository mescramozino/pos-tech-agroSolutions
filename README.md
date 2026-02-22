# Plataforma IoT AgroSolutions – Hackathon 8NETT

MVP de agricultura de precisão: autenticação, cadastro de propriedades e talhões, ingestão de dados de sensores (umidade, temperatura, precipitação), dashboard com gráficos e status por talhão, motor de alertas (ex.: seca) e previsão do tempo.

## Documentação

- [Arquitetura](docs/arquitetura.md)
- [Contratos de API e eventos](docs/contratos.md)
- [Passo a passo: Git, Docker e execução](docs/PASSO_A_PASSO_GIT_E_DOCKER.md)
- [Passo a passo: CD (build, push GHCR, deploy K8s)](docs/PASSO_A_PASSO_CD.md)
- [Scripts (criar repositório no GitHub)](scripts/README.md)

## Estrutura do repositório

```
identity/     → Serviço de Identidade (registro, login, JWT)
properties/   → Serviço de Propriedades e Talhões (CRUD)
ingestion/    → Serviço de Ingestão de dados de sensores (publica em RabbitMQ)
analysis/     → Serviço de Análise e Alertas (consome fila, PostgreSQL para leituras, regra de seca, API de previsão)
dashboard/    → Frontend Angular (login, propriedades, talhões, gráficos, alertas, previsão do tempo)
docs/         → Diagramas, contratos e passo a passo
k8s/          → Manifests Kubernetes (APIs, Postgres, RabbitMQ, Prometheus, Grafana, Ingress)
scripts/      → Seed de sensores (Docker), criação de bancos; ver scripts/README.md
apoio-desenvolvimento/ → Documentos e scripts de apoio (planejamento, análises, k8s local, etc.)
```

## Stack

| Camada        | Tecnologia |
|---------------|------------|
| Backend       | .NET 8 (ASP.NET Core Web API) – Identity, Properties, Ingestion, Analysis |
| Frontend      | Angular 17 (standalone, ng2-charts) |
| Banco         | PostgreSQL (Docker); SQLite em testes locais; leituras de sensores no Analysis em PostgreSQL |
| Mensageria    | RabbitMQ (fila `sensor.readings`) |
| Previsão do tempo | Open-Meteo (API pública) |
| Infra local   | Docker Compose (Postgres, RabbitMQ, 4 APIs, dashboard) |

CI no GitHub Actions (build + testes). Kubernetes e CD pendentes.

## Pré-requisitos

- **Docker e Docker Compose** – para subir o stack completo.
- **.NET 8** – para build e testes locais das APIs.
- **Node.js 18+** – apenas se for rodar o dashboard com `npm start` (desenvolvimento).

## Como executar o projeto (com tabelas populadas)

### Opção A: Docker (recomendado) – tudo populado na primeira subida

1. Na **raiz do projeto**, execute:

   ```bash
   docker-compose up --build
   ```

2. O que acontece:
   - **PostgreSQL** e **RabbitMQ** sobem primeiro.
   - As **4 APIs** (Identity, Properties, Ingestion, Analysis) sobem em seguida, criam os bancos `identity_db`, `properties_db` e `analysis_db` se não existirem, aplicam as **migrations** (EF Core) e rodam os **seeds**.
   - **Identity** insere o usuário **produtor@agro.local** / **Senha123!**.
   - **Properties** insere a propriedade **"Fazenda Modelo"** e os 3 talhões (Norte, Sul, Leste).
   - O serviço **seed-sensor-readings** roda em seguida e popula leituras de sensores (umidade/temperatura) para esses talhões via API de Ingestão → RabbitMQ → Analysis → PostgreSQL.
   - O **dashboard** fica disponível na porta 4200.

   **Se as tabelas ou seeds não aparecerem** (ex.: volume do Postgres já existia e o init não rodou): pare o stack (`docker compose down`), remova o volume e suba de novo (`docker compose up --build`) ou execute `.\scripts\docker-create-databases.ps1` com o stack rodando e depois `docker compose restart identity-api properties-api analysis-api`.

3. Acesse **http://localhost:4200**, faça login com **produtor@agro.local** / **Senha123!** e use os gráficos/alertas dos talhões.

Se o seed rodar antes dos talhões existirem (raro), execute de novo depois que o stack estiver estável:

```bash
docker-compose run --rm seed-sensor-readings
```

### Opção B: Sem Docker (APIs locais)

1. Suba **PostgreSQL** e **RabbitMQ** (ex.: só os serviços do compose: `docker-compose up -d postgres rabbitmq`).
2. Crie os bancos: `identity_db`, `properties_db`, `analysis_db` (use o script em `docker/postgres/init-multiple-databases.sh` ou crie manualmente).
3. Em **4 terminais**, na raiz do projeto, rode cada API:
   - `dotnet run --project identity/Identity.Api`
   - `dotnet run --project properties/Properties.Api`
   - `dotnet run --project ingestion/Ingestion.Api`
   - `dotnet run --project analysis/Analysis.Api`
4. Na subida, cada API aplica as **migrations** (EF Core) e insere os dados iniciais: Identity (produtor produtor@agro.local), Properties (Fazenda Modelo + 3 talhões).
5. Para popular as **leituras de sensores**, execute o script (PowerShell na raiz):

   ```powershell
   .\scripts\seed-sensor-readings.ps1
   ```

6. Rode o dashboard: `cd dashboard && npm install && npm start` e acesse http://localhost:4200.

Detalhes em [Passo a passo: Git, Docker e execução](docs/PASSO_A_PASSO_GIT_E_DOCKER.md).

### Como executar as migrations (criar tabelas e dados iniciais)

As **tabelas** são criadas pelas **EF Core Migrations**; os **dados iniciais** são inseridos pelo **seed** que cada API roda após a migração.

#### 1. Com Docker (automático)

Ao subir o stack, cada API aplica as migrations e o seed na subida:

```bash
docker compose up -d
```

- **Identity:** cria tabela `Producers` e insere o usuário **produtor@agro.local** / **Senha123!** (se estiver vazio).
- **Properties:** cria tabelas `Properties` e `Plots` e insere a propriedade **"Fazenda Modelo"** e os 3 talhões (se estiver vazio).
- **Analysis:** cria tabelas `Alerts` e `SensorReadings` (sem seed; leituras vêm do serviço **seed-sensor-readings** ou da ingestão).

Se os bancos não existirem (volume antigo do Postgres), crie-os e reinicie as APIs:

```powershell
.\scripts\docker-create-databases.ps1
```

Se só existir a tabela `__EFMigrationsHistory` e faltarem as demais, limpe o histórico e reinicie para as migrations rodarem de novo:

```powershell
.\scripts\docker-reset-migrations.ps1
```

Depois de as APIs subirem, o serviço **seed-sensor-readings** (no `docker-compose`) popula leituras de sensores. Se não tiver rodado, execute uma vez:

```bash
docker compose run --rm seed-sensor-readings
```

#### 2. Sem Docker (manual com dotnet ef)

Com **PostgreSQL** e os bancos **identity_db**, **properties_db** e **analysis_db** já criados, você pode aplicar as migrations sem subir as APIs:

1. Instale a ferramenta EF Core (uma vez):

   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. Defina a connection string (PowerShell; ajuste host/porta se precisar):

   ```powershell
   $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=identity_db;Username=agro;Password=secret"
   ```

3. Aplique as migrations em cada projeto:

   ```bash
   dotnet ef database update -p identity/Identity.Infrastructure -s identity/Identity.Api
   dotnet ef database update -p properties/Properties.Infrastructure -s properties/Properties.Api
   dotnet ef database update -p analysis/Analysis.Api -s analysis/Analysis.Api
   ```

   (Para Properties e Analysis, altere `Database=...` na variável de ambiente para `properties_db` e `analysis_db` antes do comando correspondente.)

4. Os **dados iniciais** (usuário, Fazenda Modelo, talhões) são inseridos quando você **sobe cada API** (`dotnet run` no projeto `.Api`), pois o seed roda após a migração na subida. Para leituras de sensores, use o script `scripts/seed-sensor-readings.ps1` (ou o serviço Docker acima).

Resumo: **Docker** → migrations e seed rodam ao subir os containers. **Local** → `dotnet ef database update` cria as tabelas; ao subir cada API com `dotnet run`, o seed preenche os dados iniciais.

### Criar uma nova migration (após alterar o modelo)

- **Identity:** `dotnet ef migrations add NomeDaAlteracao -p identity/Identity.Infrastructure -s identity/Identity.Api`
- **Properties:** `dotnet ef migrations add NomeDaAlteracao -p properties/Properties.Infrastructure -s properties/Properties.Api`
- **Analysis:** `dotnet ef migrations add NomeDaAlteracao -p analysis/Analysis.Api -s analysis/Analysis.Api`

## Rodar com Docker (recomendado)

Na raiz do projeto:

```bash
docker-compose up --build
```

| Serviço       | Porta  | Acesso |
|---------------|--------|--------|
| **Dashboard** | 4200   | http://localhost:4200 |
| Identity API  | 5001   | http://localhost:5001 |
| Properties API | 5002  | http://localhost:5002 |
| Ingestion API | 5003   | http://localhost:5003 |
| Analysis API  | 5004   | http://localhost:5004 |
| PostgreSQL    | 5432   | localhost:5432 (user: agro, password: secret) |
| RabbitMQ      | 5672, 15672 | AMQP e management UI http://localhost:15672 (agro/secret) |

Abra **http://localhost:4200**, registre um usuário ou faça login. Na subida, cada API aplica as **migrations** (EF Core) e insere os dados iniciais: usuário **produtor@agro.local** / **Senha123!**, propriedade **"Fazenda Modelo"** e três talhões. O serviço **seed-sensor-readings** roda junto e popula leituras de sensores (depois de Identity, Properties e Ingestion estarem no ar).

## Rodar o dashboard em desenvolvimento

Se as APIs já estiverem rodando (Docker ou local), na pasta do dashboard:

```bash
cd dashboard
npm install
npm start
```

Acesse http://localhost:4200. O proxy (`proxy.conf.json`) redireciona `/api/identity`, `/api/properties`, `/api/plots`, `/api/analysis` e `/api/weather` para as portas corretas (5001, 5002, 5004).

## Rodar as APIs localmente (sem Docker)

1. Subir **PostgreSQL** (ou usar o do Docker na porta 5432) e **RabbitMQ** (5672).
2. Criar os bancos: `identity_db`, `properties_db`, `analysis_db` (script em `docker/postgres/init-multiple-databases.sh`).
3. Em cada pasta de serviço (`identity`, `properties`, `ingestion`, `analysis`), configurar `appsettings.Development.json` (connection strings, RabbitMQ) e executar `dotnet run` no projeto `.Api`.

Detalhes no [passo a passo](docs/PASSO_A_PASSO_GIT_E_DOCKER.md).

## Testes e CI

```bash
dotnet build hacakton.sln -c Release
dotnet test identity/Identity.Api.Tests/Identity.Api.Tests.csproj
dotnet test properties/Properties.Api.Tests/Properties.Api.Tests.csproj
dotnet test ingestion/Ingestion.Api.Tests/Ingestion.Api.Tests.csproj
```

O pipeline no GitHub Actions executa build e esses testes em cada push.

## Status do projeto

- **Concluído:** Arquitetura em microsserviços, quatro APIs (Identity, Properties, Ingestion, Analysis), RabbitMQ, leituras em PostgreSQL, dashboard Angular (gráficos, alertas, previsão do tempo), motor de alertas (seca e praga). Kubernetes (manifests em `k8s/`), Prometheus e Grafana, CI e CD no GitHub Actions (build, testes, push GHCR, deploy em Kind).
- **Pendente para entrega do hackathon:** Vídeo de demonstração (máx. 15 min) e link do repositório público.
