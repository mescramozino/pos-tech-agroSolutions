# Passo a passo – CD (Continuous Deployment)

Guia para configurar e usar o pipeline de CD do AgroSolutions IoT: **build de imagens**, **push para o GitHub Container Registry (GHCR)** e **deploy em Kubernetes**.

---

## 1. O que o CD faz hoje

| Etapa | Onde | Descrição |
|-------|------|-----------|
| **Build & push** | Job `build-and-push` | Constrói as 5 imagens (identity, properties, ingestion, analysis, dashboard) e envia para `ghcr.io/SEU_USER/SEU_REPO/...` |
| **Deploy (Kind)** | Job `deploy` | Cria um cluster Kind na runner, substitui as imagens nos manifests por GHCR, aplica os manifests e faz o deploy (para demonstração/CI). |

O workflow dispara em **push** nas branches **main** ou **master**.

---

## 2. Pré-requisitos no repositório

### 2.1 Permissões do GITHUB_TOKEN

1. No GitHub, abra o repositório.
2. **Settings** → **Actions** → **General**.
3. Em **Workflow permissions**, marque **Read and write permissions**.
4. Salve.

Isso permite que o workflow faça **push** das imagens para o GHCR.

### 2.2 Branch principal

O CD roda em push em `main` ou `master`. Se a branch principal for outra (ex.: `develop`), edite o `on.push.branches` em `.github/workflows/cd.yml`:

```yaml
on:
  push:
    branches: [main, master, develop]
```

---

## 3. Passo a passo – primeira execução

### Passo 1: Fazer push para a branch principal

Depois de configurar as permissões (item 2.1), faça um push para `main` (ou `master`):

```bash
git add .
git commit -m "ci: habilita CD com build e deploy"
git push origin main
```

### Passo 2: Acompanhar a execução

1. Abra o repositório no GitHub.
2. Vá em **Actions**.
3. Clique no workflow **CD** e no run mais recente.
4. Verifique os jobs:
   - **build-and-push**: build e push das 5 imagens para o GHCR.
   - **deploy**: criação do cluster Kind e deploy dos manifests.

### Passo 3: Onde ficam as imagens

As imagens são publicadas em:

- **GHCR:** https://github.com/SEU_USER/SEU_REPO/pkgs/container/SEU_REPO%2Fidentity-api (e as demais).

Substitua `SEU_USER` e `SEU_REPO` pelo dono e nome do repositório (ex.: `pos-tech-agroSolutions`).

Por padrão as imagens podem ser **privadas**. Para deixar públicas: **Packages** → escolha o pacote → **Package settings** → **Change visibility** → **Public**.

---

## 4. Deploy em um cluster “de verdade” (opcional)

O job `deploy` atual usa **Kind** dentro da runner (serve para demonstração e para o pipeline ficar verde). Para fazer deploy em um cluster na nuvem (EKS, AKS, GKE) ou local (minikube com kubeconfig):

### Opção A: Usar as imagens do GHCR no seu cluster

1. Build e push já estão no pipeline; use as imagens do GHCR.
2. No seu cluster, crie o secret para o registry (para imagens privadas):

   ```bash
   kubectl create secret docker-registry ghcr-secret \
     --docker-server=ghcr.io \
     --docker-username=SEU_USER \
     --docker-password=SEU_PAT \
     -n agrosolutions
   ```

   (Substitua `SEU_USER` e use um Personal Access Token com `read:packages` como `SEU_PAT`.)

3. Atualize os manifests em `k8s/` para usar o GHCR e o secret. Exemplo para identity-api:

   - Troque a imagem de `agrosolutions/identity-api:latest` para `ghcr.io/SEU_USER/SEU_REPO/identity-api:latest`.
   - No Deployment, em `spec.template.spec` adicione:
     ```yaml
     imagePullSecrets:
       - name: ghcr-secret
     ```

4. Aplique os manifests na ordem indicada em `k8s/README.md`.

### Opção B: Deploy automático pelo GitHub Actions (cluster externo)

Para o próprio workflow aplicar no seu cluster:

1. Gere um **kubeconfig** com acesso ao cluster (ex.: arquivo que você usa com `kubectl`).
2. No repositório: **Settings** → **Secrets and variables** → **Actions**.
3. Crie um secret, por exemplo **KUBECONFIG**, com o conteúdo do kubeconfig (conteúdo do arquivo inteiro).
4. No workflow, adicione um **novo job** (ou condição) que:
   - Use `KUBECONFIG` para configurar o `kubectl`.
   - Substitua as imagens nos manifests por `ghcr.io/OWNER/REPO/...`.
   - Crie o secret `ghcr-secret` no namespace `agrosolutions` (com token para GHCR).
   - Execute `kubectl apply -f k8s/` na ordem correta.

Assim o CD fica com: **build + push** (sempre) e **deploy no seu cluster** (quando o secret existir).

---

## 5. Resumo rápido

| O que fazer | Onde |
|-------------|------|
| Dar permissão de escrita ao GITHUB_TOKEN | Settings → Actions → General |
| Rodar o CD | Push em `main` ou `master` |
| Ver execução | Actions → CD |
| Ver imagens | GitHub → Packages (GHCR) |
| Deploy em cluster real | Usar imagens GHCR + secret no cluster ou job extra com KUBECONFIG |

---

## 6. Referências

- [k8s/README.md](../k8s/README.md) – Ordem de aplicação dos manifests e uso das imagens.
- [INFRAESTRUTURA_K8S.md](INFRAESTRUTURA_K8S.md) – Infraestrutura e observabilidade no Kubernetes.
