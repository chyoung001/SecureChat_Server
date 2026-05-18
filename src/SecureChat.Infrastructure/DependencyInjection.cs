using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SecureChat.Application.Abstractions;
using SecureChat.Application.Auth;
using SecureChat.Application.Common;
using SecureChat.Application.Contacts;
using SecureChat.Application.FriendRequests;
using SecureChat.Application.Messages;
using SecureChat.Application.Rooms;
using SecureChat.Application.Users;
using SecureChat.Infrastructure.BackgroundJobs;
using SecureChat.Infrastructure.Identity;
using SecureChat.Infrastructure.Persistence;

namespace SecureChat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core ───────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // ── Repositories & Unit of Work ───────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IContactRepository, ContactRepository>();
        services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Validators (FluentValidation) ─────────────────────────────────
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

        // ── Application Services ──────────────────────────────────────────
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IFriendRequestService, FriendRequestService>();

        // ── Background Jobs ───────────────────────────────────────────────
        services.AddHostedService<MessageExpirationWorker>();

        // ── Infrastructure Services ───────────────────────────────────────
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUser, CurrentUserService>();

        // ── JWT Bearer ────────────────────────────────────────────────────
        var jwtSection = configuration.GetSection("Jwt");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256], // alg=none 차단
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    // SignalR: 쿼리스트링 access_token 인식
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            context.Token = token;
                        return Task.CompletedTask;
                    },

                    // TokenVersion 검증: 로그아웃된 JWT 차단 (T-S-004)
                    OnTokenValidated = async context =>
                    {
                        var sub = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var tv  = context.Principal?.FindFirstValue("tv");

                        if (!Guid.TryParse(sub, out var userId) ||
                            !int.TryParse(tv, out var tokenVersion))
                        {
                            context.Fail("Invalid token claims");
                            return;
                        }

                        await using var scope =
                            context.HttpContext.RequestServices.CreateAsyncScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var user = await db.Users.FindAsync(userId);

                        if (user == null || user.TokenVersion != tokenVersion)
                            context.Fail("Token has been revoked");
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
