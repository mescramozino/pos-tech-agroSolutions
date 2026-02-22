# Collections das APIs – AgroSolutions IoT

Este diretório é destinado a **collections** (Postman, Insomnia, Thunder Client, etc.) para testar as APIs do projeto.

## APIs do sistema

| API        | Base URL (local)   | Descrição                          |
|------------|--------------------|------------------------------------|
| Identity   | http://localhost:5001 | Autenticação (login, registro)   |
| Properties | http://localhost:5002 | Propriedades e talhões           |
| Ingestion  | http://localhost:5003 | Ingestão de dados de sensores    |
| Analysis   | http://localhost:5004 | Leituras, status e alertas       |

## Estrutura sugerida

- `identity.json` (ou `Identity.postman_collection.json`) – auth/register, auth/login
- `properties.json` – propriedades, talhões (CRUD)
- `ingestion.json` – POST /api/ingestion/sensors
- `analysis.json` – alertas, leituras, status de talhão

## Referência dos contratos

Consulte **docs/contratos.md** para os payloads e respostas de cada endpoint.

## Variáveis de ambiente (exemplo Postman)

- `base_url_identity`: http://localhost:5001  
- `base_url_properties`: http://localhost:5002  
- `base_url_ingestion`: http://localhost:5003  
- `base_url_analysis`: http://localhost:5004  
- `token`: preenchido após login (usar em Authorization Bearer nas APIs que exigem JWT)
