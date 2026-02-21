# Contratos de API e Eventos – AgroSolutions IoT

## 1. Serviço de Identidade (Identity)

### POST /api/auth/register

**Request:**

```json
{
  "email": "produtor@exemplo.com",
  "password": "SenhaSegura123"
}
```

| Campo    | Tipo   | Obrigatório | Descrição                    |
|----------|--------|-------------|------------------------------|
| email    | string | Sim         | E-mail único do produtor     |
| password | string | Sim         | Senha (será armazenada em hash) |

**Response 200 OK:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "produtor@exemplo.com"
}
```

**Response 400 Bad Request:** Email já cadastrado ou entrada inválida.

---

### POST /api/auth/login

**Request:**

```json
{
  "email": "produtor@exemplo.com",
  "password": "SenhaSegura123"
}
```

**Response 200 OK:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "produtor@exemplo.com"
}
```

**Response 401 Unauthorized:** E-mail ou senha inválidos.

---

## 2. Serviço de Propriedades (Properties)

### Estrutura de dados

**Property:**

| Campo     | Tipo   | Descrição                    |
|-----------|--------|------------------------------|
| id        | guid   | Identificador da propriedade |
| producerId| guid   | ID do produtor (do JWT)      |
| name      | string | Nome da propriedade          |
| location  | string | Localização (opcional)      |
| createdAt | datetime | Data de criação           |

**Plot (Talhão):**

| Campo     | Tipo   | Descrição                    |
|-----------|--------|------------------------------|
| id        | guid   | Identificador do talhão      |
| propertyId| guid   | ID da propriedade            |
| name      | string | Nome do talhão               |
| culture   | string | Cultura plantada (ex.: Soja)  |
| createdAt | datetime | Data de criação           |

**Autenticação:** Bearer JWT (token do Identity) ou header `X-Producer-Id` (guid do produtor) para testes.

---

### POST /api/properties

**Request:**

```json
{
  "name": "Fazenda Sul",
  "location": "Região Sul"
}
```

**Response 201 Created:** Corpo com `PropertyResponse` (id, producerId, name, location, createdAt).

---

### GET /api/properties

**Response 200 OK:** Lista de `PropertyResponse`.

---

### GET /api/properties/{id}

**Response 200 OK:** Um `PropertyResponse`. **404** se não pertencer ao produtor.

---

### PUT /api/properties/{id}

**Request:** Mesmo corpo de POST (name, location).

**Response 204 No Content.** **404** se não pertencer ao produtor.

---

### DELETE /api/properties/{id}

**Response 204 No Content.** **404** se não pertencer ao produtor.

---

### POST /api/properties/{propertyId}/plots

**Request:**

```json
{
  "name": "Talhão 1",
  "culture": "Soja"
}
```

**Response 201 Created:** Corpo com `PlotResponse` (id, propertyId, name, culture, createdAt).

---

### GET /api/properties/{propertyId}/plots

**Response 200 OK:** Lista de `PlotResponse`.

---

### GET /api/plots/{id}

**Response 200 OK:** Um `PlotResponse`. **404** se o talhão não pertencer a uma propriedade do produtor.

---

### PUT /api/plots/{id}

**Request:** name, culture.

**Response 204 No Content.**

---

### DELETE /api/plots/{id}

**Response 204 No Content.**

---

## 3. Serviço de Ingestão (Ingestion)

### POST /api/ingestion/sensors

**Request:**

```json
{
  "plotId": "550e8400-e29b-41d4-a716-446655440000",
  "readings": [
    {
      "type": "moisture",
      "value": 45.5,
      "timestamp": "2025-01-30T12:00:00Z"
    },
    {
      "type": "temperature",
      "value": 28.0,
      "timestamp": "2025-01-30T12:00:00Z"
    },
    {
      "type": "precipitation",
      "value": 0,
      "timestamp": "2025-01-30T12:00:00Z"
    }
  ]
}
```

| Campo     | Tipo   | Obrigatório | Descrição                                      |
|-----------|--------|-------------|------------------------------------------------|
| plotId    | guid   | Sim         | ID do talhão                                   |
| readings  | array  | Sim         | Lista de leituras (pelo menos uma)             |

**Cada reading:**

