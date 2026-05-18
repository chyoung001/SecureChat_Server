using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecureChat.Common;
using SecureChat.Forms;
using SecureChat.Services;
using SecureChat.Storage;

namespace SecureChat;

internal static class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        SyncContextHelper.Initialize();

        var loginForm = Services.GetRequiredService<LoginForm>();
        Application.Run(loginForm);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var appSettings = new AppSettings();
        config.Bind(appSettings);

        // Storage
        services.AddSingleton(appSettings);
        services.AddSingleton<TokenStorage>();
        services.AddSingleton<LocalKeyStore>();
        services.AddSingleton<VerifiedKeyStore>();

        // HTTP (singleton, DPAPI-aware via TokenStorage)
        services.AddSingleton<ApiHttpClient>();

        // Real services
        services.AddSingleton<IAuthService, HttpAuthService>();
        services.AddSingleton<IChatService, SignalRChatService>();
        services.AddSingleton<IRoomService, HttpRoomService>();
        services.AddSingleton<IContactService, HttpContactService>();

        services.AddSingleton<IMessageStore, InMemoryMessageStore>();
        services.AddSingleton<IFriendRequestService, HttpFriendRequestService>();

        // Forms
        services.AddTransient<LoginForm>();
        services.AddTransient<MainForm>();
        services.AddTransient<SettingsForm>();
        services.AddTransient<CreateRoomDialog>();

        services.AddSingleton<Func<string, ChatForm>>(sp =>
            roomId => ActivatorUtilities.CreateInstance<ChatForm>(sp, roomId));
    }
}
