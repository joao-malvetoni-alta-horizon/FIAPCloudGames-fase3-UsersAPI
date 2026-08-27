using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace FCG.IntegrationTests;

/// <summary>
/// Sobe a UsersAPI completa contra um PostgreSQL e um RabbitMQ reais (Testcontainers).
/// O pipeline de inicialização da API aplica as migrations e o seed do admin, e a config
/// de RabbitMq é apontada para o broker do container.
/// </summary>
public class UsersApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string RabbitUser = "fcg";
    private const string RabbitPassword = "fcg123";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("fcgdb")
        .WithUsername("fcg")
        .WithPassword("fcg123")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine")
        .WithUsername(RabbitUser)
        .WithPassword(RabbitPassword)
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _db.GetConnectionString(),
                ["RabbitMq:Host"] = _rabbitMq.Hostname,
                ["RabbitMq:Port"] = _rabbitMq.GetMappedPublicPort(5672).ToString(),
                ["RabbitMq:Username"] = RabbitUser,
                ["RabbitMq:Password"] = RabbitPassword,
                ["RabbitMq:VirtualHost"] = "/"
            });
        });
    }

    /// <summary>Abre uma conexão com o RabbitMQ do container (para consumir e validar eventos nos testes).</summary>
    public async Task<IConnection> CreateRabbitMqConnectionAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMq.Hostname,
            Port = _rabbitMq.GetMappedPublicPort(5672),
            UserName = RabbitUser,
            Password = RabbitPassword
        };
        return await factory.CreateConnectionAsync();
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_db.StartAsync(), _rabbitMq.StartAsync());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }
}