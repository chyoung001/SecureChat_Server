# ── Stage 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 복원은 csproj만 먼저 (레이어 캐시 활용)
COPY SecureChat.sln .
COPY src/SecureChat.Domain/SecureChat.Domain.csproj             src/SecureChat.Domain/
COPY src/SecureChat.Application/SecureChat.Application.csproj   src/SecureChat.Application/
COPY src/SecureChat.Infrastructure/SecureChat.Infrastructure.csproj src/SecureChat.Infrastructure/
COPY src/SecureChat.Api/SecureChat.Api.csproj                   src/SecureChat.Api/

RUN dotnet restore src/SecureChat.Api/SecureChat.Api.csproj

# 전체 소스 복사 후 빌드
COPY . .
RUN dotnet publish src/SecureChat.Api/SecureChat.Api.csproj \
    -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# SQLite 데이터 디렉터리 (Railway Volume 마운트 경로)
RUN mkdir -p /data

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Railway가 $PORT를 주입 → ASPNETCORE_URLS에 반영. 기본값 8080
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet SecureChat.Api.dll"]
