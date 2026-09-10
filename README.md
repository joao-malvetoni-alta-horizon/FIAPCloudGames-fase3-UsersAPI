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
- AWS SNS (`AWSSDK.SimpleNotificationService`) para eventos de integração
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
  FCG.IntegrationTests # xUnit + Shouldly + Testcontainers (PostgreSQL + LocalStack/SNS/SQS)
```

## Variáveis de ambiente

| Variável | Descrição | Exemplo |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | String de conexão do PostgreSQL | `Host=db;Port=5432;Database=fcgdb;Username=fcg;Password=fcg123` |
| `JwtSettings__SecretKey` | Chave secreta para assinar os tokens JWT | *(string com ≥ 32 caracteres)* |
| `JwtSettings__ExpirationHours` | Validade do token, em horas | `4` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente (`Development` habilita o Swagger) | `Development` |
| `Sns__TopicArn` | ARN do tópico SNS onde o `UserRegisteredEvent` é publicado | `arn:aws:sns:us-east-1:450753703903:fcg-user-events` |
| `AWS_REGION` / `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` | Credenciais AWS para publicar no SNS (padrão do SDK; sem elas a publicação falha silenciosamente e o cadastro continua normal) | — |

## Executando localmente

Suba o PostgreSQL via Docker e rode a API localmente:

```bash
docker compose up -d          # PostgreSQL
dotnet run --project src/FCG.API
```

A publicação do `UserRegisteredEvent` (SNS) exige credenciais AWS reais no ambiente
para funcionar de ponta a ponta; sem elas, a chamada ao SNS falha e é apenas logada
como warning — o cadastro de usuário continua funcionando normalmente.

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

## Eventos de integração (SNS)

No cadastro de usuário a API publica o evento em um tópico SNS, usando
`AWSSDK.SimpleNotificationService` diretamente (`SnsIntegrationEventPublisher`). Os
contratos vêm do pacote `FiapCloudGames.Contracts`, compartilhado entre os
microsserviços. O consumidor é a função Lambda do repositório
`FIAPCloudGames-fase3-NotificationsAPI` (SNS → SQS → Lambda → DynamoDB).

| Gatilho | Evento | Tópico SNS |
|---|---|---|
| `POST /api/users/register` | `UserRegisteredEvent` | `fcg-user-events` |
| `POST /api/admin/users` | `UserRegisteredEvent` | `fcg-user-events` |

A publicação ocorre após o commit no banco, com o `traceparent` (W3C) injetado como
message attribute para permitir a correlação do trace no New Relic entre esta API e a
Lambda consumidora. Se o SNS estiver indisponível (ou sem credenciais), a falha é
logada sem quebrar a operação de negócio (entrega garantida exigiria o padrão Outbox).

Testado de ponta a ponta em `tests/FCG.IntegrationTests/UserEventsPublishingTests.cs`,
que sobe um LocalStack (SNS + SQS) via Testcontainers para validar a publicação real.

## Próximos passos

- Trocar a referência local de `FiapCloudGames.Contracts` por `PackageReference` (nuget.org);
- Padrão Outbox para entrega garantida dos eventos;
- Dockerfile de produção (multi-stage — já há um ponto de partida em `src/FCG.API/Dockerfile`);
- Manifestos Kubernetes em `/k8s` (Deployment, Service, ConfigMap, Secret).
