#!/bin/sh
# Popula leituras de sensores via API (Identity -> Properties -> Ingestion -> RabbitMQ -> Analysis).
# Uso:
#   - Automático: serviço seed-sensor-readings no docker-compose (roda uma vez após as APIs subirem).
#   - Manual:     docker compose run --rm seed-sensor-readings
# Variáveis: IDENTITY_URL, PROPERTIES_URL, INGESTION_URL, EMAIL, PASSWORD, DAYS_BACK, READINGS_PER_DAY, WAIT_MAX

set -e

IDENTITY_URL="${IDENTITY_URL:-http://identity-api:8080}"
PROPERTIES_URL="${PROPERTIES_URL:-http://properties-api:8080}"
INGESTION_URL="${INGESTION_URL:-http://ingestion-api:8080}"
EMAIL="${EMAIL:-produtor@agro.local}"
PASSWORD="${PASSWORD:-Senha123!}"
DAYS_BACK="${DAYS_BACK:-30}"
READINGS_PER_DAY="${READINGS_PER_DAY:-6}"
WAIT_MAX="${WAIT_MAX:-90}"

echo "Aguardando Identity em $IDENTITY_URL (até ${WAIT_MAX}s)..."
n=0
while [ $n -lt "$WAIT_MAX" ]; do
  if curl -sf -o /dev/null "$IDENTITY_URL/health" 2>/dev/null || \
     curl -sf -o /dev/null -w '%{http_code}' -X POST "$IDENTITY_URL/api/auth/login" \
       -H "Content-Type: application/json" -d '{"email":"x","password":"y"}' 2>/dev/null | grep -q 401; then
    break
  fi
  n=$((n + 2))
  sleep 2
done
if [ $n -ge "$WAIT_MAX" ]; then echo "Timeout aguardando Identity."; exit 1; fi
echo "Identity OK."

echo "Aguardando Ingestion em $INGESTION_URL (até ${WAIT_MAX}s)..."
n=0
while [ $n -lt "$WAIT_MAX" ]; do
  if curl -sf -o /dev/null "$INGESTION_URL/health" 2>/dev/null; then
    break
  fi
  n=$((n + 2))
  sleep 2
done
if [ $n -ge "$WAIT_MAX" ]; then echo "Timeout aguardando Ingestion."; exit 1; fi
echo "Ingestion OK."

LOGIN_RESP="$(curl -sf -X POST "$IDENTITY_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")"
TOKEN="$(echo "$LOGIN_RESP" | jq -r '.token')"
if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
  echo "Login falhou. Verifique EMAIL/PASSWORD e se o usuário existe (ex.: produtor@agro.local / Senha123!)."
  exit 1
fi

echo "Obtendo propriedades e talhões..."
PROPS="$(curl -sf -H "Authorization: Bearer $TOKEN" "$PROPERTIES_URL/api/Properties")"
PROP_IDS="$(echo "$PROPS" | jq -r '.[].id')"
if [ -z "$PROP_IDS" ]; then
  echo "Nenhuma propriedade encontrada. Crie propriedades e talhões antes (dashboard ou API)."
  exit 0
fi

PLOT_IDS=""
for PID in $PROP_IDS; do
  PLOTS="$(curl -sf -H "Authorization: Bearer $TOKEN" "$PROPERTIES_URL/api/Properties/$PID/plots")"
  for PLOT_ID in $(echo "$PLOTS" | jq -r '.[].id'); do
    [ -n "$PLOT_ID" ] && [ "$PLOT_ID" != "null" ] && PLOT_IDS="$PLOT_IDS $PLOT_ID"
  done
done

if [ -z "$PLOT_IDS" ]; then
  echo "Nenhum talhão encontrado. Crie talhões nas propriedades antes."
  exit 0
fi

TOTAL_READINGS=$((DAYS_BACK * READINGS_PER_DAY * 2))
echo "Encontrados $(echo $PLOT_IDS | wc -w) talhão(ões). Gerando $TOTAL_READINGS leituras por talhão (umidade + temperatura)..."

for PLOT_ID in $PLOT_IDS; do
  BODY="$(jq -n --arg plotId "$PLOT_ID" --argjson days "$DAYS_BACK" --argjson total "$TOTAL_READINGS" '
    (now - ($days * 86400)) as $start |
    (now | floor) as $t |
    [range($total) | . as $i |
     {
       type: (if ($i % 2) == 0 then "moisture" else "temperature" end),
       value: (if ($i % 2) == 0 then ((($i * 7 + $t) % 61) + 20) else ((($i * 11 + $t) % 16) + 18) end | . * 10 | floor / 10),
       timestamp: (($start + ($i * (($days * 86400) / $total))) | strftime("%Y-%m-%dT%H:%M:%SZ"))
     }
    ] as $readings |
    {plotId: $plotId, readings: $readings}
  ')"
  if curl -sf -X POST "$INGESTION_URL/api/Ingestion/sensors" \
    -H "Content-Type: application/json" \
    -d "$BODY" >/dev/null 2>&1; then
    echo "  Plot $PLOT_ID: $TOTAL_READINGS leituras enviadas."
  else
    echo "  Plot $PLOT_ID: falha ao enviar (ingestion pode ainda estar iniciando)." >&2
  fi
done

echo "Seed concluído. As leituras foram enviadas ao Ingestion (RabbitMQ)."
echo "A Analysis API consome a fila e grava em analysis_db (SensorReadings + Alerts)."
echo "Aguarde alguns segundos e confira o dashboard em http://localhost:4200"
