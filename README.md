# Plataforma IoT AgroSolutions – Hackathon 8NETT

MVP de agricultura de precisão: autenticação, cadastro de propriedades e talhões, ingestão de dados de sensores (umidade, temperatura, precipitação), dashboard com gráficos e status por talhão, motor de alertas (ex.: seca) e previsão do tempo.

## Documentação

- [Plano de execução e implementação](PLANO_EXECUCAO_E_IMPLEMENTACAO.md)
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
analysis/     → Serviço de Análise e Alertas (consome fila, InfluxDB, regra de seca, API de previsão)
dashboard/    → Frontend Angular (login, propriedades, talhões, gráficos, alertas, previsão do tempo)
docs/         → Diagramas, contratos e passo a passo
```

## Stack

| Camada        | Tecnologia |
|---------------|------------|
| Backend       | .NET 8 (ASP.NET Core Web API) – Identity, Properties, Ingestion, Analysis |
| Frontend      | Angular 17 (standalone, ng2-charts) |
| Banco         | PostgreSQL (Docker); SQLite em testes locais |
| Séries temporais | InfluxDB 2 (leituras de sensores no Analysis) |
| Mensageria    | RabbitMQ (fila `sensor.readings`) |
| Previsão do tempo | Open-Meteo (API pública) |
| Infra local   | Docker Compose (Postgres, RabbitMQ, InfluxDB, 4 APIs, dashboard) |

CI no GitHub Actions (build + testes). Kubernetes e CD pendentes.

## Pré-requisitos

- **Docker e Docker Compose** – para subir o stack completo.
- **.NET 8** – para build e testes locais das APIs.
- **Node.js 18+** – apenas se for rodar o dashboard com `npm start` (desenvolvimento).

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
| InfluxDB      | 8086   | http://localhost:8086 (agro/secret, org: agro, bucket: sensor_readings) |

Abra **http://localhost:4200**, registre um usuário ou faça login, e use: Propriedades, talhões, gráficos por talhão, alertas e widget de previsão do tempo.

## Rodar o dashboard em desenvolvimento

Se as APIs já estiverem rodando (Docker ou local), na pasta do dashboard:

```bash
cd dashboard
npm install
npm start
```

Acesse http://localhost:4200. O proxy (`proxy.conf.json`) redireciona `/api/identity`, `/api/properties`, `/api/plots`, `/api/analysis` e `/api/weather` para as portas corretas (5001, 5002, 5004).

## Rodar as APIs localmente (sem Docker)

1. Subir **PostgreSQL** (ou usar o do Docker na porta 5432), **RabbitMQ** (5672) e **InfluxDB** (8086), ou usar apenas Postgres + RabbitMQ (sem InfluxDB: a API de readings retorna vazio).
2. Criar os bancos: `identity_db`, `properties_db`, `analysis_db` (script em `docker/postgres/init-multiple-databases.sh`).
3. Em cada pasta de serviço (`identity`, `properties`, `ingestion`, `analysis`), configurar `appsettings.Development.json` (connection strings, RabbitMQ, InfluxDB) e executar `dotnet run` no projeto `.Api`.

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

- **Concluído:** Fases 1 a 3 (arquitetura, quatro microsserviços, RabbitMQ, InfluxDB para sensores, dashboard Angular, previsão do tempo).
- **Parcial:** CI (build + testes); CD pendente.
- **Pendente:** Kubernetes, observabilidade (Prometheus/Grafana), vídeo e entrega final.

Resumo detalhado no [Plano de execução](PLANO_EXECUCAO_E_IMPLEMENTACAO.md#31-status-atual-da-implementação).
