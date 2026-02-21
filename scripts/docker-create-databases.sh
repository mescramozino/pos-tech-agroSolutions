#!/bin/bash
# Cria os bancos identity_db, properties_db e analysis_db no Postgres do Docker.
# Use quando as tabelas não forem criadas (ex.: volume do Postgres já existia e o init não rodou).
#
# Uso (na raiz do projeto):
#   chmod +x scripts/docker-create-databases.sh
#   ./scripts/docker-create-databases.sh

set -e
CONTAINER="${POSTGRES_CONTAINER:-$(docker compose ps -q postgres 2>/dev/null || docker ps -q -f 'ancestor=postgres:16-alpine' 2>/dev/null)}"
if [ -z "$CONTAINER" ]; then
  echo "Container do Postgres não encontrado. Suba o stack antes: docker compose up -d"
  exit 1
fi
CONTAINER=$(echo "$CONTAINER" | tr -d '[:space:]')
echo "Usando container Postgres: $CONTAINER"

for db in identity_db properties_db analysis_db; do
  echo "Criando banco $db (se não existir)..."
  if ! docker exec "$CONTAINER" psql -U agro -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$db'" | grep -q 1; then
    docker exec "$CONTAINER" psql -U agro -d postgres -c "CREATE DATABASE $db;"
    echo "  Banco $db criado."
  else
    echo "  Banco $db já existe."
  fi
done

echo ""
echo "Reiniciando as APIs para aplicar as migrations..."
docker compose restart identity-api properties-api analysis-api

echo ""
echo "Pronto. Aguarde alguns segundos e verifique os logs:"
echo "  docker compose logs -f analysis-api"
echo "  (deve aparecer 'Migrations aplicadas com sucesso')"
