using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SecureChat.Infrastructure.Persistence;

// dotnet ef migrations 실행 시 Program.cs 없이 AppDbContext를 인스턴스화하기 위한 팩토리
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=securechat.db")
            .Options;

        return new AppDbContext(options);
    }
}
