# BudgetWise API — Roadmap de Implementações

## Legenda
- ✅ Concluído
- 🔲 Pendente

---

## 🔐 Autenticação
- ✅ `POST /auth/register` — Cadastro de usuário
- ✅ `POST /auth/login` — Login com lockout após 5 tentativas
- ✅ `POST /auth/refresh` — Rotação obrigatória de refresh token

---

## 🗂️ Categorias
- ✅ `GET /categories` — Listar (sistema + pessoais, paginado)
- ✅ `GET /categories/{id}` — Detalhe
- ✅ `POST /categories` — Criar categoria pessoal
- ✅ `PUT /categories/{id}` — Atualizar
- ✅ `DELETE /categories/{id}` — Desativar (soft delete)

---

## 💸 Transações
- ✅ `GET /transactions` — Listar com filtros (tipo, categoria, período, status) + paginação
- ✅ `GET /transactions/{id}` — Detalhe
- ✅ `POST /transactions` — Criar
- ✅ `PUT /transactions/{id}` — Atualizar
- ✅ `DELETE /transactions/{id}` — Excluir (soft delete)
- ✅ `PATCH /transactions/{id}/confirm` — Confirmar transação pendente

---

## 🏷️ Tags
- ✅ `GET /tags` — Listar tags do usuário
- ✅ `GET /tags/{id}` — Detalhe
- ✅ `POST /tags` — Criar tag
- ✅ `PUT /tags/{id}` — Renomear tag
- ✅ `DELETE /tags/{id}` — Excluir tag
- ✅ `POST /transactions/{id}/tags/{tagId}` — Vincular tag a transação
- ✅ `DELETE /transactions/{id}/tags/{tagId}` — Desvincular tag

---

## 👨‍👩‍👧 Grupos Familiares
- 🔲 `GET /family-groups` — Listar grupos do usuário
- 🔲 `GET /family-groups/{id}` — Detalhe + membros
- 🔲 `POST /family-groups` — Criar grupo
- 🔲 `PUT /family-groups/{id}` — Atualizar nome/descrição
- 🔲 `DELETE /family-groups/{id}` — Excluir grupo
- 🔲 `POST /family-groups/join` — Entrar via invite code
- 🔲 `DELETE /family-groups/{id}/members/{userId}` — Remover membro
- 🔲 `POST /family-groups/{id}/invite` — Regenerar invite code

---

## 💰 Despesas Compartilhadas
- 🔲 `GET /shared-expenses` — Listar despesas do grupo
- 🔲 `GET /shared-expenses/{id}` — Detalhe + participantes
- 🔲 `POST /shared-expenses` — Criar despesa compartilhada
- 🔲 `PUT /shared-expenses/{id}` — Atualizar
- 🔲 `DELETE /shared-expenses/{id}` — Excluir
- 🔲 `PATCH /shared-expenses/{id}/participants/{userId}/settle` — Marcar participante como quitado

---

## ⚡ Domain Events / Background Logic
- 🔲 `TransactionCreatedHandler` — Recalcular saldo, verificar limite de gastos
- 🔲 `TransactionConfirmedHandler` — Notificações, atualizar cache
- 🔲 `FamilyMemberJoinedHandler` — Notificar grupo
- 🔲 `FamilyMemberRemovedHandler` — Limpeza de dados
- 🔲 `SharedExpenseFullySettledHandler` — Notificar criador da despesa

---

## 🧪 Testes
- ✅ Unit tests — Domain (entities, value objects, events)
- ✅ Unit tests — Application (Auth use cases)
- ✅ Unit tests — Application (Transaction use cases)
- ✅ Unit tests — Infrastructure (seeder, domain event dispatcher)
- ✅ Unit tests — Architecture (dependency rules)
- ✅ Integration tests — Auth endpoints
- ✅ Integration tests — Transaction endpoints
- 🔲 Integration tests — Category endpoints
- ✅ Integration tests — Tag endpoints
- 🔲 Integration tests — Family Group endpoints
- 🔲 Integration tests — Shared Expense endpoints

---

## 🏗️ Infraestrutura / Qualidade
- ✅ Docker Compose (API + Postgres + Seq)
- ✅ Migrations automáticas na inicialização
- ✅ Seed de categorias do sistema
- ✅ Global error handler (ProblemDetails)
- ✅ Serilog + Seq
- ✅ Health check (Postgres)
- ✅ OpenAPI (Scalar)
- ✅ JWT com refresh token rotativo
- 🔲 Rate limiting nos endpoints de auth
- 🔲 Paginação por cursor (alternativa ao offset para grandes volumes)
- 🔲 Endpoint de sumário financeiro (`GET /summary?month=2026-04`)
