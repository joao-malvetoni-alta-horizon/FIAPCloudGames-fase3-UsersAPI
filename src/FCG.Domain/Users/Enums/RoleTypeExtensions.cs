namespace FCG.Domain.Users.Enums;

public static class RoleTypeExtensions
{
    extension(RoleType role)
    {
        public string DisplayName => role switch
        {
            RoleType.User => "Usuário",
            RoleType.Administrator => "Administrador",
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
    }
}