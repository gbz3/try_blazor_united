namespace BlazorApp1.Security;

/// <summary>
/// セキュリティポリシーの設定オプション
/// appsettings.json の SecurityPolicy セクションにバインドされます
/// </summary>
public class SecurityPolicyOptions
{
    public bool Enabled { get; set; } = true;
    public string ScriptSrc { get; set; } = "'self'";
    public string ConnectSrc { get; set; } = "'self'";
    public string StyleSrc { get; set; } = "'self' 'unsafe-inline'";
    public string ImgSrc { get; set; } = "'self' data: https:";
    public string ObjectSrc { get; set; } = "'none'";
    public string DefaultSrc { get; set; } = "'self'";
    public string? ReportUri { get; set; }

    /// <summary>
    /// 現在の設定から Content-Security-Policy ヘッダーの値を生成
    /// </summary>
    public string BuildCspHeader()
    {
        var directives = new List<string>
        {
            $"script-src {ScriptSrc}",
            $"connect-src {ConnectSrc}",
            $"style-src {StyleSrc}",
            $"img-src {ImgSrc}",
            $"object-src {ObjectSrc}",
            $"default-src {DefaultSrc}"
        };

        if (!string.IsNullOrWhiteSpace(ReportUri))
        {
            directives.Add($"report-uri {ReportUri}");
        }

        return string.Join("; ", directives);
    }
}

