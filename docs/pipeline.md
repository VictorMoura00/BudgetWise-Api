# BudgetWise — Guia de Pipeline

## Visão Geral

| Responsabilidade | Ferramenta | Observações |
|-----------------|------------|-------------|
| Infraestrutura local | Docker Compose | PostgreSQL, pgAdmin, Seq |
| Execução em desenvolvimento | `dotnet run` | Conecta à infra do Compose |
| Testes unitários | `dotnet test` | Sem dependências externas |
| Testes de integração | `dotnet test` | Testcontainers sobe um PostgreSQL isolado automaticamente |
| Imagem de publicação | `docker build` | Testes unitários são portão de entrada no Dockerfile |
| CI/CD | GitHub Actions | Ver `.github/workflows/` |

---

## 1. Pré-requisitos

| Ferramenta | Versão mínima |
|------------|--------------|
| .NET SDK | 10.0 |
| Docker Desktop | 24+ |
| Git | qualquer recente |

Copie o template de variáveis de ambiente antes da primeira execução:

```bash
cp .env.example .env
```

Os valores padrão do `.env.example` funcionam sem nenhuma alteração para desenvolvimento local. Troque as senhas em ambientes compartilhados ou de produção.

---

## 2. Desenvolvimento — Modo Só-Infra (Recomendado)

Sobe apenas os serviços de infraestrutura e executa a API na máquina host. Oferece o ciclo de feedback mais rápido e permite debug completo pela IDE.

```bash
# Sobe PostgreSQL, pgAdmin e Seq
docker compose up -d postgres pgadmin seq

# Executa a API (hot-reload habilitado)
# As migrations pendentes e o seeder de categorias são aplicados automaticamente no startup
dotnet run --project src/BudgetWise.Api
```

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| API (Scalar UI) | http://localhost:5000/scalar | — |
| pgAdmin | http://localhost:5050 | admin@budgetwise.dev / admin |
| Seq (logs) | http://localhost:8081 | admin / SeqAdmin123! |

Para parar a infra ao terminar:

```bash
docker compose down
```

> **Dica:** Os dados ficam em volumes Docker nomeados (`postgres-data` etc.). Para apagar tudo e começar do zero, use `docker compose down -v`.

---

## 3. Desenvolvimento — Modo Stack Completo

Compila e executa a API dentro do Docker junto com a infraestrutura. Útil para validar a configuração do Docker ou testar em ambiente containerizado.

```bash
# Compila e sobe tudo
docker compose up -d --build

# Acompanhar os logs da API
docker compose logs -f api
```

> **Atenção:** Neste modo os testes unitários do Dockerfile são executados como parte do `docker compose up --build`. Se qualquer teste falhar, o build falha e o container da API não é iniciado.

---

## 4. Execução de Testes

### Testes unitários (sem dependências externas)

```bash
dotnet test tests/BudgetWise.UnitTests
```

Com saída detalhada:

```bash
dotnet test tests/BudgetWise.UnitTests \
  --logger "console;verbosity=normal"
```

Com cobertura de código:

```bash
dotnet test tests/BudgetWise.UnitTests \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage
```

### Testes de integração (Testcontainers — requer Docker em execução)

O Testcontainers sobe um container PostgreSQL isolado por sessão de testes; nenhuma infra manual é necessária.

```bash
dotnet test tests/BudgetWise.IntegrationTests
```

### Todos os testes

```bash
dotnet test
```

---

## 5. Build da Imagem de Publicação

O Dockerfile impõe um **portão de testes unitários**: o estágio `publish` herda do estágio `test`, portanto a imagem final é inalcançável se qualquer teste unitário falhar.

```
build → test (RUN dotnet test) → publish → final
```

### Construir a imagem localmente

```bash
docker build \
  -f src/BudgetWise.Api/Dockerfile \
  -t budgetwise-api:local \
  .
```

Se os testes unitários falharem, o `docker build` encerra com código de erro e nenhuma imagem é gerada.

### Build sem testes (apenas desenvolvimento — não usar em CI)

Aponte para o estágio `build` para pular o portão de testes:

```bash
docker build \
  -f src/BudgetWise.Api/Dockerfile \
  --target build \
  -t budgetwise-api:build-only \
  .
```

### Executar a imagem publicada

```bash
docker run --rm \
  -p 8080:8080 \
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5432;Database=budgetwise;Username=budgetwise;Password=budgetwise_dev" \
  -e Jwt__Secret="superSecretKeyForDev_MustBeAtLeast32Chars!" \
  -e Jwt__Issuer="budgetwise-api" \
  -e Jwt__Audience="budgetwise-client" \
  -e Seq__ServerUrl="http://host.docker.internal:5341" \
  budgetwise-api:local
```

---

## 6. Migrations do EF Core

> **A aplicação já sobe as migrations automaticamente no startup** (`MigrateAsync` + seeder de categorias). Não é necessário rodar `database update` manualmente — basta a API iniciar com o banco acessível.

Os comandos abaixo são usados apenas para **criar novas migrations** durante o desenvolvimento:

```bash
# Criar uma nova migration após alterar o modelo
dotnet ef migrations add <NomeDaMigration> \
  --project src/BudgetWise.Infrastructure \
  --startup-project src/BudgetWise.Api

# Revisar o SQL gerado antes de commitar
dotnet ef migrations script \
  --project src/BudgetWise.Infrastructure \
  --startup-project src/BudgetWise.Api \
  --idempotent

# Aplicar manualmente (caso necessário fora do startup da API)
dotnet ef database update \
  --project src/BudgetWise.Infrastructure \
  --startup-project src/BudgetWise.Api
```

---

## 7. Outros Comandos Úteis

```bash
# Compilar a solução (verificar erros de compilação)
dotnet build

# Verificar formatação (falha se houver problemas)
dotnet format --verify-no-changes

# Aplicar formatação automaticamente
dotnet format
```

---

## 8. Referência de Variáveis de Ambiente

| Variável | Padrão | Descrição |
|----------|--------|-----------|
| `POSTGRES_PASSWORD` | `budgetwise_dev` | Senha do PostgreSQL |
| `JWT_SECRET` | `superSecretKeyForDev_MustBeAtLeast32Chars!` | Chave de assinatura do JWT (mín. 32 caracteres) |
| `PGADMIN_EMAIL` | `admin@budgetwise.dev` | E-mail de login do pgAdmin |
| `PGADMIN_PASSWORD` | `admin` | Senha de login do pgAdmin |
| `SEQ_ADMIN_PASSWORD` | `SeqAdmin123!` | Senha do administrador do Seq |

Essas variáveis são lidas pelo `docker-compose.yml` a partir do arquivo `.env`. A própria API lê `ConnectionStrings__Default`, `Jwt__*` e `Seq__ServerUrl` via `appsettings.json` ou sobrescrita por variável de ambiente.
