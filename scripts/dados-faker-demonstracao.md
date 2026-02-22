# Dados Faker – Roteiro para vídeo de demonstração

Use os blocos abaixo na ordem: criar propriedades, depois talhões (substituindo `{propertyId}` pelo ID retornado), depois enviar sensores (substituindo `SEU_PLOT_ID_AQUI` pelo ID do talhão desejado).

---

## 1.1 Criar propriedades (POST /api/properties)

**Propriedade 1 – Fazenda Santa Helena**

```json
{
  "name": "Fazenda Santa Helena",
  "location": "Ribeirão Preto, SP - Rodovia Anhanguera Km 312"
}
```

**Propriedade 2 – Sítio Boa Esperança**

```json
{
  "name": "Sítio Boa Esperança",
  "location": "Uberlândia, MG - Zona Rural"
}
```

**Propriedade 3 – Estância Verde**

```json
{
  "name": "Estância Verde",
  "location": "Dourados, MS - BR-163"
}
```

---

## 1.2 Criar talhões (POST /api/properties/{propertyId}/plots)

Substitua `{propertyId}` pelo id retornado ao criar a propriedade.

**Talhões – Propriedade 1 (Fazenda Santa Helena)**

```json
{ "name": "Talhão Norte", "culture": "Soja" }
```

```json
{ "name": "Talhão Sul", "culture": "Milho" }
```

```json
{ "name": "Talhão Leste", "culture": "Algodão" }
```

```json
{ "name": "Talhão Oeste", "culture": "Soja" }
```

**Talhões – Propriedade 2 (Sítio Boa Esperança)**

```json
{ "name": "Área A", "culture": "Café" }
```

```json
{ "name": "Área B", "culture": "Cana-de-açúcar" }
```

```json
{ "name": "Módulo 1", "culture": "Soja" }
```

```json
{ "name": "Módulo 2", "culture": "Milho" }
```

**Talhões – Propriedade 3 (Estância Verde)**

```json
{ "name": "Campo Central", "culture": "Soja" }
```

```json
{ "name": "Campo Sul", "culture": "Milho" }
```

---

## 2. Dados de sensores (POST /api/ingestion/sensors)

Substitua `SEU_PLOT_ID_AQUI` pelo id do talhão retornado ao criar os talhões.

**2.1 Um lote com umidade, temperatura e precipitação**

```json
{
  "plotId": "SEU_PLOT_ID_AQUI",
  "readings": [
    { "type": "moisture", "value": 42.5, "timestamp": "2025-02-20T06:00:00Z" },
    { "type": "temperature", "value": 22.0, "timestamp": "2025-02-20T06:00:00Z" },
    { "type": "precipitation", "value": 0, "timestamp": "2025-02-20T06:00:00Z" },
    { "type": "moisture", "value": 38.0, "timestamp": "2025-02-20T12:00:00Z" },
    { "type": "temperature", "value": 28.5, "timestamp": "2025-02-20T12:00:00Z" },
    { "type": "precipitation", "value": 0, "timestamp": "2025-02-20T12:00:00Z" },
    { "type": "moisture", "value": 55.2, "timestamp": "2025-02-20T18:00:00Z" },
    { "type": "temperature", "value": 24.0, "timestamp": "2025-02-20T18:00:00Z" },
    { "type": "precipitation", "value": 5.5, "timestamp": "2025-02-20T18:00:00Z" }
  ]
}
```

**2.2 Vários dias (simulação histórica) – umidade e temperatura**

```json
{
  "plotId": "SEU_PLOT_ID_AQUI",
  "readings": [
    { "type": "moisture", "value": 35.0, "timestamp": "2025-02-15T08:00:00Z" },
    { "type": "temperature", "value": 26.0, "timestamp": "2025-02-15T08:00:00Z" },
    { "type": "moisture", "value": 28.5, "timestamp": "2025-02-16T08:00:00Z" },
    { "type": "temperature", "value": 27.5, "timestamp": "2025-02-16T08:00:00Z" },
    { "type": "moisture", "value": 25.0, "timestamp": "2025-02-17T08:00:00Z" },
    { "type": "temperature", "value": 29.0, "timestamp": "2025-02-17T08:00:00Z" },
    { "type": "moisture", "value": 22.0, "timestamp": "2025-02-18T08:00:00Z" },
    { "type": "temperature", "value": 30.5, "timestamp": "2025-02-18T08:00:00Z" },
    { "type": "moisture", "value": 45.0, "timestamp": "2025-02-19T08:00:00Z" },
    { "type": "temperature", "value": 23.0, "timestamp": "2025-02-19T08:00:00Z" },
    { "type": "moisture", "value": 52.0, "timestamp": "2025-02-20T08:00:00Z" },
    { "type": "temperature", "value": 21.5, "timestamp": "2025-02-20T08:00:00Z" }
  ]
}
```

**2.3 Cenário de alerta de seca (umidade baixa por período)**

*Gera alerta **Drought** se todas as leituras de umidade nas últimas 24h forem &lt; 30%. Use o mesmo plotId e envie este bloco para um talhão.*

