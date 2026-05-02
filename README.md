# BudgetWise API

> API de organização financeira pessoal.
> Construída com **.NET 10**, **PostgreSQL** e Clean Architecture.

---

## Índice

1. [Visão Geral](#visão-geral)
2. [Funcionalidades](#funcionalidades)
3. [Stack Tecnológica](#stack-tecnológica)
4. [Arquitetura](#arquitetura)
5. [Endpoints](#endpoints)
6. [Começando](#começando)
7. [Configuração](#configuração)
8. [Testes](#testes)
9. [Infraestrutura Docker](#infraestrutura-docker)
10. [Observabilidade](#observabilidade)
11. [Roadmap](#roadmap)

---

## Visão Geral

O **BudgetWise** é uma API RESTful para organização financeira pessoal. O objetivo é oferecer uma base robusta para registro, categorização e consulta de transações financeiras — receitas e despesas — com filtros avançados, paginação e resumo financeiro.

O projeto serve como portfólio técnico demonstrando domínio de **Clean Architecture**, **ASP.NET Core Minimal APIs**, modelagem relacional consistente no PostgreSQL e autenticação interna com ASP.NET Core Identity. Um frontend Angular será desenvolvido em repositório separado e integrado via Docker Compose.

---

## Funcionalidades

### Autenticação
- Registro e login com ASP.NET Core Identity
- Tokens JWT com expiração configurável
- Refresh token com rotação obrigatória
- Bloqueio de contas inativas

### Transações
- CRUD completo com soft delete
- Tipos: `INCOME` e `EXPENSE`
- Meios de pagamento: PIX, Cartão de Crédito, Débito, Dinheiro, TED, Boleto
- Recorrências: Diária, Semanal, Mensal, Anual
- Confirmação unidirecional de transações
- Filtros avançados por tipo, categoria, data, valor e tags
- Paginação configurável (padrão: 20 itens por página)
- Resumo financeiro agregado por período

### Categorias
- CRUD com escopo por usuário
- Categorias do sistema (somente leitura para usuários)
- Soft delete via `is_active` — categorias desativadas permanecem vinculadas a transações existentes
- Unicidade case-insensitive por usuário

### Tags
- CRUD escopado por usuário
- Upsert automático por nome nas transações
- Sincronização total de tags no update (substituição)
- Limite de 10 tags por transação

### Grupos Familiares
- Criação de grupos com código de convite
- Entrada via código (case-insensitive)
- Gerenciamento de membros (OWNER / MEMBER)
- Regeneração de código de convite
- Limite de 5 grupos por usuário
- OWNER não pode sair sem transferir a propriedade

### Despesas Compartilhadas
- Divisão de despesas entre membros do grupo
- Soma dos participantes deve ser exatamente igual ao total
- Quitação individual unidirecional
- Apenas o participante ou o OWNER pode quitar

---

## Stack Tecnológica

| Camada | Tecnologia | Finalidade |
|---|---|---|
| Runtime | .NET 10 / C# 14 | SDK e runtime principal |
| HTTP | ASP.NET Core Minimal APIs | Roteamento e endpoints |
| ORM | Entity Framework Core + Npgsql | Acesso a dados |
| Banco | PostgreSQL 16 | Persistência relacional |
| Identidade | ASP.NET Core Identity | Gerenciamento de usuários |
| Auth | JWT Bearer | Autenticação stateless |
| Validação | FluentValidation | Validação de DTOs |
| Mapeamento | Mapster | Mapeamento de objetos (instalado, uso manual/DTOs) |
| Logging | Serilog + Seq | Logging estruturado |
| Documentação | Scalar | OpenAPI UI interativo |
| Testes | xUnit + Testcontainers | Testes de integração com PostgreSQL real |
| Containers | Docker Compose | Orquestração local |

---

## Arquitetura

O projeto segue **Clean Architecture** com dependências sempre apontando para o Domain:

```
API → Application → Domain
       Infrastructure → Domain
```

```
src/
├── BudgetWise.Domain/         # Entidades, enums, interfaces, exceções de domínio
├── BudgetWise.Application/    # Use cases, DTOs, validadores, mapeamentos
├── BudgetWise.Infrastructure/ # EF Core, Identity, repositórios, migrations, seeds
└── BudgetWise.Api/            # Endpoints, middleware, extensions, Program.cs

tests/
├── BudgetWise.UnitTests/      # xUnit + Moq + Bogus (sem banco, sem HTTP)
└── BudgetWise.IntegrationTests/ # Testcontainers + WebApplicationFactory
```

### Fluxo de uma requisição autenticada

```
Cliente HTTP
    ↓
JWT Middleware (valida token, extrai claims)
    ↓
Endpoint (API) — valida entrada, extrai userId, mapeia para DTO
    ↓
Use Case (Application) — aplica regra de negócio, chama repositório
    ↓
Repositório (Infrastructure) — consulta via EF Core filtrando por user_id
    ↓
PostgreSQL
    ↓
Resposta mapeada para DTO → Cliente HTTP (JSON)
```

---

## Endpoints

Base URL: `/api/v1`

| Módulo | Método | Rota |
|---|---|---|
| **Auth** | POST | `/auth/register` |
| | POST | `/auth/login` |
| | POST | `/auth/refresh` |
| **Categorias** | GET | `/categories` |
| | GET | `/categories/{id}` |
| | POST | `/categories` |
| | PUT | `/categories/{id}` |
| | DELETE | `/categories/{id}` |
| **Tags** | GET | `/tags` |
| | GET | `/tags/{id}` |
| | POST | `/tags` |
| | PUT | `/tags/{id}` |
| | DELETE | `/tags/{id}` |
| **Transações** | GET | `/transactions` |
| | GET | `/transactions/{id}` |
| | GET | `/transactions/summary` |
| | GET | `/transactions/dashboard` |
| | GET | `/transactions/monthly-summary` |
| | POST | `/transactions` |
| | PUT | `/transactions/{id}` |
| | PATCH | `/transactions/{id}/confirm` |
| | DELETE | `/transactions/{id}` |
| | POST | `/transactions/{id}/tags/{tagId}` |
| | DELETE | `/transactions/{id}/tags/{tagId}` |
| **Grupos Familiares** | GET | `/family-groups` |
| | GET | `/family-groups/{id}` |
| | POST | `/family-groups` |
| | PUT | `/family-groups/{id}` |
| | DELETE | `/family-groups/{id}` |
| | POST | `/family-groups/join` |
| | POST | `/family-groups/{id}/invite/regenerate` |
| | DELETE | `/family-groups/{id}/members/{userId}` |
| | POST | `/family-groups/{id}/leave` |
| **Despesas Compartilhadas** *(roadmap)* | GET | `/family-groups/{id}/shared-expenses` |
| | GET | `/shared-expenses/{id}` |
| | POST | `/family-groups/{id}/shared-expenses` |
| | PATCH | `/shared-expenses/{id}/settle` |
| | DELETE | `/shared-expenses/{id}` |
| **Admin** | GET | `/admin/users` |
| | GET | `/admin/users/{id}` |
| | PATCH | `/admin/users/{id}` |
| | PATCH | `/admin/users/{id}/toggle-status` |
| | PATCH | `/admin/users/{id}/unlock` |
| | PATCH | `/admin/users/{id}/role` |
| **Utilitários** | GET | `/health` |
| | GET | `/health/live` |
| | GET | `/health/ready` |
| | GET | `/scalar/v1` |

> Todos os endpoints exceto `/auth/register` e `/auth/login` requerem `Authorization: Bearer {token}`.

---

## Começando

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Subindo o ambiente

```bash
# Clone o repositório
git clone https://github.com/seu-usuario/BudgetWise.git
cd BudgetWise

# Configure as variáveis de ambiente
cp .env.example .env
# edite o .env com suas configurações

# Suba os serviços (PostgreSQL, pgAdmin, Seq)
docker compose up -d

# Aplique as migrations
dotnet ef database update --project src/BudgetWise.Infrastructure --startup-project src/BudgetWise.Api

# Rode a API
dotnet run --project src/BudgetWise.Api
```

### Serviços disponíveis

| Serviço | URL | Descrição |
|---|---|---|
| API | http://localhost:8080 | Endpoints da aplicação |
| Scalar (docs) | http://localhost:8080/scalar/v1 | Documentação interativa |
| Health Check | http://localhost:8080/health | Status da API e banco |
| pgAdmin | http://localhost:5050 | Administração do PostgreSQL |
| Seq | http://localhost:8081 | Visualização de logs |

---

## Configuração

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=budgetwise;Username=...;Password=..."
  },
  "Jwt": {
    "Secret": "...",
    "Issuer": "budgetwise-api",
    "Audience": "budgetwise-client",
    "AccessTokenExpirationMinutes": "15",
    "RefreshTokenExpirationDays": "7"
  },
  "Seq": {
    "ServerUrl": "http://localhost:5341"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://budgetwise.app"
    ]
  },
  "RateLimit": {
    "Auth": {
      "LoginPermitLimit": 5,
      "LoginWindowMinutes": 1,
      "RefreshPermitLimit": 10,
      "RefreshWindowMinutes": 1,
      "RegisterPermitLimit": 5,
      "RegisterWindowMinutes": 1
    }
  }
}
```

`appsettings.json` contém apenas a estrutura com valores vazios (versionado). Valores reais ficam em `appsettings.Development.json` (no `.gitignore`).

> **CORS:** se `Cors:AllowedOrigins` estiver vazio ou ausente, o middleware CORS não é registrado e requisições cross-origin serão bloqueadas pelo navegador.

### Rate Limiting

Os endpoints de autenticação (`/auth/register`, `/auth/login`, `/auth/refresh`) possuem rate limiting por IP utilizando `FixedWindowLimiter`. Os limites são configuráveis via `appsettings.json` na seção `RateLimit:Auth`. Em ambiente de testes os limites são ilimitados via `WebApplicationFactory`.

---

## Testes

```bash
# Todos os testes
dotnet test

# Apenas unitários
dotnet test tests/BudgetWise.UnitTests

# Apenas integração (requer Docker para Testcontainers)
dotnet test tests/BudgetWise.IntegrationTests
```

- **Unitários** — xUnit + Moq + Bogus. Sem banco, sem HTTP.
- **Integração** — Testcontainers.PostgreSql + WebApplicationFactory. PostgreSQL efêmero por test run.
- **Convenção de nome**: `Método_Cenário_ResultadoEsperado`

> **Cobertura atual (338 testes):**
> - 225 testes unitários
> - 113 testes de integração cobrindo: Auth, Categories, Tags, Transactions, Family Groups, Admin, Health Checks
>
> **Pendente:** Shared Expenses.

---

## Infraestrutura Docker

```bash
# Subir todos os serviços
docker compose up -d

# Verificar status
docker compose ps

# Parar tudo
docker compose down

# Parar e remover volumes
docker compose down -v
```

---

## Observabilidade

### Logging (Serilog + Seq)

Todos os logs são emitidos em JSON estruturado e enviados ao Seq. São registrados:

- Cada request HTTP com método, rota, status, duração e `userId`
- Falhas de autenticação e tentativas de acesso não autorizado
- Erros de validação com os campos que falharam
- Exceções não tratadas (sem stack trace em produção)
- Eventos de negócio: transação criada, membro adicionado, despesa quitada

### Padrão de erro (ProblemDetails — RFC 7807)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 422,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "amount": ["Amount must be greater than zero."],
    "type": ["Type must be INCOME or EXPENSE."]
  }
}
```

| Status | Quando |
|---|---|
| `200 OK` | Leitura ou atualização bem-sucedida |
| `201 Created` | Recurso criado |
| `204 No Content` | Exclusão bem-sucedida |
| `401 Unauthorized` | Token ausente, inválido ou expirado |
| `403 Forbidden` | Autenticado, mas sem permissão |
| `404 Not Found` | Recurso não encontrado (ou sem acesso — ownership) |
| `409 Conflict` | Conflito de unicidade |
| `422 Unprocessable Entity` | Falha de validação de negócio |
| `500 Internal Server Error` | Erro inesperado |

---

## Roadmap

### v1.0 — Uso individual ✅
- [x] Estrutura de solução com Clean Architecture
- [x] Infraestrutura: Identity, JWT, EF Core, Serilog, Health Checks, Docker Compose
- [x] Entidades e enums do domínio
- [x] Fluent API configurations + migrations
- [x] Use Cases de Auth (registro, login, refresh token)
- [x] Use Cases de Transações, Categorias, Tags
- [x] Testes unitários e de integração (Auth, Categories, Tags, Transactions, Family Groups, Admin)
- [x] Seed de categorias do sistema
- [x] Pipeline CI com GitHub Actions
- [x] Rate limiting nos endpoints de Auth
- [x] CORS configurável via `appsettings.json`
- [x] Serilog request logging com enriquecimento de contexto

### v2.0 — Dashboards e relatórios
- [x] Resumo financeiro por período (`/transactions/summary`)
- [x] Dashboard consolidado (`/transactions/dashboard`)
- [x] Evolução mensal (`/transactions/monthly-summary`)
- [ ] Relatório de gastos por categoria
- [ ] Relatório de gastos por tag
- [ ] Importação de extratos bancários (OFX/CSV)
- [ ] Notificações e alertas automáticos
- [ ] Frontend Angular (repositório separado)

### v3.0 — Uso compartilhado
- [x] Grupos Familiares (criação, convite, gerenciamento de membros)
- [~] Despesas Compartilhadas — entidades de domínio modeladas; use cases e endpoints pendentes
- [x] Testes de integração para Grupos Familiares
- [x] Testes de integração para Admin endpoints
- [ ] Testes de integração para Despesas Compartilhadas

---

## Convenções

- **Commits:** Conventional Commits — `feat`, `fix`, `refactor`, `test`, `docs`, `chore`
- **Branches:** `main` ← `develop` ← `feat/*` / `fix/*` / `chore/*`
- **PRs:** obrigatórios para `main`, squash merge
- **Formatação:** `.editorconfig` + `dotnet format --verify-no-changes` no CI
