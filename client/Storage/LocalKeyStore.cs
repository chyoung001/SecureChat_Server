using System.Security.Cryptography;
using System.Text;

namespace SecureChat.Storage;

// 개인키는 DPAPI로 암호화해 로컬 파일에 저장. 공개키는 평문 저장.
public class LocalKeyStore
{
    private static readonly string BaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SecureChat");

    private static string PrivateKeyPath => Path.Combine(BaseDir, "identity.key");
    private static string PublicKeyPath  => Path.Combine(BaseDir, "identity.pub");

    public void SaveKeyPair(string privatePem, string publicPem)
    {
        Directory.CreateDirectory(BaseDir);

        var privateBytes = Encoding.UTF8.GetBytes(privatePem);
        var encrypted = ProtectedData.Protect(privateBytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(PrivateKeyPath, encrypted);
        File.WriteAllText(PublicKeyPath, publicPem, Encoding.UTF8);
    }

    public (string privatePem, string publicPem)? LoadKeyPair()
    {
        if (!File.Exists(PrivateKeyPath) || !File.Exists(PublicKeyPath))
            return null;

        try
        {
            var encrypted = File.ReadAllBytes(PrivateKeyPath);
            var privateBytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var privatePem = Encoding.UTF8.GetString(privateBytes);
            var publicPem  = File.ReadAllText(PublicKeyPath, Encoding.UTF8);
            return (privatePem, publicPem);
        }
        catch
        {
            return null;
        }
    }

    public void ClearKeys()
    {
        if (File.Exists(PrivateKeyPath)) File.Delete(PrivateKeyPath);
        if (File.Exists(PublicKeyPath))  File.Delete(PublicKeyPath);
    }
}
