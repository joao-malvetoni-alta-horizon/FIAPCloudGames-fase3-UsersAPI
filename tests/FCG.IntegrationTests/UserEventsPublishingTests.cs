using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FCG.Application.Auth.DTOs;
using FCG.Domain.Users.Enums;
using FiapCloudGames.Contracts.Users;
using Shouldly;

namespace FCG.IntegrationTests;

/// <summary>
/// Verifica, ponta a ponta, que os eventos de integração são realmente publicados no
/// SNS quando os endpoints de usuário são chamados. Uma fila SQS já inscrita no tópico
/// (ver UsersApiFactory) recebe a mensagem e o teste valida o conteúdo.
/// </summary>
public class UserEventsPublishingTests(UsersApiFactory factory) : IClassFixture<UsersApiFactory>
{
    private const string SeededAdminEmail = "admin@fcg.com";
    private const string SeededAdminPassword = "Admin@123";

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task PostRegister_WhenRequestIsValid_ShouldPublishUserRegisteredEvent()
    {
        using var sqs = factory.CreateSqsClient();

        var client = factory.CreateClient();
        var email = UniqueEmail();
        var response = await client.PostAsJsonAsync("/api/users/register",
            new { name = "Evt Register", email, password = "Strong@123" });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var json = await WaitForMessageAsync(sqs, factory.QueueUrl);
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
        using var sqs = factory.CreateSqsClient();

        var client = factory.CreateClient();
        var token = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var email = UniqueEmail();
        var response = await client.PostAsJsonAsync("/api/admin/users",
            new { name = "Evt Created", email, password = "Strong@123", role = RoleType.User.ToString() });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var json = await WaitForMessageAsync(sqs, factory.QueueUrl);
        var @event = JsonSerializer.Deserialize<UserRegisteredEvent>(json);

        @event.ShouldNotBeNull();
        @event!.Email.ShouldBe(email);
        @event.Name.ShouldBe("Evt Created");
        @event.UserId.ShouldNotBe(Guid.Empty);
    }

    /// <summary>Faz polling na fila SQS até receber uma mensagem ou expirar o tempo limite.</summary>
    private static async Task<string> WaitForMessageAsync(IAmazonSQS sqs, string queueUrl)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        while (!cts.IsCancellationRequested)
        {
            var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 2
            }, cts.Token);

            var message = response.Messages.FirstOrDefault();
            if (message is not null)
            {
                await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cts.Token);
                return message.Body;
            }
        }

        throw new TimeoutException("Nenhum evento recebido do SNS/SQS dentro do tempo limite.");
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
