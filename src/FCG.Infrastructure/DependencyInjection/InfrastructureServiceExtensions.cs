using System.Security.Claims;
using System.Text;
using Amazon.SimpleNotificationService;
using FCG.Application.Auth.Interfaces;
using FCG.Domain.Shared;
using FCG.Domain.Users.Enums;
using FCG.Domain.Users.Interfaces;
using FCG.Domain.Users.Services;
using FCG.Infrastructure.Persistence;
using FCG.Infrastructure.Persistence.Context;
using FCG.Infrastructure.Persistence.Repositories;
using FCG.Application.Messaging;
using FCG.Infrastructure.Messaging;
using FCG.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FCG.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            return services
                .ConfigureDb(configuration)
                .AddRepositories()
                .AddUnitOfWork()
                .AddDomainServices()
                .AddMessaging(configuration)
                .AddJwtAuthentication(configuration)
                .AddAuthorizationPolicies();
        }

        private IServiceCollection ConfigureDb(IConfiguration configuration)
        {
            return services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }

        private IServiceCollection AddRepositories()
        {
            return services.AddScoped<IUserRepository, UserRepository>();
        }

        private IServiceCollection AddUnitOfWork()
        {
            return services.AddScoped<UnitOfWork>()
                .AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<UnitOfWork>())
                .AddScoped<IUserUnitOfWork>(provider => provider.GetRequiredService<UnitOfWork>());
        }

        private IServiceCollection AddDomainServices()
        {
            return services
                .AddScoped<IPasswordHasher, BcryptPasswordHasher>()
                .AddScoped<IUserService, UserService>()
                .AddScoped<IJwtTokenService, JwtTokenService>()
                .AddScoped<DatabaseSeeder>();
        }

        private IServiceCollection AddMessaging(IConfiguration configuration)
        {
            services.Configure<SnsOptions>(configuration.GetSection(SnsOptions.SectionName));
            services.AddSingleton<IAmazonSimpleNotificationService>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<SnsOptions>>().Value;

                // ServiceUrl só vem preenchido em teste (LocalStack). Em produção o SDK
                // resolve o endpoint real da AWS a partir da região (AWS_REGION).
                if (string.IsNullOrWhiteSpace(options.ServiceUrl))
                    return new AmazonSimpleNotificationServiceClient();

                return new AmazonSimpleNotificationServiceClient(
                    new AmazonSimpleNotificationServiceConfig { ServiceURL = options.ServiceUrl });
            });
            return services.AddSingleton<IIntegrationEventPublisher, SnsIntegrationEventPublisher>();
        }

        private IServiceCollection AddJwtAuthentication(IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
            services.AddSingleton(jwtSettings);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                        RoleClaimType = ClaimTypes.Role
                    };
                });

            return services;
        }

        private IServiceCollection AddAuthorizationPolicies()
        {
            return services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleType.Administrator.DisplayName));
            });
        }
    }
}