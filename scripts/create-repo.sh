#!/bin/bash
# Cria um repositório no GitHub e faz o primeiro push (ou só configura o remote).
# Uso: ./create-repo.sh [NOME_REPO] [public|private]
# Pré-requisitos: Git; GitHub CLI (gh) OU variável GITHUB_TOKEN.
# Exemplo: ./create-repo.sh agrosolutions-iot public

set -e

REPO_NAME="${1:-$(basename "$(pwd)")}"
VISIBILITY="${2:-public}"
DESCRIPTION="MVP Plataforma IoT AgroSolutions - Hackathon 8NETT"

if ! command -v git &>/dev/null; then
  echo "Erro: Git nao encontrado. Instale em https://git-scm.com" >&2
  exit 1
fi

REPO_URL=""

if command -v gh &>/dev/null; then
  echo "Usando GitHub CLI (gh)..."
  if gh repo create "$REPO_NAME" --"$VISIBILITY" --description "$DESCRIPTION" --source=. --remote=origin --push 2>/dev/null; then
    echo "Repositorio criado e codigo enviado com sucesso."
    exit 0
  fi
  if gh repo view "$REPO_NAME" &>/dev/null; then
    REPO_URL="$(gh repo view "$REPO_NAME" --json url -q .url)"
    [ "${REPO_URL#*.git}" = "$REPO_URL" ] && REPO_URL="${REPO_URL}.git"
    echo "Repositorio '$REPO_NAME' ja existe. Configurando remote e enviando..."
  else
    echo "Falha ao criar repositorio. Verifique: gh auth status" >&2
    exit 1
  fi
else
  if [ -z "$GITHUB_TOKEN" ]; then
    echo "Erro: GitHub CLI (gh) nao encontrado e GITHUB_TOKEN nao definido." >&2
    echo "Instale gh (https://cli.github.com) ou defina GITHUB_TOKEN." >&2
    exit 1
  fi
  echo "Usando API do GitHub com GITHUB_TOKEN..."
  PRIVATE="false"
  [ "$VISIBILITY" = "private" ] && PRIVATE="true"
  RESP=$(curl -s -w "\n%{http_code}" -X POST "https://api.github.com/user/repos" \
    -H "Authorization: token $GITHUB_TOKEN" \
    -H "Accept: application/vnd.github.v3+json" \
    -d "{\"name\":\"$REPO_NAME\",\"description\":\"$DESCRIPTION\",\"private\":$PRIVATE}")
  HTTP_CODE=$(echo "$RESP" | tail -n1)
  BODY=$(echo "$RESP" | sed '$d')
  if [ "$HTTP_CODE" = "201" ]; then
    REPO_URL=$(echo "$BODY" | grep -o '"clone_url": *"[^"]*"' | head -1 | sed 's/.*: *"\(.*\)".*/\1/')
    echo "Repositorio criado: $REPO_URL"
  elif [ "$HTTP_CODE" = "422" ]; then
    LOGIN=$(curl -s -H "Authorization: token $GITHUB_TOKEN" "https://api.github.com/user" | grep -o '"login": *"[^"]*"' | sed 's/.*: *"\(.*\)".*/\1/')
    REPO_URL="https://github.com/${LOGIN}/${REPO_NAME}.git"
    echo "Repositorio '$REPO_NAME' ja existe. URL: $REPO_URL"
  else
    echo "Falha na API GitHub (HTTP $HTTP_CODE)." >&2
    exit 1
  fi
fi

[ -z "$REPO_URL" ] && { echo "Nao foi possivel obter a URL do repositorio." >&2; exit 1; }
[ "${REPO_URL#*.git}" = "$REPO_URL" ] && REPO_URL="${REPO_URL}.git"

if [ ! -d .git ]; then
  echo "Inicializando repositorio Git local..."
  git init
  git branch -M main
  git add .
  git commit -m "Initial commit: AgroSolutions IoT Hackathon 8NETT"
fi

git remote remove origin 2>/dev/null || true
git remote add origin "$REPO_URL"
git add .
git diff --staged --quiet || git commit -m "Initial commit: AgroSolutions IoT Hackathon 8NETT"
echo "Enviando para origin main..."
git push -u origin main
echo "Concluido. Repositorio: $REPO_URL"
