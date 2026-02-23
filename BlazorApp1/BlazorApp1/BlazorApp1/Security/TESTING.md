// このファイルは実装の検証ステップを説明しています

// 【ステップ 1】アプリケーションの起動
// dotnet run
// 
// ターミナルに以下のログが出力されることを確認：
// info: BlazorApp1.Security.SecurityPolicyService[0]
//       セキュリティポリシーが更新されました。新しい CSP: ...

// 【ステップ 2】ブラウザで確認
// URL: https://localhost:5001
// 
// F12 キーを押して開発者ツールを開く
// → Network タブ → 任意のリクエストをクリック
// → Response Headers で "Content-Security-Policy" を確認
// 例：Content-Security-Policy: script-src 'self'; connect-src 'self' https://api.trusted.com; ...

// 【ステップ 3】設定ファイルを編集（アプリケーション実行中）
// appsettings.json の ConnectSrc を編集：
// 変更前: "ConnectSrc": "'self' https://api.trusted.com"
// 変更後: "ConnectSrc": "'self'"
// 
// ファイルを保存

// 【ステップ 4】ログを確認
// ターミナルに以下のログが出力されることを確認：
// warn: BlazorApp1.Security.SecurityPolicyService[0]
//       セキュリティポリシーが更新されました。新しい CSP: script-src 'self'; connect-src 'self'; ...

// 【ステップ 5】ブラウザをリロード
// F5 キーを押してページをリロード
// 
// 開発者ツールの Network タブで、
// Content-Security-Policy ヘッダーが更新されていることを確認
// → connect-src が "'self'" のみになっている

// 【ステップ 6】CSP 違反をテスト
// ブラウザのコンソールで以下を実行：
// await fetch('https://external-api.com/data');
// 
// コンソールに以下のエラーが表示される：
// Refused to connect to 'https://external-api.com/data' because it violates the Content Security Policy directive: "connect-src 'self'".

// 【結論】
// ✅ appsettings.json を編集するだけで、アプリケーション再起動なしに CSP が変更される
// ✅ 次のリクエストから新しい設定が適用される
// ✅ セキュリティ侵害時に迅速に対応できる

