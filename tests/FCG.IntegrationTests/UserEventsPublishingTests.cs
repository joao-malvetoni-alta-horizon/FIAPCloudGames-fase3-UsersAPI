using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FCG.Application.Auth.DTOs;
using FCG.Domain.Users.Enums;
using FiapCloudGames.Contracts.Users;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;

namespace FCG.IntegrationTests;

/// <summary>
/// Verifica, ponta a ponta, que os eventos de integração são realmente publicados no
/// RabbitMQ quando os endpoints de usuário são chamados. Um consumidor de teste declara
/// uma fila ligada à exchange e às routing keys dos eventos e valida a mensagem recebida.
/// </summary>
public class UserEventsPublishingTests(UsersApiFactory factory) : IClassFixture<UsersApiFactory>
{
    private const string SeededAdminEmail = "admin@fcg.com";
    private const string SeededAdminPassword = "Admin@123";

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task PostRegister_WhenRequestIsValid_ShouldPublishUserRegisteredEvent()
    {
        await using var connection = await factory.CreateRabbitMqConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var queue = await BindQueueAsync(channel, UserMessaging.RoutingKeys.Registered);

        var client = factory.CreateClient();
        var email = UniqueEmail();
        var response = await client.PostAsJsonAsync("/api/users/register",
            new { name = "Evt Register", email, password = "Strong@123" });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var json = await WaitForMessageAsync(channel, queue);
        var @event = JsonSerializer.Deserialize<UserRegisteredEvent>(json);

        @event.ShouldNotBeNull();
        @event!.Email.ShouldBe(email);
        @event.Name.ShouldBe("Evt Register");
        @event.UserId.ShouldNotBe(Guid.Empty);
        @event.EventId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task PostAdminUsers_WhenRequestIsValid_ShouldPublishUserRegisteredEvent()
    {
        await using var connection = await factory.CreateRabbitMqConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var queue = await BindQueueAsync(channel, UserMessaging.RoutingKeys.Registered);

        var client = factory.CreateClient();
        var token = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var email = UniqueEmail();
        var response = await client.PostAsJsonAsync("/api/admin/users",
            new { name = "Evt Created", email, password = "Strong@123", role = RoleType.User.ToString() });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var json = await WaitForMessageAsync(channel, queue);
        var @event = JsonSerializer.Deserialize<UserRegisteredEvent>(json);

        @event.ShouldNotBeNull();
        @event!.Email.ShouldBe(email);
        @event.Name.ShouldBe("Evt Created");
        @event.UserId.ShouldNotBe(Guid.Empty);
    }

    /// <summary>Declara a exchange (igual ao produtor) e uma fila temporária ligada à routing key.</summary>
    private static async Task<string> BindQueueAsync(IChannel channel, string routingKey)
    {
        await channel.ExchangeDeclareAsync(
            UserMessaging.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        var queue = await channel.QueueDeclareAsync();
        await channel.QueueBindAsync(queue.QueueName, UserMessaging.Exchange, routingKey);
        return queue.QueueName;
    }

    private static async Task<string> WaitForMessageAsync(IChannel channel, string queue)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) =>
        {
            tcs.TrySetResult(Encoding.UTF8.GetString(ea.Body.ToArray()));
            return Task.CompletedTask;
        };
        await channel.BasicConsumeAsync(queue, autoAck: true, consumer);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var _ = cts.Token.Register(() => tcs.TrySetException(
            new TimeoutException("Nenhum evento recebido do RabbitMQ dentro do tempo limite.")));
        return await tcs.Task;
    }

    private static async Task<string> LoginAsAdminAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = SeededAdminEmail, password = SeededAdminPassword });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload!.AccessToken;
    }
}
