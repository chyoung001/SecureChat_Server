using SecureChat.Models;

namespace SecureChat.Services.Mock;

public class MockContactService : IContactService
{
    private readonly List<Contact> _contacts = new()
    {
        new Contact { UserId = "u-dohyun",  Username = "dohyun_lee",  DisplayName = "이도현",  KeyFingerprint = "A1B2C3", IsVerified = true  },
        new Contact { UserId = "u-seoyeon", Username = "seoyeon",     DisplayName = "최서연",  KeyFingerprint = "D4E5F6", IsVerified = true  },
        new Contact { UserId = "u-junho",   Username = "junho_p",     DisplayName = "박준호",  KeyFingerprint = "G7H8I9", IsVerified = true  },
        new Contact { UserId = "u-jiwon",   Username = "jiwon_han",   DisplayName = "한지원",  KeyFingerprint = "J1K2L3", IsVerified = false },
        new Contact { UserId = "u-hyojin",  Username = "hyojin",      DisplayName = "효진",    KeyFingerprint = "M4N5O6", IsVerified = true  },
    };

    // 검색은 가능하지만 아직 연락처에 없는 유저 풀
    private static readonly List<Contact> _allUsers = new()
    {
        new Contact { UserId = "u-dohyun",  Username = "dohyun_lee",  DisplayName = "이도현",  KeyFingerprint = "A1B2C3", IsVerified = true  },
        new Contact { UserId = "u-seoyeon", Username = "seoyeon",     DisplayName = "최서연",  KeyFingerprint = "D4E5F6", IsVerified = true  },
        new Contact { UserId = "u-junho",   Username = "junho_p",     DisplayName = "박준호",  KeyFingerprint = "G7H8I9", IsVerified = true  },
        new Contact { UserId = "u-jiwon",   Username = "jiwon_han",   DisplayName = "한지원",  KeyFingerprint = "J1K2L3", IsVerified = false },
        new Contact { UserId = "u-hyojin",  Username = "hyojin",      DisplayName = "효진",    KeyFingerprint = "M4N5O6", IsVerified = true  },
        new Contact { UserId = "u-minjun",  Username = "minjun_k",    DisplayName = "김민준",  KeyFingerprint = "N7O8P9", IsVerified = false },
        new Contact { UserId = "u-soyeon",  Username = "soyeon_park", DisplayName = "박소연",  KeyFingerprint = "Q1R2S3", IsVerified = true  },
        new Contact { UserId = "u-taehun",  Username = "taehun",      DisplayName = "태훈",    KeyFingerprint = "T4U5V6", IsVerified = false },
    };

    public Task<List<Contact>> GetContactsAsync() => Task.FromResult(_contacts.ToList());

    public Task<Contact?> SearchUserAsync(string username)
    {
        var c = _allUsers.FirstOrDefault(x =>
            x.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(c);
    }

    public Task AddContactAsync(string userId)
    {
        if (_contacts.Any(c => c.UserId == userId)) return Task.CompletedTask;
        var user = _allUsers.FirstOrDefault(u => u.UserId == userId);
        if (user != null) _contacts.Add(new Contact
        {
            UserId       = user.UserId,
            Username     = user.Username,
            DisplayName  = user.DisplayName,
            KeyFingerprint = user.KeyFingerprint,
            IsVerified   = user.IsVerified,
        });
        return Task.CompletedTask;
    }

    public Task<Contact?> GetContactPublicKeyAsync(string userId)
        => Task.FromResult(_contacts.FirstOrDefault(c => c.UserId == userId));

    public Task MarkVerifiedAsync(string userId)
    {
        var c = _contacts.FirstOrDefault(x => x.UserId == userId);
        if (c != null) c.IsVerified = true;
        return Task.CompletedTask;
    }

    public Task RemoveContactAsync(string userId)
    {
        _contacts.RemoveAll(c => c.UserId == userId);
        return Task.CompletedTask;
    }

    public Task BlockContactAsync(string userId)
    {
        var c = _contacts.FirstOrDefault(x => x.UserId == userId);
        if (c != null) c.IsBlocked = true;
        return Task.CompletedTask;
    }
}
