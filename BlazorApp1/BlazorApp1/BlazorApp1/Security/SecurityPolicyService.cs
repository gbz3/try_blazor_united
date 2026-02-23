using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlazorApp1.Security;

/// <summary>
/// セキュリティポリシー設定を動的に管理するサービス
/// appsettings.json の変更を監視して、メモリ内の設定を更新
/// </summary>
public interface ISecurityPolicyService
{
    string GetCurrentCspHeader();
    SecurityPolicyOptions GetCurrentOptions();
}

public class SecurityPolicyService : ISecurityPolicyService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecurityPolicyService> _logger;
    private SecurityPolicyOptions _currentOptions;
    private IDisposable? _changeToken;

    public SecurityPolicyService(
        IConfiguration configuration,
        ILogger<SecurityPolicyService> logger,
        IOptions<SecurityPolicyOptions> options)
    {
        _configuration = configuration;
        _logger = logger;
        _currentOptions = options.Value;

        // appsettings.json の変更を監視
        WatchConfigurationChanges();
    }

    /// <summary>
    /// 現在の CSP ヘッダー値を取得
    /// </summary>
    public string GetCurrentCspHeader()
    {
        return _currentOptions.BuildCspHeader();
    }

    /// <summary>
    /// 現在のセキュリティポリシーオプションを取得
    /// </summary>
    public SecurityPolicyOptions GetCurrentOptions()
    {
        return _currentOptions;
    }

    /// <summary>
    /// appsettings.json の変更を監視して設定を更新
    /// </summary>
    private void WatchConfigurationChanges()
    {
        _changeToken = _configuration.GetReloadToken().RegisterChangeCallback(_ =>
        {
            try
            {
                var newOptions = new SecurityPolicyOptions();
                _configuration.GetSection("SecurityPolicy").Bind(newOptions);

                // 設定が変更された場合、ログに記録して更新
                if (HasConfigurationChanged(_currentOptions, newOptions))
                {
                    _logger.LogWarning(
                        "セキュリティポリシーが更新されました。新しい CSP: {Csp}",
                        newOptions.BuildCspHeader());
                    _currentOptions = newOptions;
                }

                // 再度変更を監視
                WatchConfigurationChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "セキュリティポリシーの更新中にエラーが発生しました");
            }
        }, null);
    }

    /// <summary>
    /// セキュリティポリシーが実際に変更されたかを確認
    /// </summary>
    private static bool HasConfigurationChanged(
        SecurityPolicyOptions oldOptions,
        SecurityPolicyOptions newOptions)
    {
        return oldOptions.ScriptSrc != newOptions.ScriptSrc ||
               oldOptions.ConnectSrc != newOptions.ConnectSrc ||
               oldOptions.StyleSrc != newOptions.StyleSrc ||
               oldOptions.ImgSrc != newOptions.ImgSrc ||
               oldOptions.ObjectSrc != newOptions.ObjectSrc ||
               oldOptions.DefaultSrc != newOptions.DefaultSrc ||
               oldOptions.ReportUri != newOptions.ReportUri ||
               oldOptions.Enabled != newOptions.Enabled;
    }

    public void Dispose()
    {
        _changeToken?.Dispose();
    }
}


