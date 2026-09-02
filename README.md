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
  FCG.IntegrationTests # xUnit + Shouldly + Testcontainers (PostgreSQL real)
```

## Variáveis de ambiente

| Variável | Descrição | Exemplo |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | String de conexão do PostgreSQL | `Host=db;Port=5432;Database=fcgdb;Username=fcg;Password=fcg123` |
| `JwtSettings__SecretKey` | Chave secreta para assinar os tokens JWT | *(string com ≥ 32 caracteres)* |
| `JwtSettings__ExpirationHours` | Validade do token, em horas | `4` |
| `ASPNETCORE_ENVIRONMENT` | Ambiente (`Development` habilita o Swagger) | `Development` |
| `NEW_RELIC_LICENSE_KEY` | License key da conta New Relic (segredo) | *(chave de 40 caracteres)* |

As demais variáveis do agente APM estão documentadas em [Observabilidade (New Relic)](#observabilidade-new-relic).

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
> as outras chaves (`RabbitMq__Password`, `JwtSettings__SecretKey`, `Users__ConnectionString`),
> repita-as no mesmo comando ou use `kubectl patch`.

### Rodando localmente com o agente

O `docker-compose.yml` sobe apenas a infraestrutura (PostgreSQL + RabbitMQ) — a API roda
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

### Limitação conhecida: trace sobre o RabbitMQ

O fluxo de **Compra de Jogo** não passa por este serviço — biblioteca e compra são
responsabilidade do `CatalogAPI`. Aqui o RabbitMQ é usado apenas para publicar
`UserRegisteredEvent` no cadastro. Ainda assim vale o registro:

a instrumentação de mensageria do agente 10.54 (`NewRelic.Providers.Wrapper.RabbitMq`)
casa com `RabbitMQ.Client` até `maxVersion="6.8.1"`, e este serviço resolve
**`RabbitMQ.Client` 7.2.1** (transitivo, via `FiapCloudGames.RabbitMq`). Ou seja: a
publicação na fila **não** é instrumentada automaticamente e o trace não se propaga
através do broker. Nenhum wrapper de mensageria foi reescrito para contornar isso — o
trace continua ponta-a-ponta em todo o trecho HTTP.

## Próximos passos (Fase 2)

- Trocar a referência local de `FiapCloudGames.Contracts` por `PackageReference` (nuget.org);
- Padrão Outbox para entrega garantida dos eventos;
- Dockerfile de produção (multi-stage — já há um ponto de partida em `src/FCG.API/Dockerfile`);
- Manifestos Kubernetes em `/k8s` (Deployment, Service, ConfigMap, Secret).
