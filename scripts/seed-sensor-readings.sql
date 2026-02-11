-- Popula a tabela SensorReadings no banco analysis_db (PostgreSQL).
-- Use quando quiser inserir dados diretamente no banco, sem passar pela API de Ingestão/RabbitMQ.
--
-- Pré-requisitos:
--   1. Obter os IDs dos talhões (PlotId). Ex.: GET /api/properties e GET /api/properties/{id}/plots (com login).
--   2. Substituir 'SEU_PLOT_ID_AQUI' abaixo pelo GUID do talhão (ex.: '11111111-2222-3333-4444-555555555555').
--
-- Uso:
--   psql -h localhost -p 5432 -U agro -d analysis_db -f scripts/seed-sensor-readings.sql

INSERT INTO "SensorReadings" ("Id", "PlotId", "Type", "Value", "Timestamp", "IngestedAt")
SELECT
    gen_random_uuid(),
    'SEU_PLOT_ID_AQUI'::uuid,
    type,
    value,
    ts,
    now() AT TIME ZONE 'UTC'
FROM (
    SELECT 'moisture' AS type, (20 + random() * 60)::double precision AS value, ts
    FROM generate_series(
        (now() AT TIME ZONE 'UTC') - interval '30 days',
        (now() AT TIME ZONE 'UTC'),
        interval '4 hours'
    ) AS t(ts)
    UNION ALL
    SELECT 'temperature', (18 + random() * 15)::double precision, ts
    FROM generate_series(
        (now() AT TIME ZONE 'UTC') - interval '30 days',
        (now() AT TIME ZONE 'UTC'),
        interval '4 hours'
    ) AS t(ts)
) AS data;
