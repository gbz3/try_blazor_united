namespace BlazorApp1.Security;

/// <summary>
/// Content-Security-Policy ヘッダーを動的に設定するミドルウェア
/// SecurityPolicyService から現在の設定を取得して適用
/// </summary>
public class ContentSecurityPolicyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISecurityPolicyService _securityPolicyService;

    public ContentSecurityPolicyMiddleware(
        RequestDelegate next,
        ISecurityPolicyService securityPolicyService)
    {
        _next = next;
        _securityPolicyService = securityPolicyService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var options = _securityPolicyService.GetCurrentOptions();

        // セキュリティポリシーが有効な場合、CSP ヘッダーを追加
        if (options.Enabled)
        {
            var cspHeader = _securityPolicyService.GetCurrentCspHeader();
            context.Response.Headers["Content-Security-Policy"] = cspHeader;
        }

        await _next(context);
    }
}

/// <summary>
/// ContentSecurityPolicyMiddleware をアプリケーションに登録するための拡張メソッド
/// </summary>
public static class ContentSecurityPolicyMiddlewareExtensions
{
    public static IApplicationBuilder UseContentSecurityPolicy(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<ContentSecurityPolicyMiddleware>();
    }
}

