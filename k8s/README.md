# Kubernetes – AgroSolutions IoT

Manifests para rodar a aplicação no Kubernetes (item 2 dos entregáveis: demonstração da infraestrutura com K8s, Grafana e Prometheus).

**Banco de dados:** As APIs (Identity, Properties, Analysis) aplicam **EF Core Migrations** na subida (`Database.MigrateAsync()`). O schema é criado/atualizado automaticamente quando os pods iniciam. Identity e Properties inserem os dados iniciais (produtor, Fazenda Modelo, talhões) após a migração. Nenhum script extra é necessário para criar tabelas.

## Pré-requisitos

- **kubectl** instalado ([instalar kubectl](https://kubernetes.io/docs/tasks/tools/))
- **Docker** instalado (para Kind ou para build das imagens)
- **Cluster:** Kind ou Minikube (local) ou cluster na nuvem (EKS, AKS, GKE)
- **Imagens Docker** das APIs e do dashboard (build local ou GHCR)

## Script PowerShell (Windows)

Na **raiz do repositório**, execute para subir cluster (Kind), build, carga de imagens e apply dos manifests em um único comando:

```powershell
.\scripts\k8s-up.ps1
```

Parâmetros opcionais: `-ClusterType Kind|Minikube`, `-ClusterName`, `-SkipClusterCreate`, `-SkipBuild`, `-SkipApply`. Ex.: `.\scripts\k8s-up.ps1 -ClusterType Minikube` ou `.\scripts\k8s-up.ps1 -SkipClusterCreate -SkipBuild` (só aplica manifests).

### Derrubar e começar de novo (arquivos atualizados)

Para remover tudo (namespace e recursos) e subir novamente com os manifests atualizados, na **raiz do repositório**:

```powershell
.\scripts\k8s-down.ps1
.\scripts\k8s-up.ps1
```

Só remove o namespace (padrão `agrosolutions`); o cluster (Kind/Minikube ou Docker Desktop) continua. Para deletar também o cluster Kind: `.\scripts\k8s-down.ps1 -DeleteCluster -ClusterType Kind`.

## Como subir o Kubernetes (passo a passo)

### 1. Criar o cluster (escolha uma opção)

**Kind (recomendado no Windows/Linux):**

```bash
# Instalar Kind: https://kind.sigs.k8s.io/docs/user/installation/
kind create cluster --name agrosolutions
# Ou use o nome padrão "kind": kind create cluster
```

**Minikube:**

```bash
# Instalar: https://minikube.sigs.k8s.io/docs/start/
minikube start
minikube addons enable ingress   # opcional
```

Confirme que o cluster está ativo: `kubectl cluster-info`

### 2. Build das imagens e carregar no cluster

Na **raiz do repositório** (onde está o `docker-compose.yml`):

```bash
docker build -t agrosolutions/identity-api:latest ./identity
docker build -t agrosolutions/properties-api:latest ./properties
docker build -t agrosolutions/ingestion-api:latest ./ingestion
docker build -t agrosolutions/analysis-api:latest ./analysis
docker build -t agrosolutions/dashboard:latest ./dashboard
```

**Se estiver usando Kind** (nome do cluster `kind` ou `agrosolutions`):

```bash
kind load docker-image agrosolutions/identity-api:latest
kind load docker-image agrosolutions/properties-api:latest
kind load docker-image agrosolutions/ingestion-api:latest
kind load docker-image agrosolutions/analysis-api:latest
kind load docker-image agrosolutions/dashboard:latest
```

**Se estiver usando Minikube:** use o Docker do Minikube antes do build: `eval $(minikube docker-env)` e depois refaça os `docker build` acima (ou use `minikube image load ...`).

### 3. Aplicar os manifests

Ainda na raiz do repositório:

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/postgres-pv.yaml
kubectl apply -f k8s/secrets.yaml -n agrosolutions
kubectl apply -f k8s/configmap-postgres-init.yaml -n agrosolutions
kubectl apply -f k8s/postgres.yaml -n agrosolutions
kubectl apply -f k8s/rabbitmq.yaml -n agrosolutions
kubectl wait --for=condition=ready pod -l app=postgres -n agrosolutions --timeout=120s
kubectl apply -f k8s/identity-api.yaml -n agrosolutions
kubectl apply -f k8s/properties-api.yaml -n agrosolutions
kubectl apply -f k8s/ingestion-api.yaml -n agrosolutions
kubectl apply -f k8s/analysis-api.yaml -n agrosolutions
kubectl apply -f k8s/dashboard.yaml -n agrosolutions
kubectl apply -f k8s/prometheus.yaml -n agrosolutions
kubectl apply -f k8s/grafana.yaml -n agrosolutions
kubectl apply -f k8s/ingress.yaml -n agrosolutions
```

### 4. Verificar e acessar

```bash
kubectl get pods -n agrosolutions
kubectl get svc -n agrosolutions
```

Quando todos os pods estiverem `Running`:

- **Dashboard:** NodePort 30080 → `http://localhost:30080` (ou o IP do nó)
- **Prometheus:** NodePort 30090
- **Grafana:** NodePort 30300 (login: **admin** / **admin**)

No **Minikube:** `minikube service dashboard-nodeport -n agrosolutions --url` para obter a URL do dashboard.

**Se os pods ficarem Unhealthy (Postgres com PVC Pending):** aplique o PV e force a recriação do PVC: `kubectl delete pvc postgres-pvc -n agrosolutions`, depois `kubectl apply -f k8s/postgres-pv.yaml` e `kubectl apply -f k8s/postgres.yaml -n agrosolutions`. Em seguida reinicie os deployments das APIs: `kubectl rollout restart deployment -n agrosolutions`.

---

## Ordem de aplicação (referência)

Aplique os arquivos na ordem abaixo (ou use `kubectl apply -f k8s/ -n agrosolutions` após criar o namespace).

```bash
# 1. Namespace e segredos
kubectl apply -f namespace.yaml
kubectl apply -f secrets.yaml -n agrosolutions
kubectl apply -f configmap-postgres-init.yaml -n agrosolutions

# 2. Infraestrutura (PV do Postgres, depois Postgres e RabbitMQ)
kubectl apply -f postgres-pv.yaml
kubectl apply -f postgres.yaml -n agrosolutions
kubectl apply -f rabbitmq.yaml -n agrosolutions

# 3. Aguardar Postgres ficar pronto (opcional)
kubectl wait --for=condition=ready pod -l app=postgres -n agrosolutions --timeout=120s

# 4. APIs e dashboard
kubectl apply -f identity-api.yaml -n agrosolutions
kubectl apply -f properties-api.yaml -n agrosolutions
kubectl apply -f ingestion-api.yaml -n agrosolutions
kubectl apply -f analysis-api.yaml -n agrosolutions
kubectl apply -f dashboard.yaml -n agrosolutions

# 5. Observabilidade (Prometheus e Grafana)
kubectl apply -f prometheus.yaml -n agrosolutions
kubectl apply -f grafana.yaml -n agrosolutions

# 6. Acesso externo (NodePort + opcional Ingress)
kubectl apply -f ingress.yaml -n agrosolutions
```

## Build das imagens e uso no cluster

As APIs e o dashboard usam a imagem `agrosolutions/<serviço>:latest`. Escolha uma das opções:

### Opção A: Build local e carregar no kind/minikube

```bash
# Na raiz do repositório
docker build -t agrosolutions/identity-api:latest ./identity
docker build -t agrosolutions/properties-api:latest ./properties
docker build -t agrosolutions/ingestion-api:latest ./ingestion
docker build -t agrosolutions/analysis-api:latest ./analysis
docker build -t agrosolutions/dashboard:latest ./dashboard

# kind
kind load docker-image agrosolutions/identity-api:latest
kind load docker-image agrosolutions/properties-api:latest
kind load docker-image agrosolutions/ingestion-api:latest
kind load docker-image agrosolutions/analysis-api:latest
kind load docker-image agrosolutions/dashboard:latest

# minikube
minikube image load agrosolutions/identity-api:latest
minikube image load agrosolutions/properties-api:latest
# ... (repetir para as outras imagens)
```

### Opção B: Push para GHCR e usar no cluster

Após rodar o workflow `cd.yml`, as imagens estarão em `ghcr.io/<SEU_USER>/<REPO>/identity-api:latest` etc. Edite os manifests em `k8s/` e troque `agrosolutions/identity-api:latest` pelo endereço completo do GHCR e use `imagePullPolicy: Always` (ou omita para Always quando usar tag :latest).

## Acesso à aplicação

- **Dashboard (NodePort):** `http://<IP_DO_NODE>:30080`  
  Ex.: minikube: `minikube service dashboard-nodeport -n agrosolutions --url`  
  kind: use o IP do nó (ex.: do host da Docker).

- **Prometheus (NodePort):** `http://<IP_DO_NODE>:30090`  
- **Grafana (NodePort):** `http://<IP_DO_NODE>:30300` (login: admin / admin)

Se tiver **Ingress** habilitado (ex.: `minikube addons enable ingress` ou ingress-nginx no kind), acesse pela URL do Ingress (ex.: `http://localhost` ou o IP do ingress controller).

## Grafana – data source e dashboard

1. Acesse Grafana (NodePort 30300).
2. **Configuration → Data sources → Add data source:** escolha **Prometheus**.
3. **URL:** `http://prometheus:9090` (serviço no mesmo namespace).
4. **Save & Test.**
5. **Dashboards → New → Import:** use um dashboard existente (ex.: ID 3662 para “ASP.NET Core”) ou crie um painel com métricas como `http_requests_received_total` ou `dotnet_*`.

## Evidências para o item 2

Para a demonstração da infraestrutura, capture:

1. **Kubernetes:** `kubectl get pods,svc -n agrosolutions` com todos os pods `Running`.
2. **Aplicação:** tela do dashboard no browser (login, propriedades, talhões).
3. **Prometheus:** tela **Status → Targets** com os targets das APIs em “UP”.
4. **Grafana:** um dashboard exibindo métricas das APIs (ex.: requisições por segundo).

Documentação detalhada em `docs/INFRAESTRUTURA_K8S.md`.
