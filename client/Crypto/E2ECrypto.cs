using System.Security.Cryptography;
using System.Text;

namespace SecureChat.Crypto;

public static class E2ECrypto
{
    // RSA-2048 키쌍 생성. 반환값은 PEM 문자열.
    public static (string privatePem, string publicPem) GenerateKeyPair()
    {
        using var rsa = RSA.Create(2048);
        return (rsa.ExportRSAPrivateKeyPem(), rsa.ExportRSAPublicKeyPem());
    }

    // 수신자 공개키(PEM)로 AES 키를 RSA-OAEP(SHA-256) 암호화 → Base64
    public static string EncryptAesKey(byte[] aesKey, string recipientPublicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(recipientPublicKeyPem);
        var encrypted = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(encrypted);
    }

    // 자신의 개인키(PEM)로 AES 키 복호화
    public static byte[] DecryptAesKey(string encryptedAesKeyB64, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var encrypted = Convert.FromBase64String(encryptedAesKeyB64);
        return rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
    }

    // AES-256-GCM 암호화. 반환: (ciphertextB64, ivB64, tagB64, aesKey)
    public static (string ciphertextB64, string ivB64, string tagB64, byte[] aesKey)
        EncryptMessage(string plainText)
    {
        var aesKey = RandomNumberGenerator.GetBytes(32); // 256-bit
        var iv     = RandomNumberGenerator.GetBytes(12); // 96-bit GCM IV
        var tag    = new byte[16];
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var ciphertext = new byte[plainBytes.Length];

        using var aes = new AesGcm(aesKey, 16);
        aes.Encrypt(iv, plainBytes, ciphertext, tag);

        return (
            Convert.ToBase64String(ciphertext),
            Convert.ToBase64String(iv),
            Convert.ToBase64String(tag),
            aesKey
        );
    }

    // AES-256-GCM 복호화. 인증 실패 시 예외 발생.
    public static string DecryptMessage(string ciphertextB64, string ivB64, string tagB64, byte[] aesKey)
    {
        var ciphertext = Convert.FromBase64String(ciphertextB64);
        var iv         = Convert.FromBase64String(ivB64);
        var tag        = Convert.FromBase64String(tagB64);
        var plainBytes = new byte[ciphertext.Length];

        using var aes = new AesGcm(aesKey, 16);
        aes.Decrypt(iv, ciphertext, tag, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
