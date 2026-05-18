using System.Security.Cryptography;
using System.Text;

namespace SecureChat.Storage;

// JWT access token을 DPAPI로 암호화해 로컬 파일에 보관.
// 메모리에는 캐시해 매 호출마다 디스크 IO를 피한다.
public class TokenStorage
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SecureChat",
        "token.dat");

    private readonly object _lock = new();
    private string? _cached;
    private bool _loaded;

    public void SaveToken(string token)
    {
        lock (_lock)
        {
            _cached = token;
            _loaded = true;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                var encrypted = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(token), null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(FilePath, encrypted);
            }
            catch
            {
                // 디스크 쓰기 실패는 무시 — 메모리 캐시는 동작하므로 현재 세션은 계속 진행.
            }
        }
    }

    public string? LoadToken()
    {
        lock (_lock)
        {
            if (_loaded) return _cached;
            _loaded = true;

            if (!File.Exists(FilePath)) return null;
            try
            {
                var encrypted = File.ReadAllBytes(FilePath);
                var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                _cached = Encoding.UTF8.GetString(plain);
                return _cached;
            }
            catch
            {
                // 파일 손상 또는 사용자 변경 — 토큰 폐기.
                return null;
            }
        }
    }

    public void ClearToken()
    {
        lock (_lock)
        {
            _cached = null;
            _loaded = true;

            if (File.Exists(FilePath))
            {
                try { File.Delete(FilePath); } catch { /* ignore */ }
            }
        }
    }
}
