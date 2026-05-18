using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecureChat.Storage;

// 사용자가 수동으로 검증한 상대방 공개키 fingerprint를 로컬에 저장한다.
// userId → 검증된 fingerprint. fingerprint가 바뀌면 검증 상태는 무효(파일에 더 이상 없음).
// DPAPI로 암호화 — 다른 윈도우 계정이 못 읽도록.
public class VerifiedKeyStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SecureChat",
        "verified-keys.dat");

    private readonly Dictionary<string, string> _store = new();
    private readonly object _lock = new();
    private bool _loaded;

    public bool IsVerified(string userId, string fingerprint)
    {
        EnsureLoaded();
        lock (_lock)
        {
            return _store.TryGetValue(userId, out var stored)
                && string.Equals(stored, fingerprint, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void MarkVerified(string userId, string fingerprint)
    {
        EnsureLoaded();
        lock (_lock)
        {
            _store[userId] = fingerprint;
            Save();
        }
    }

    public void Clear(string userId)
    {
        EnsureLoaded();
        lock (_lock)
        {
            if (_store.Remove(userId)) Save();
        }
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        lock (_lock)
        {
            if (_loaded) return;
            _loaded = true;

            if (!File.Exists(FilePath)) return;
            try
            {
                var encrypted = File.ReadAllBytes(FilePath);
                var plain = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(plain);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (loaded is not null)
                    foreach (var kv in loaded) _store[kv.Key] = kv.Value;
            }
            catch
            {
                // 파일 손상 시 무시 — 사용자가 다시 검증하면 된다.
            }
        }
    }

    private void Save()
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(_store);
        var encrypted = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, encrypted);
    }
}
