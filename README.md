# FIAP Cloud Games — UsersAPI (Fase 3)

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
- Observabilidade: agente APM do New Relic (`NewRelic.Agent`), sem código de instrumentação
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
| `NEW_RELIC_LICENSE_KEY` | License key da conta New Relic (segredo) | *(chave de 40 caracteres)* |

As demais variáveis do agente APM estão documentadas em [Observabilidade (New Relic)](#observabilidade-new-relic).

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

## Observabilidade (New Relic)

A stack de observabilidade escolhida para a Fase 3 é a **opção B — plataforma de APM
gerenciada**, com o **New Relic**. A instrumentação é feita pelo *agente APM*, não por
código: o pacote NuGet `NewRelic.Agent` (referenciado apenas em `FCG.API`) copia o agente
para a pasta `newrelic/` do output de publish, e o profiler do CoreCLR o carrega em runtime
quando as variáveis `CORECLR_*` estão presentes — elas já vêm definidas no
`src/FCG.API/Dockerfile`.

Nenhuma configuração de log foi reescrita: o agente instrumenta o Serilog e o
`Microsoft.Extensions.Logging` automaticamente.

### Variáveis de ambiente do agente

| Variável | Descrição | Valor |
|---|---|---|
| `NEW_RELIC_LICENSE_KEY` | License key da conta New Relic. **Segredo** — vem do Secret `fcg-secrets` | *(não versionada)* |
| `CORECLR_ENABLE_PROFILING` | Liga o profiler do CoreCLR | `1` |
| `CORECLR_PROFILER` | GUID do profiler do New Relic (fixo, case-sensitive) | `{36032161-FFC0-4B61-B559-F6C5D41BAE5A}` |
| `CORECLR_NEWRELIC_HOME` | Diretório do agente | `/app/newrelic` |
| `CORECLR_PROFILER_PATH` | Biblioteca nativa do profiler (linux-x64) | `/app/newrelic/libNewRelicProfiler.so` |
| `NEW_RELIC_APP_NAME` | Nome da aplicação no New Relic | `FCG-UsersAPI` |
| `NEW_RELIC_DISTRIBUTED_TRACING_ENABLED` | Trace distribuído (propagação W3C sobre HTTP) | `true` |
| `NEW_RELIC_APPLICATION_LOGGING_ENABLED` | Coleta de logs da aplicação | `true` |
| `NEW_RELIC_APPLICATION_LOGGING_FORWARDING_ENABLED` | Encaminha os logs para o New Relic | `true` |
| `NEW_RELIC_APPLICATION_LOGGING_LOCAL_DECORATING_ENABLED` | Decora os logs locais com os ids de trace/span | `true` |

Todas exceto a license key já estão no `Dockerfile`; só a chave é injetada em runtime.

### Kubernetes — a chave via Secret

O requisito da Fase 3 é que as chaves de API sejam gerenciadas via **Kubernetes Secrets**.
O `k8s/deployment.yaml` lê `NEW_RELIC_LICENSE_KEY` do Secret `fcg-secrets`, na chave
`NewRelic__LicenseKey` (mesmo estilo das demais chaves do Secret):

```bash
kubectl -n fcg create secret generic fcg-secrets \
  --from-literal=NewRelic__LicenseKey=<sua-license-key> \
  --dry-run=client -o yaml | kubectl apply -f -
```

> O comando acima recria o Secret apenas com essa chave. Se `fcg-secrets` já existir com
> as outras chaves (`JwtSettings__SecretKey`, `Users__ConnectionString`,
> `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`),
> repita-as no mesmo comando ou use `kubectl patch`.

### Rodando localmente com o agente

O `docker-compose.yml` sobe apenas a infraestrutura (PostgreSQL) — a API roda
com `dotnet run`, então as variáveis do `Dockerfile` não valem aqui. Exporte-as no shell
apontando para o output do build (o agente precisa existir em disco, então compile antes):

```bash
dotnet build src/FCG.API                      # gera bin/Debug/net10.0/newrelic

export NEW_RELIC_LICENSE_KEY=<sua-license-key>
export CORECLR_ENABLE_PROFILING=1
export CORECLR_PROFILER='{36032161-FFC0-4B61-B559-F6C5D41BAE5A}'
export CORECLR_NEWRELIC_HOME="$PWD/src/FCG.API/bin/Debug/net10.0/newrelic"
export CORECLR_PROFILER_PATH="$CORECLR_NEWRELIC_HOME/libNewRelicProfiler.so"
export NEW_RELIC_APP_NAME=FCG-UsersAPI
export NEW_RELIC_DISTRIBUTED_TRACING_ENABLED=true
export NEW_RELIC_APPLICATION_LOGGING_ENABLED=true
export NEW_RELIC_APPLICATION_LOGGING_FORWARDING_ENABLED=true

dotnet run --project src/FCG.API
```

Sem `NEW_RELIC_LICENSE_KEY` o agente carrega mas não conecta — a aplicação sobe normalmente.
Nunca comite a license key: ela é um segredo, como as demais chaves do `fcg-secrets`.

### Os três pilares

| Pilar | Como é atendido |
|---|---|
| **Métricas** | Automáticas do agente APM (latência, throughput e taxa de erro por endpoint). O dashboard é montado na UI do New Relic — fora deste repositório. |
| **Logs** | `NEW_RELIC_APPLICATION_LOGGING_*` liga o encaminhamento e a decoração automáticos dos logs do Serilog, com correlação por `trace.id`/`span.id`. Nenhum sink HTTP foi adicionado. |
| **Traces** | Trace distribuído habilitado. Sobre HTTP a propagação (W3C Trace Context) é automática: a UsersAPI aparece no mapa de serviços e nos traces que a atravessam. |

### Limitação conhecida: trace através da mensageria

O fluxo de **Compra de Jogo** não passa por este serviço — biblioteca e compra são
responsabilidade do `CatalogAPI`. Aqui a mensageria é usada apenas para publicar
`UserRegisteredEvent` no cadastro, via SNS (ver
[Eventos de integração](#eventos-de-integração-sns)). Ainda assim vale o registro:

SNS e SQS não propagam o contexto de trace sozinhos. O `SnsIntegrationEventPublisher`
injeta o `traceparent` (W3C) como message attribute do `PublishRequest`, mas a ponta a
ponta só se fecha se o consumidor ler esse atributo e linkar o span ao trace de origem —
o que é responsabilidade da Lambda do `FIAPCloudGames-fase3-NotificationsAPI`, fora deste
repositório. No trecho HTTP a propagação é automática e o trace é contínuo.

## Próximos passos

- Trocar a referência local de `FiapCloudGames.Contracts` por `PackageReference` (nuget.org);
- Padrão Outbox para entrega garantida dos eventos.
