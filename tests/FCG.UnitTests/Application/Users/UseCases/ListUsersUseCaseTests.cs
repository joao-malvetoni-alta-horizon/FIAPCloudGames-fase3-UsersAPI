using FCG.Application.Users.UseCases;
using FCG.Domain.Users.Entities;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Interfaces;
using NSubstitute;
using Shouldly;

namespace FCG.UnitTests.Application.Users.UseCases;

public class ListUsersUseCaseTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();

    private static User MakeUserWithRole(string name, string email)
    {
        return User.Create(name, email, "hash", RoleType.User);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPageIsValid_ShouldReturnPagedResponse()
    {
        var users = new List<User>
        {
            MakeUserWithRole("Alice", "alice@example.com"),
            MakeUserWithRole("Bob", "bob@example.com")
        };
        _userRepo
            .ListAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)users, 2));

        var useCase = new ListUsersUseCase(_userRepo);
        var response = await useCase.ExecuteAsync(1, 10);

        response.TotalCount.ShouldBe(2);
        response.Page.ShouldBe(1);
        response.PageSize.ShouldBe(10);
        response.Items.Count.ShouldBe(2);
        response.Items.Select(i => i.Email).ShouldContain("alice@example.com");
    }

    [Theory]
    [InlineData(0, 10, 1, 10)]
    [InlineData(-5, 10, 1, 10)]
    [InlineData(1, 0, 1, 10)]
    [InlineData(1, -3, 1, 10)]
    [InlineData(1, 50, 1, 10)]
    public async Task ExecuteAsync_WhenPageOrSizeIsOutOfRange_ShouldUseDefaults(
        int inputPage, int inputSize, int expectedPage, int expectedSize)
    {
        _userRepo
            .ListAsync(expectedPage, expectedSize, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)[], 0));

        var useCase = new ListUsersUseCase(_userRepo);
        var response = await useCase.ExecuteAsync(inputPage, inputSize);

        response.Page.ShouldBe(expectedPage);
        response.PageSize.ShouldBe(expectedSize);
        await _userRepo.Received(1).ListAsync(expectedPage, expectedSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMapRoleDisplayName()
    {
        var user = User.Create("Some User", "some@example.com", "hash", RoleType.User);
        _userRepo
            .ListAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)[user], 1));

        var useCase = new ListUsersUseCase(_userRepo);
        var response = await useCase.ExecuteAsync(1, 10);

        response.Items.Single().Role.ShouldBe(RoleType.User.DisplayName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoUsers_ShouldReturnEmptyList()
    {
        _userRepo
            .ListAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<User>)[], 0));

        var useCase = new ListUsersUseCase(_userRepo);
        var response = await useCase.ExecuteAsync(1, 10);

        response.Items.ShouldBeEmpty();
        response.TotalCount.ShouldBe(0);
    }
}