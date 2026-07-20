using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using EyesGuard.Core.Abstractions;
using EyesGuard.Core.Models;

namespace EyesGuard.App.Services;

public sealed class UpdateService : IUpdateService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly string _downloadDirectory;

    public UpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
        _downloadDirectory = Path.Combine(Path.GetTempPath(), "EyesGuard", "updates");
    }

    public Version CurrentVersion => typeof(UpdateService).Assembly.GetName().Version ?? new Version(0, 1, 0);

    public async Task<UpdateCheckResult> CheckAsync(string? manifestUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(manifestUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            return new(UpdateCheckStatus.NotConfigured, CurrentVersion, null, "尚未配置更新地址");
        }

        try
        {
            var manifest = await _httpClient.GetFromJsonAsync<UpdateManifest>(uri, JsonOptions, cancellationToken);
            if (manifest is null || !Version.TryParse(manifest.Version, out var available))
            {
                return new(UpdateCheckStatus.InvalidManifest, CurrentVersion, null, "更新清单格式无效");
            }

            return available > CurrentVersion
                ? new(UpdateCheckStatus.Available, CurrentVersion, available, manifest.ReleaseNotes ?? "有新版本可用",
                    manifest.DownloadUrl, manifest.Sha256, manifest.ReleaseNotes)
                : new(UpdateCheckStatus.UpToDate, CurrentVersion, available, "当前已是最新版本");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(UpdateCheckStatus.Failed, CurrentVersion, null, $"检查更新失败：{exception.Message}");
        }
    }

    public async Task<UpdateCheckResult> DownloadAndLaunchAsync(UpdateCheckResult update, CancellationToken cancellationToken = default)
    {
        if (update.Status != UpdateCheckStatus.Available || !Uri.TryCreate(update.DownloadUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            return update with { Status = UpdateCheckStatus.InvalidManifest, Message = "更新下载地址无效" };
        }

        try
        {
            Directory.CreateDirectory(_downloadDirectory);
            var installerPath = Path.Combine(_downloadDirectory, $"EyesGuard-{update.AvailableVersion}.exe");
            await using (var source = await _httpClient.GetStreamAsync(uri, cancellationToken))
            await using (var target = File.Create(installerPath))
            {
                await source.CopyToAsync(target, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(update.Sha256))
            {
                await using var stream = File.OpenRead(installerPath);
                var actualHash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
                if (!string.Equals(actualHash, update.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(installerPath);
                    return update with { Status = UpdateCheckStatus.Failed, Message = "更新文件校验失败" };
                }
            }

            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
            return update with { Message = "更新程序已启动，请按安装向导完成升级" };
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            return update with { Status = UpdateCheckStatus.Failed, Message = $"下载更新失败：{exception.Message}" };
        }
    }

    private sealed record UpdateManifest(string Version, string DownloadUrl, string? Sha256, string? ReleaseNotes);
}
