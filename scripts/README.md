# Scripts – Hackathon AgroSolutions

Os scripts desta pasta são usados pelo **Docker Compose** (seed de sensores) e para **criação de bancos** quando o init do Postgres falhar. Scripts de apoio ao desenvolvimento (create-repo, k8s-up/down, reset de migrações, validação de ambiente, SQL manual) estão em **[apoio-desenvolvimento/scripts/](apoio-desenvolvimento/scripts/)**.

## create-repo (criar repositório no GitHub)

Os scripts `create-repo.ps1` e `create-repo.sh` foram movidos para **apoio-desenvolvimento/scripts/**.

Na pasta **raiz do projeto**:

- **Windows:** `.\apoio-desenvolvimento\scripts\create-repo.ps1 -RepoName agrosolutions-iot -Description "MVP IoT Hackathon 8NETT"`
- **Linux/macOS:** `./apoio-desenvolvimento/scripts/create-repo.sh agrosolutions-iot public`

Pré-requisitos: Git; GitHub CLI (gh) ou variável GITHUB_TOKEN.

---

## seed-sensor-readings (popular leituras de sensores)

Popula a tabela de dados dos sensores para uso em gráficos e alertas no dashboard.

### Execução automática com Docker Compose

Ao subir o stack com `docker-compose up`, o serviço **seed-sensor-readings** é executado após Identity, Properties e Ingestion estarem no ar. Ele faz login, lista os talhões e envia leituras para a API de Ingestão (RabbitMQ → Analysis → PostgreSQL). O container encerra após concluir (não fica rodando). Variáveis de ambiente no `docker-compose.yml`:

| Variável         | Padrão                 | Descrição                |
|------------------|------------------------|---------------------------|
| `EMAIL` / `PASSWORD` | produtor@agro.local / Senha123! | Login do produtor |
| `DAYS_BACK`      | 30                     | Dias de histórico         |
| `READINGS_PER_DAY` | 6                    | Leituras/dia por tipo     |

Se ainda não houver propriedades/talhões (primeira subida), o seed termina sem erro; crie uma propriedade e talhões pelo dashboard e rode o seed de novo: `docker-compose run --rm seed-sensor-readings`.

### Opção 1: Via API (manual)

Usa a API de Ingestão (POST /api/ingestion/sensors). Os dados passam pelo RabbitMQ e o Analysis persiste no PostgreSQL. Requer Identity, Properties e Ingestion rodando.

**Pré-requisitos:** Stack no ar (Docker ou APIs locais), usuário `produtor@agro.local` / `Senha123!` e pelo menos uma propriedade com talhões.

**Windows (PowerShell), na raiz do projeto:**

```powershell
.\scripts\seed-sensor-readings.ps1
```

Parâmetros opcionais:

| Parâmetro       | Padrão                 | Descrição                          |
|-----------------|------------------------|------------------------------------|
| `IdentityUrl`    | http://localhost:5001  | URL do Identity                    |
| `PropertiesUrl`  | http://localhost:5002  | URL do Properties                  |
| `IngestionUrl`   | http://localhost:5003  | URL do Ingestion                   |
| `Email`         | produtor@agro.local    | E-mail para login                  |
| `Password`      | Senha123!              | Senha do produtor                  |
| `DaysBack`      | 30                     | Dias de histórico a gerar          |
| `ReadingsPerDay`| 6                      | Leituras por dia por tipo (umidade/temperatura) |

Exemplo: 7 dias com 12 leituras/dia por tipo:

```powershell
.\scripts\seed-sensor-readings.ps1 -DaysBack 7 -ReadingsPerDay 12
```

### Opção 2: Inserção direta no PostgreSQL

Útil quando você já tem os IDs dos talhões e quer inserir direto no `analysis_db`. O arquivo SQL está em **apoio-desenvolvimento/scripts/seed-sensor-readings.sql**.

1. Obtenha um `PlotId`: faça login no dashboard ou via API e liste propriedades/talhões.
2. Abra `apoio-desenvolvimento/scripts/seed-sensor-readings.sql` e substitua `'SEU_PLOT_ID_AQUI'` pelo GUID do talhão.
3. Execute:

```bash
psql -h localhost -p 5432 -U agro -d analysis_db -f apoio-desenvolvimento/scripts/seed-sensor-readings.sql
```

Senha padrão (Docker): `secret`. O script insere ~30 dias de umidade e temperatura (intervalo de 4 h).
