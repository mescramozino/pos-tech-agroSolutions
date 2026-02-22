# Links e acessos – Kubernetes AgroSolutions

Referência de URLs, usuários/senhas e conexão aos bancos de dados quando o stack está rodando no Kubernetes (namespace `agrosolutions`).

---

## 1. Como acessar (NodePort vs Ingress)

- **NodePort:** use `<NODE_IP>:<PORT>`. No **Docker Desktop Kubernetes**, o NODE_IP costuma ser `localhost`. No **Kind** ou **Minikube**, use o IP do nó (ex.: `minikube ip` ou o IP retornado por `kubectl get nodes -o wide`).
- **Ingress:** se o Ingress estiver configurado, acesse por `http://<INGRESS_IP>` (path `/` vai para o dashboard). O proxy do dashboard redireciona `/api/*` para as APIs.

---

## 2. APIs e serviços públicos (NodePort)

| Serviço          | NodePort | URL (substitua `<NODE_IP>` por `localhost` ou IP do nó) |
|------------------|----------|--------------------------------------------------------|
| **Dashboard**    | 30080    | `http://<NODE_IP>:30080`                               |
| **Identity API** | 30001    | `http://<NODE_IP>:30001`                               |
| **Properties API** | 30002  | `http://<NODE_IP>:30002`                               |
| **Ingestion API**  | 30003  | `http://<NODE_IP>:30003`                               |
| **Analysis API**   | 30004  | `http://<NODE_IP>:30004`                               |
| **Prometheus**     | 30090  | `http://<NODE_IP>:30090`                               |
| **Grafana**        | 30300  | `http://<NODE_IP>:30300`                               |

### Acesso às APIs via Dashboard (proxy)

Com o dashboard em `http://<NODE_IP>:30080`, o frontend chama as APIs pelos paths abaixo (o proxy do dashboard encaminha para os serviços no cluster):

| Path (no browser/API) | Serviço de destino |
|------------------------|---------------------|
| `/api/identity`        | Identity API        |
| `/api/properties`      | Properties API      |
| `/api/plots`           | Properties API      |
| `/api/ingestion`       | Ingestion API       |
| `/api/analysis`        | Analysis API        |
| `/api/weather`         | Analysis API        |

Exemplo: `http://<NODE_IP>:30080/api/identity/health` → Identity API.

---

## 3. Usuários e senhas

| Recurso    | Usuário | Senha  | Observação |
|------------|---------|--------|------------|
| **Dashboard (login)** | `produtor@agro.local` | `Senha123!` | Usuário produtor criado no seed da Identity API; use para fazer login no dashboard. |
| **PostgreSQL** (todos os DBs) | `agro` | `secret` | Mesmo usuário para `identity_db`, `properties_db`, `analysis_db` e `postgres`. |
| **RabbitMQ** (AMQP + Management UI) | `agro` | `secret` | Management UI só acessível via port-forward (ver seção 5). |
| **Grafana** | `admin` | `admin` | Trocar no primeiro login. |
| **JWT** | — | — | Chave usada pelas APIs para tokens: `AgroSolutionsIdentitySecretKeyMin32Chars!` (não é login de usuário). |

---

## 4. DBeaver – conexão aos bancos PostgreSQL

O serviço Postgres no Kubernetes é **ClusterIP** (não tem NodePort). Para conectar o DBeaver na sua máquina, use **port-forward** e depois configure as conexões abaixo.

### 4.1. Abrir o túnel (port-forward)

Em um terminal, deixe rodando:

```bash
kubectl port-forward svc/postgres 5432:5432 -n agrosolutions
```

Mantenha o comando ativo enquanto usar o DBeaver. Conexão: **Host** `localhost`, **Port** `5432`.

### 4.2. Conexões no DBeaver

Use **Driver:** PostgreSQL. Para cada banco, crie uma conexão com os dados abaixo (com o port-forward ativo).

| Campo      | Valor        |
|-----------|--------------|
| **Host**  | `localhost`  |
| **Port**  | `5432`       |
| **Username** | `agro`    |
| **Password** | `secret`    |

**Databases disponíveis:**

| Database       | Uso principal        |
|----------------|----------------------|
| `postgres`     | Banco padrão (init)  |
| `identity_db`  | Identity API         |
| `properties_db`| Properties API       |
| `analysis_db`  | Analysis API         |

**Resumo por conexão:**

- **Identity:** Host `localhost`, Port `5432`, Database `identity_db`, User `agro`, Password `secret`
- **Properties:** Host `localhost`, Port `5432`, Database `properties_db`, User `agro`, Password `secret`
- **Analysis:** Host `localhost`, Port `5432`, Database `analysis_db`, User `agro`, Password `secret`

**JDBC URL (alternativa):**

```
jdbc:postgresql://localhost:5432/identity_db
jdbc:postgresql://localhost:5432/properties_db
jdbc:postgresql://localhost:5432/analysis_db
```

---

## 5. RabbitMQ Management UI (opcional)

O RabbitMQ não está exposto por NodePort. Para acessar a interface web (porta 15672) na sua máquina:

```bash
kubectl port-forward svc/rabbitmq 15672:15672 -n agrosolutions
```

Depois abra no navegador: **`http://localhost:15672`**  
Login: **agro** / **secret**.

---

## 6. Resumo rápido (Docker Desktop com NodePort em localhost)

- **Dashboard:** http://localhost:30080 — login: **produtor@agro.local** / **Senha123!**  
- **Identity API:** http://localhost:30001  
- **Properties API:** http://localhost:30002  
- **Ingestion API:** http://localhost:30003  
- **Analysis API:** http://localhost:30004  
- **Prometheus:** http://localhost:30090  
- **Grafana:** http://localhost:30300 (admin / admin)  
- **Postgres (DBeaver):** port-forward `5432:5432` → localhost:5432, user `agro`, password `secret`, databases `identity_db`, `properties_db`, `analysis_db`  
- **RabbitMQ Management:** port-forward `15672:15672` → http://localhost:15672 (agro / secret)

---

## 7. Ver métricas (Prometheus e Grafana)

- **Prometheus:** acesse http://localhost:30090 → aba **Graph**, na query use por exemplo `up` ou `rate(http_requests_received_total[5m])` e clique **Execute**.
- **Grafana:** acesse http://localhost:30300 (admin / admin). O datasource **Prometheus** já está configurado. Para ver as APIs:
  - **Dashboards** → pasta **AgroSolutions** → dashboard **APIs AgroSolutions** (request rate por API e status “up”).
  - Ou **Explore** → selecione **Prometheus** e rode uma query como `http_requests_received_total`.
