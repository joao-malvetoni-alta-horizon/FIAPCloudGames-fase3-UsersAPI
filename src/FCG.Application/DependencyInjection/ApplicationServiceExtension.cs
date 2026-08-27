using FCG.Application.Auth.Interfaces;
using FCG.Application.Auth.UseCases;
using FCG.Application.Users.Interfaces;
using FCG.Application.Users.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Application.DependencyInjection;

public static class ApplicationServiceExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()

        {
            return services.AddUseCases();
        }

        private IServiceCollection AddUseCases()
        {
            return services
                .AddScoped<IRegisterUserUseCase, RegisterUserUseCase>()
                .AddScoped<IAdminCreateUserUseCase, AdminCreateUserUseCase>()
                .AddScoped<IListUsersUseCase, ListUsersUseCase>()
                .AddScoped<IGetUserDetailUseCase, GetUserDetailUseCase>()
                .AddScoped<IAdminUpdateUserUseCase, AdminUpdateUserUseCase>()
                .AddScoped<IAdminDeleteUserUseCase, AdminDeleteUserUseCase>()
                .AddScoped<ILoginUseCase, LoginUseCase>();
        }
    }
}