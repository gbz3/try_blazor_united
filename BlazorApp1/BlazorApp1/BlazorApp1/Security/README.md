# 動的セキュリティポリシー（CSP）実装ガイド

## 概要

このドキュメントは、アプリケーションの再起動なしに **Content-Security-Policy（CSP）** をリアルタイムで変更できる機能について説明しています。

セキュリティ侵害が発生した場合、迅速に対応する必要があります。このシステムでは、`appsettings.json` ファイルを編集するだけで、設定がアプリケーション再起動なしに即座に反映されます。

## アーキテクチャ

### コンポーネント

| ファイル | 説明 |
|---------|------|
| `SecurityPolicyOptions.cs` | CSP 設定をバインドするための POCO クラス |
| `SecurityPolicyService.cs` | 設定を管理し、`appsettings.json` の変更を監視するサービス |
| `ContentSecurityPolicyMiddleware.cs` | リクエスト毎に CSP ヘッダーを動的に設定するミドルウェア |
| `appsettings.json` | セキュリティポリシーの設定ファイル |
| `Program.cs` | サービスとミドルウェアの登録 |

### フロー

```
HTTP リクエスト到着
    ↓
ContentSecurityPolicyMiddleware
    ↓
SecurityPolicyService から現在の CSP を取得
    ↓
Response ヘッダーに追加
    ↓
クライアントに送信
```

## 設定方法

### 1. appsettings.json での CSP 設定

```json
{
  "SecurityPolicy": {
    "Enabled": true,
    "ScriptSrc": "'self'",
    "ConnectSrc": "'self' https://api.trusted.com",
    "StyleSrc": "'self' 'unsafe-inline'",
    "ImgSrc": "'self' data: https:",
    "ObjectSrc": "'none'",
    "DefaultSrc": "'self'",
    "ReportUri": "/api/csp-report"
  }
}
```

### 2. 各ディレクティブの説明

| ディレクティブ | 説明 | 例 |
|---------------|------|-----|
| `Enabled` | セキュリティポリシーを有効にするか | `true`/`false` |
| `ScriptSrc` | JavaScript の実行元を制限 | `'self'`, `'self' https://cdn.example.com` |
| `ConnectSrc` | fetch、XHR、WebSocket の接続先を制限 | `'self'`, `'self' https://api.example.com` |
| `StyleSrc` | CSS の読み込み元を制限 | `'self' 'unsafe-inline'` |
| `ImgSrc` | 画像の読み込み元を制限 | `'self' data: https:` |
| `ObjectSrc` | Flash などを制限 | `'none'` |
| `DefaultSrc` | デフォルトポリシー | `'self'` |
| `ReportUri` | CSP 違反をレポートするエンドポイント | `/api/csp-report` |

## 使用シナリオ

### シナリオ 1: 通常の本番環境設定

```json
{
  "SecurityPolicy": {
    "Enabled": true,
    "ScriptSrc": "'self'",
    "ConnectSrc": "'self' https://api.example.com",
    "StyleSrc": "'self' 'unsafe-inline'",
    "ImgSrc": "'self' data: https:",
    "ObjectSrc": "'none'",
    "DefaultSrc": "'self'"
  }
}
```

### シナリオ 2: セキュリティ侵害時（緊急対応）

**脆弱性が判明した外部 API への接続を即座にブロック：**

```json
{
  "SecurityPolicy": {
    "Enabled": true,
    "ScriptSrc": "'self'",
    "ConnectSrc": "'self'",
    "StyleSrc": "'self' 'unsafe-inline'",
    "ImgSrc": "'self' data: https:",
    "ObjectSrc": "'none'",
    "DefaultSrc": "'self'",
    "ReportUri": "/api/csp-report"
  }
}
```

**変更内容：**
- `ConnectSrc` から `https://vulnerable-api.com` を削除
- すべての外部 API 呼び出しをブロック
- CSP 違反レポートを有効化して、何がブロックされているかを監視

### シナリオ 3: セキュリティポリシーを一時的に無効化

```json
{
  "SecurityPolicy": {
    "Enabled": false
  }
}
```

**用途：**
- トラブルシューティング時
- CSP により正常な機能が阻害されている場合（開発環境）

## リアルタイム反映の仕組み

### 1. ファイル監視

`SecurityPolicyService` は ASP.NET Core の `IConfiguration.GetReloadToken()` を使用して、`appsettings.json` の変更を監視します。

```csharp
_changeToken = _configuration.GetReloadToken().RegisterChangeCallback(_ => {
    // ファイルが変更されたら自動的にこのコールバックが実行
});
```

### 2. 設定の自動更新

ファイルが変更されると：

1. 新しい設定を読み込む
2. 前の設定と比較して変更を検出
3. ログに変更を記録
4. メモリ内の設定を更新
5. 次のリクエストから新設定が適用される

### 3. 即座に反映

- **アプリケーション再起動：** 不要
- **リクエストの再発行：** 次のリクエストから新設定が適用
- **キャッシュの無効化：** HTTP ヘッダーなのでキャッシュされない

## CSP 違反ログの監視

CSP 違反が発生した場合、`ReportUri` で指定したエンドポイントにレポートが送信されます。

### エンドポイント実装例

```csharp
[ApiController]
[Route("api")]
public class SecurityController : ControllerBase
{
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(ILogger<SecurityController> logger)
    {
        _logger = logger;
    }

    [HttpPost("csp-report")]
    public IActionResult CspReport([FromBody] CspReportDto report)
    {
        _logger.LogWarning(
            "CSP 違反: {ViolatedDirective}, ブロック対象: {BlockedUri}",
            report.ViolatedDirective,
            report.BlockedUri);

        return Ok();
    }
}

public class CspReportDto
{
    [JsonPropertyName("violated-directive")]
    public string ViolatedDirective { get; set; } = string.Empty;

    [JsonPropertyName("blocked-uri")]
    public string BlockedUri { get; set; } = string.Empty;

    [JsonPropertyName("document-uri")]
    public string DocumentUri { get; set; } = string.Empty;
}
```

## トラブルシューティング

### Q: 設定変更が反映されない

**A:** 以下を確認してください：

1. ファイルが実際に保存されているか
2. `appsettings.json` が正しい JSON フォーマットか
3. アプリケーションのログに変更メッセージが出力されているか

ログで確認：
```
セキュリティポリシーが更新されました。新しい CSP: ...
```

### Q: 本番環境での運用方法

**A:** Docker/Kubernetes での運用を想定：

```bash
# ConfigMap として appsettings.json を管理
kubectl create configmap app-config --from-file=appsettings.json

# Pod 内でマウント
# - name: config
#   configMap:
#     name: app-config

# 設定変更時：
kubectl edit configmap app-config

# ファイルが自動的に更新され、アプリケーションが検出
```

### Q: パフォーマンスへの影響

**A:** ほぼゼロです：

- 設定チェックはメモリ内で実行（ディスク I/O なし）
- ファイル監視はバックグラウンドで実行
- リクエスト毎のオーバーヘッドは微小（ヘッダー追加のみ）

## まとめ

このシステムにより、以下が実現されます：

✅ **アプリケーション再起動なし** で CSP 設定を変更
✅ **即座に反映** される
✅ **セキュリティ侵害時に迅速対応** 可能
✅ **開発環境から本番環境** まで対応
✅ **ログで変更を追跡** 可能

本番環境での突発的なセキュリティ対応が必要な場合、このシステムが大きな力になります。