```json
{
  "plotId": "SEU_PLOT_ID_AQUI",
  "readings": [
    { "type": "moisture", "value": 28.0, "timestamp": "2025-02-20T00:00:00Z" },
    { "type": "moisture", "value": 26.0, "timestamp": "2025-02-20T04:00:00Z" },
    { "type": "moisture", "value": 24.0, "timestamp": "2025-02-20T08:00:00Z" },
    { "type": "moisture", "value": 22.0, "timestamp": "2025-02-20T12:00:00Z" },
    { "type": "moisture", "value": 20.0, "timestamp": "2025-02-20T16:00:00Z" },
    { "type": "moisture", "value": 18.0, "timestamp": "2025-02-20T20:00:00Z" },
    { "type": "moisture", "value": 25.0, "timestamp": "2025-02-21T00:00:00Z" },
    { "type": "temperature", "value": 30.0, "timestamp": "2025-02-20T12:00:00Z" }
  ]
}
```

**2.4 Cenário de risco de praga (umidade e temperatura altas)**

*Gera alerta **Plague** se média de umidade &gt; 75% e média de temperatura &gt; 26°C nas últimas 24h. Use outro plotId (outro talhão).*

```json
{
  "plotId": "SEU_PLOT_ID_AQUI",
  "readings": [
    { "type": "moisture", "value": 78.0, "timestamp": "2025-02-20T00:00:00Z" },
    { "type": "temperature", "value": 27.5, "timestamp": "2025-02-20T00:00:00Z" },
    { "type": "moisture", "value": 80.0, "timestamp": "2025-02-20T06:00:00Z" },
    { "type": "temperature", "value": 28.0, "timestamp": "2025-02-20T06:00:00Z" },
    { "type": "moisture", "value": 79.0, "timestamp": "2025-02-20T12:00:00Z" },
    { "type": "temperature", "value": 27.0, "timestamp": "2025-02-20T12:00:00Z" },
    { "type": "moisture", "value": 78.0, "timestamp": "2025-02-20T18:00:00Z" },
    { "type": "temperature", "value": 28.5, "timestamp": "2025-02-20T18:00:00Z" }
  ]
}
```

**2.5 Cenário de alerta de geada (temperatura muito baixa)**

*Gera alerta **Frost** se a temperatura mínima nas últimas 24h for &lt; 2°C. Use outro plotId.*

```json
{
  "plotId": "SEU_PLOT_ID_AQUI",
  "readings": [
    { "type": "temperature", "value": 1.5, "timestamp": "2025-02-20T05:00:00Z" },
    { "type": "temperature", "value": 0.5, "timestamp": "2025-02-20T06:00:00Z" },
    { "type": "temperature", "value": 3.0, "timestamp": "2025-02-20T12:00:00Z" },
    { "type": "temperature", "value": 1.0, "timestamp": "2025-02-20T18:00:00Z" }
  ]
}
```

**2.6 Cenário de risco de alagamento (umidade muito alta)**

*Gera alerta **Flood** se a média de umidade nas últimas 24h for &gt; 90%. Use outro plotId.*

```json
{
  "plotId": "SEU_PLOT_ID_AQUI",
  "readings": [
    { "type": "moisture", "value": 92.0, "timestamp": "2025-02-20T00:00:00Z" },
    { "type": "moisture", "value": 94.0, "timestamp": "2025-02-20T06:00:00Z" },
    { "type": "moisture", "value": 91.0, "timestamp": "2025-02-20T12:00:00Z" },
    { "type": "moisture", "value": 93.0, "timestamp": "2025-02-20T18:00:00Z" }
  ]
}
```

**2.7 Cenário informativo (condições favoráveis)**

*Gera alerta **Info** se a média de umidade estiver entre 45% e 65% e a média de temperatura entre 20°C e 26°C nas últimas 24h. Use outro plotId.*

```json
{
  "plotId": "SEU_PLOT_ID_AQUI",
  "readings": [
    { "type": "moisture", "value": 52.0, "timestamp": "2025-02-20T00:00:00Z" },
    { "type": "temperature", "value": 23.0, "timestamp": "2025-02-20T00:00:00Z" },
    { "type": "moisture", "value": 55.0, "timestamp": "2025-02-20T08:00:00Z" },
    { "type": "temperature", "value": 24.5, "timestamp": "2025-02-20T08:00:00Z" },
    { "type": "moisture", "value": 50.0, "timestamp": "2025-02-20T16:00:00Z" },
    { "type": "temperature", "value": 22.0, "timestamp": "2025-02-20T16:00:00Z" }
  ]
}
```

---

**Resumo:** Propriedades e talhões são criados com POST nas APIs de Identity/Properties (com token). Sensores são enviados em POST /api/ingestion/sensors (sem autenticação). Os alertas **Drought**, **Plague**, **Frost**, **Flood** e **Info** são gerados automaticamente pela Analysis API ao processar as leituras da fila RabbitMQ, conforme as regras do motor de alertas.
