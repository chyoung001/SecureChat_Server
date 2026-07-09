using Microsoft.EntityFrameworkCore;
using SecureChat.Api.Hubs;
using SecureChat.Api.Services;
using SecureChat.Application.Abstractions;
using SecureChat.Infrastructure;
using SecureChat.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));

// ── Core Services ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "로그인 후 받은 accessToken을 입력하세요."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHttpContextAccessor();

// ── Infrastructure (EF, JWT, BCrypt, Repositories, Workers) ──────────────
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR 구현체는 IHubContext<ChatHub>가 필요해 Api 레이어에 등록
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

// ── CORS ──────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    var origins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// ── Auto Migration (컨테이너 최초 기동 시 DB 테이블 생성) ────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ── Middleware Pipeline ───────────────────────────────────────────────────
// Swagger는 프로덕션에서 API 표면을 공개하지 않도록 개발 환경에서만 노출한다.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
