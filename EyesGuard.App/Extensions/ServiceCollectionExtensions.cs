using Microsoft.Extensions.DependencyInjection;
using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Services;
using EyesGuard.Infrastructure.Storage;
using EyesGuard.Platform.Windows;
using EyesGuard.Application.Services;
using EyesGuard.App.ViewModels;
using EyesGuard.App.Services;
using System.Net.Http;

namespace EyesGuard.App.Extensions;

/// <summary>
/// 依赖注入扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册核心层服务
    /// </summary>
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        // 保留 IClock 用于向后兼容，但推荐使用 ISystemServices
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }

    /// <summary>
    /// 注册基础设施层服务
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<AppDataPaths>();
        services.AddSingleton<IActivityRepository, SqliteActivityRepository>();
        services.AddSingleton<ISettingsStore, SqliteSettingsStore>();
        services.AddSingleton<ISyncStateStore, JsonSyncStateStore>();
        services.AddSingleton<ISyncTransport, UnavailableSyncTransport>();
        return services;
    }

    /// <summary>
    /// 注册平台层服务
    /// </summary>
    public static IServiceCollection AddPlatform(this IServiceCollection services)
    {
        // 整合后的系统服务
        services.AddSingleton<ISystemServices, WindowsSystemServices>();

        // 保留旧接口用于向后兼容
        services.AddSingleton<IIdleTimeProvider, WindowsIdleTimeProvider>();
        services.AddSingleton<IProcessDetector, WindowsProcessDetector>();

        services.AddSingleton<IDisplayControlService, WindowsDisplayControlService>();
#pragma warning disable CA1416
        services.AddSingleton<IStartupService, WindowsStartupService>();
#pragma warning restore CA1416
        return services;
    }

    /// <summary>
    /// 注册应用层服务
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 休息提醒服务
        services.AddSingleton(provider =>
        {
            var settingsStore = provider.GetRequiredService<ISettingsStore>();
            var settings = settingsStore.LoadAsync().GetAwaiter().GetResult();
            return new BreakReminderService(
                settings,
                provider.GetRequiredService<IClock>(),
                provider.GetRequiredService<IIdleTimeProvider>(),
                provider.GetRequiredService<IActivityRepository>(),
                settingsStore);
        });

        // 应用服务
        services.AddSingleton<UsageReportingService>(provider =>
            new UsageReportingService(provider.GetRequiredService<BreakReminderService>()));
        services.AddSingleton<ScenarioAutomationService>();
        services.AddSingleton<BlueLightSchedulingService>();

        // 同步服务
        services.AddSingleton<SyncService>(provider => new SyncService(
            $"{Environment.MachineName}:{Environment.UserName}",
            provider.GetRequiredService<ISyncStateStore>(),
            provider.GetRequiredService<ISyncTransport>(),
            provider.GetRequiredService<IClock>()));

        // 更新服务
        services.AddSingleton<IUpdateService>(_ => new UpdateService(new HttpClient()));

        return services;
    }

    /// <summary>
    /// 注册表示层服务（ViewModels）
    /// </summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        // 所有 ViewModels 注册为 Singleton，因为 MainWindowViewModel 持有对它们的引用
        // 且它们在整个应用生命周期中应保持单例
        services.AddSingleton<BreakControlViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<BlueLightViewModel>();
        services.AddSingleton<SceneManagementViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<UpdateViewModel>();
        services.AddSingleton<SyncViewModel>();
        services.AddSingleton<BrightnessViewModel>();
        services.AddSingleton<StartupViewModel>();
        services.AddSingleton<WidgetViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        return services;
    }
}
