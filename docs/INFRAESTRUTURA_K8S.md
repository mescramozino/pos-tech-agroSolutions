# Infraestrutura Kubernetes e Observabilidade (Item 2)

Este documento descreve como subir a aplicação AgroSolutions IoT no Kubernetes e configurar Prometheus e Grafana para atender ao **item 2 dos entregáveis** (Demonstração da Infraestrutura).

---

## 1. O que foi implementado

- **Pasta `k8s/`** com manifests para o namespace `agrosolutions`:
  - **Namespace,** **Secrets** e **ConfigMap** (init do Postgres).
  - **Postgres** e **RabbitMQ** (Deployments + Services + PVCs onde aplicável).
  - **identity-api, properties-api, ingestion-api, analysis-api** e **dashboard** (Deployments + Services).
  - **Ingress** (regra para o dashboard) e **NodePorts** para dashboard, APIs, Prometheus e Grafana.
  - **Prometheus** (ConfigMap, Deployment, Service) com scrape das quatro APIs em `/metrics`.
  - **Grafana** (Deployment, Service, NodePort 30300).
- **APIs .NET:** endpoint **`/metrics`** (Prometheus) em todas as quatro APIs via pacote `prometheus-net.AspNetCore`.
- **Banco de dados:** Identity, Properties e Analysis aplicam **EF Core Migrations** na subida (`MigrateAsync()`); schema e seed são aplicados automaticamente ao iniciar os pods. Não é necessário executar migrations manualmente no deploy.

---

## 2. Como subir o cluster (minikube ou kind)

### Minikube

```bash
minikube start
minikube addons enable ingress   # opcional, para usar Ingress
```

### Kind

```bash
kind create cluster --name agrosolutions
# Ingress (opcional): seguir documentação do ingress-nginx para kind
```

---

## 3. Build das imagens e carregar no cluster

Na **raiz do repositório**:

```bash
docker build -t agrosolutions/identity-api:latest ./identity
docker build -t agrosolutions/properties-api:latest ./properties
docker build -t agrosolutions/ingestion-api:latest ./ingestion
docker build -t agrosolutions/analysis-api:latest ./analysis
docker build -t agrosolutions/dashboard:latest ./dashboard
```

**Kind:**

```bash
kind load docker-image agrosolutions/identity-api:latest --name agrosolutions
kind load docker-image agrosolutions/properties-api:latest --name agrosolutions
kind load docker-image agrosolutions/ingestion-api:latest --name agrosolutions
kind load docker-image agrosolutions/analysis-api:latest --name agrosolutions
kind load docker-image agrosolutions/dashboard:latest --name agrosolutions
```

**Minikube:**

```bash
eval $(minikube docker-env)
# Em seguida, refaça o build das imagens (ou use minikube image load ...)
```

---

## 4. Aplicar os manifests

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secrets.yaml -n agrosolutions
kubectl apply -f k8s/configmap-postgres-init.yaml -n agrosolutions
kubectl apply -f k8s/postgres.yaml -n agrosolutions
kubectl apply -f k8s/rabbitmq.yaml -n agrosolutions
# Aguardar Postgres (opcional): kubectl wait --for=condition=ready pod -l app=postgres -n agrosolutions --timeout=120s
kubectl apply -f k8s/identity-api.yaml -n agrosolutions
kubectl apply -f k8s/properties-api.yaml -n agrosolutions
kubectl apply -f k8s/ingestion-api.yaml -n agrosolutions
kubectl apply -f k8s/analysis-api.yaml -n agrosolutions
kubectl apply -f k8s/dashboard.yaml -n agrosolutions
kubectl apply -f k8s/prometheus.yaml -n agrosolutions
kubectl apply -f k8s/grafana.yaml -n agrosolutions
kubectl apply -f k8s/ingress.yaml -n agrosolutions
```

Ou, a partir da pasta `k8s/`:

```bash
for f in namespace.yaml secrets.yaml configmap-postgres-init.yaml postgres.yaml rabbitmq.yaml identity-api.yaml properties-api.yaml ingestion-api.yaml analysis-api.yaml dashboard.yaml prometheus.yaml grafana.yaml ingress.yaml; do
  kubectl apply -f "$f" -n agrosolutions 2>/dev/null || kubectl apply -f "$f"
done
```

---

## 5. Verificar pods e serviços

```bash
kubectl get pods -n agrosolutions
kubectl get svc -n agrosolutions
```

Todos os pods devem ficar `Running`. Se algum ficar em `ImagePullBackOff`, confira o nome da imagem e se ela foi carregada no cluster (kind/minikube) ou se o registry está acessível.

---

## 6. Acessar a aplicação e a observabilidade

- **Dashboard:** NodePort 30080  
  - Minikube: `minikube service dashboard-nodeport -n agrosolutions --url`  
  - Kind: `http://<IP_DO_NODE>:30080` (ex.: IP da máquina ou do container kind)
- **Prometheus:** NodePort 30090 → `http://<IP>:30090`
- **Grafana:** NodePort 30300 → `http://<IP>:30300` (login: **admin** / **admin**)

No Grafana: **Configuration → Data sources → Add data source → Prometheus.**  
URL: **`http://prometheus:9090`**. Save & Test.

Para um dashboard rápido: **Dashboards → New → Import** e use o ID **3662** (ASP.NET Core) ou crie um painel com a métrica `http_requests_received_total`.

---

## 7. Evidências para a banca (item 2)

1. **Kubernetes:** print de `kubectl get pods -n agrosolutions` e `kubectl get svc -n agrosolutions` com todos os pods Running.
2. **Aplicação:** print do dashboard no browser (login, listagem de propriedades/talhões).
3. **Prometheus:** print da página **Status → Targets** mostrando os targets das APIs em estado “UP”.
4. **Grafana:** print de um dashboard exibindo métricas das APIs (ex.: requisições HTTP).

Isso atende ao requisito do item 2: aplicação rodando em ambiente (K8s) e evidências de uso de Kubernetes, Grafana e Prometheus.
