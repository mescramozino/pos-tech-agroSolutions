#!/bin/sh
# Popula leituras de sensores via API (Identity -> Properties -> Ingestion).
# Uso: dentro do container com IDENTITY_URL, PROPERTIES_URL, INGESTION_URL, EMAIL, PASSWORD.
# Rode após as APIs estarem no ar (ex.: como serviço no docker-compose).

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
    [range($total) | . as $i |
     {
       type: (if ($i % 2) == 0 then "moisture" else "temperature" end),
       value: (if ($i % 2) == 0 then (20 + (random * 60)) else (18 + (random * 15)) end | . * 10 | floor / 10),
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

echo "Seed concluído. Aguarde o Analysis processar a fila (alguns segundos) e confira o dashboard."
