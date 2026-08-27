# FIAP Cloud Games — UsersAPI (Fase 2)

Microsserviço de **Usuários** da plataforma FIAP Cloud Games. Responsável por:

- **Cadastro** de usuários (nome, e-mail e senha forte);
- **Autenticação** via token JWT;
- **Autorização** com dois níveis de acesso (`Usuário` e `Administrador`);
- **Administração** de usuários (CRUD restrito a administradores).

Extraído do monólito da Fase 1, mantendo a arquitetura limpa em camadas
(Domain / Application / Infrastructure / API). O fluxo de biblioteca/compra de
jogos **não** pertence a este serviço — ele é responsabilidade do `CatalogAPI`.

## Stack

- .NET 10 — Minimal APIs
- Entity Framework Core + PostgreSQL (Npgsql) com migrations
- JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- BCrypt para hash de senha
- RabbitMQ (`RabbitMQ.Client`, sem MassTransit) para eventos de integração
- Contratos de eventos compartilhados via pacote NuGet `FiapCloudGames.Contracts`
- Serilog (logs estruturados) + Swagger
- Testes: xUnit + Shouldly + NSubstitute (unitários) e Testcontainers (integração)

## Estrutura

```
src/
  FCG.Domain          # Entidades, Value Objects, regras de negócio
  FCG.Application     # Casos de uso e DTOs
  FCG.Infrastructure  # EF Core, repositórios, segurança (JWT/BCrypt), migrations
  FCG.API             # Endpoints, middleware, composição
tests/
  FCG.UnitTests        # xUnit + Shouldly + NSubstitute
  FCG.IntegrationTests # xUnit + Shouldly + Testcontainers (PostgreSQL real)
```

## Variáveis de ambiente

| Variável | Descrição | Exemplo |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | String de conexão do PostgreSQL | `Host=db;Port=5432;Database=fcgdb;Username=fcg;Password=fcg123` |
| `JwtSettings__SecretKey` | Chave secreta para assinar os tokens JWT | *(string com ≥ 32 caracteres)* |
| `JwtSettings__ExpirationHours` | Validade do token, em horas | `4` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente (`Development` habilita o Swagger) | `Development` |

## Executando localmente

Suba a infraestrutura (PostgreSQL + RabbitMQ) via Docker e rode a API localmente:

```bash
docker compose up -d          # PostgreSQL + RabbitMQ (Management UI em http://localhost:15672 — fcg/fcg123)
dotnet run --project src/FCG.API
```

As migrations e o seed do administrador raiz são aplicados automaticamente na
inicialização. Ajuste as conexões em `src/FCG.API/appsettings.Development.json`
se necessário.

Swagger: `https://localhost:<porta>/swagger` (ambiente Development).

### Admin raiz (seed)

| E-mail | Senha |
|---|---|
| `admin@fcg.com` | `Admin@123` |

## Principais endpoints

| Método | Rota | Acesso |
|---|---|---|
| `POST` | `/api/users/register` | Público |
| `POST` | `/api/auth/login` | Público |
| `POST` | `/api/admin/users` | Administrador |
| `GET` | `/api/admin/users` | Administrador |
| `GET` | `/api/admin/users/{id}` | Administrador |
| `PUT` | `/api/admin/users/{id}` | Administrador |
| `DELETE` | `/api/admin/users/{id}` | Administrador |

## Testes

```bash
dotnet test tests/FCG.UnitTests          # unitários (rápidos, sem dependências)
dotnet test tests/FCG.IntegrationTests   # integração (requer Docker p/ Testcontainers)
```

## Eventos de integração (RabbitMQ)

No cadastro de usuário a API publica eventos em uma exchange `topic`, usando o
`RabbitMQ.Client` diretamente (sem MassTransit). Os contratos vêm do pacote
`FiapCloudGames.Contracts`, compartilhado entre os microsserviços.

| Gatilho | Evento | Exchange | Routing key |
|---|---|---|---|
| `POST /api/users/register` | `UserRegisteredEvent` | `users.exchange` | `user.registered` |
| `POST /api/admin/users` | `UserRegisteredEvent` | `users.exchange` | `user.registered` |

A publicação ocorre após o commit no banco. Se o broker estiver indisponível, a
falha é logada sem quebrar a operação de negócio (entrega garantida exigiria o
padrão Outbox).

## Próximos passos (Fase 2)

- Trocar a referência local de `FiapCloudGames.Contracts` por `PackageReference` (nuget.org);
- Padrão Outbox para entrega garantida dos eventos;
- Dockerfile de produção (multi-stage — já há um ponto de partida em `src/FCG.API/Dockerfile`);
- Manifestos Kubernetes em `/k8s` (Deployment, Service, ConfigMap, Secret).
