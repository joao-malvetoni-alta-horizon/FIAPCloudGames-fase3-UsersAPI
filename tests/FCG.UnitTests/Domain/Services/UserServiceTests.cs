using FCG.Domain.Users.Exceptions;
using FCG.Domain.Users.Interfaces;
using FCG.Domain.Users.Services;
using NSubstitute;
using Shouldly;

namespace FCG.UnitTests.Domain.Services;

public class UserServiceTests
{
    [Fact]
    public async Task CheckEmailUniquenessAsync_WhenEmailAlreadyExists_ShouldThrowUserAlreadyExistsException()
    {
        // Arrange
        const string email = "existing.user@example.com";
        var userRepository = Substitute.For<IUserRepository>();
        userRepository
            .ExistsByEmailAsync(email, Arg.Any<CancellationToken>())
            .Returns(true);

        var service = new UserService(userRepository);

        // Act
        var act = async () => await service.CheckEmailUniquenessAsync(email, CancellationToken.None);

        // Assert
        var ex = await Should.ThrowAsync<UserAlreadyExistsException>(act);
        ex.Message.ShouldContain(email);
    }
}