| Campo     | Tipo   | Obrigatório | Descrição                                      |
|-----------|--------|-------------|------------------------------------------------|
| type      | string | Sim         | `moisture`, `temperature` ou `precipitation` (ou umidade, temperatura, precipitacao) |
| value     | number | Sim         | Valor numérico                                 |
| timestamp | string (ISO 8601) | Sim | Data/hora da leitura                    |

**Response 202 Accepted:** Dados aceitos para processamento.

**Response 400 Bad Request:** plotId ausente, readings vazio ou tipo inválido.

---

## 4. Mensageria (RabbitMQ)

### 4.1 Fila e exchange

- **Fila:** `sensor.readings` – leituras de sensores para o serviço de Análise/Alertas.
- **Formato:** uma mensagem por leitura (cada item do array de `readings` gera uma mensagem).

### 4.2 Evento: leitura de sensor (sensor.readings)

Publicado pelo **Serviço de Ingestão** ao receber `POST /api/ingestion/sensors`. O **Serviço de Análise/Alertas** consome e persiste em séries temporais e aplica regras de alerta.

**Payload (por leitura):**

```json
{
  "plotId": "550e8400-e29b-41d4-a716-446655440000",
  "type": "moisture",
  "value": 45.5,
  "timestamp": "2025-01-30T12:00:00Z",
  "ingestedAt": "2025-01-30T12:00:05Z"
}
```

| Campo      | Tipo   | Descrição                          |
|-----------|--------|------------------------------------|
| plotId    | guid   | ID do talhão                       |
| type      | string | `moisture`, `temperature`, `precipitation` |
| value     | number | Valor da leitura                   |
| timestamp | string (ISO 8601) | Data/hora da leitura no sensor |
| ingestedAt| string (ISO 8601) | Data/hora em que a Ingestão aceitou o dado |

### 4.3 Regra de alerta (serviço de Análise)

- **Alerta de Seca:** se a umidade do solo (`moisture`) for &lt; 30% por mais de 24 horas contínuas para um mesmo talhão, o serviço de Análise cria um alerta do tipo `Drought` (Alerta de Seca) e persiste na tabela de alertas para o dashboard consultar.

---

## 5. Serviço de Análise/Alertas (Analysis)

### GET /api/analysis/plots/{plotId}/readings

**Query:** `from` (ISO 8601), `to` (ISO 8601), `type` (opcional: moisture, temperature, precipitation).

**Response 200 OK:** Lista de leituras persistidas para o talhão no período.

```json
[
  {
    "id": "uuid",
    "plotId": "uuid",
    "type": "moisture",
    "value": 25.0,
    "timestamp": "2025-01-30T12:00:00Z"
  }
]
```

---

### GET /api/analysis/plots/{plotId}/status

**Response 200 OK:** Status atual do talhão com base nas regras de alerta.

```json
{
  "plotId": "uuid",
  "status": "DroughtAlert",
  "message": "Alerta de Seca: umidade abaixo de 30% por mais de 24h."
}
```

**Valores de status:** `Normal`, `DroughtAlert`, `PestRisk` (futuro).

**Persistência de leituras:** O serviço de Análise armazena leituras de sensores em **PostgreSQL** (tabela `SensorReadings`). O histórico é consultado via `GET /api/analysis/plots/{plotId}/readings`. Alertas permanecem em PostgreSQL.

---

### GET /api/analysis/alerts

**Query:** `plotId` (opcional), `from` (opcional, ISO 8601).

**Response 200 OK:** Lista de alertas.

```json
[
  {
    "id": "uuid",
    "plotId": "uuid",
    "type": "Drought",
    "message": "Alerta de Seca",
    "createdAt": "2025-01-30T14:00:00Z"
  }
]
```

---

## 6. API de Previsão do Tempo (Analysis)

Integração com Open-Meteo para exibir previsão no dashboard.

### GET /api/weather/forecast

**Query:** `city` (opcional, nome da cidade), `lat` e `lon` (opcionais, coordenadas). Se nenhum for informado, usa São Paulo como padrão.

**Response 200 OK:**

```json
{
  "location": "São Paulo",
  "temperatureC": 24.1,
  "humidityPercent": 76,
  "precipitationMm": 0.0,
  "weatherCode": 3,
  "daily": [
    {
      "date": "2025-01-30",
      "maxTempC": 24.4,
      "minTempC": 20.3,
      "precipitationMm": 19.6
    }
  ]
}
```

O dashboard exibe este dado no widget "Previsão do tempo" (busca por cidade ou exibe padrão).
