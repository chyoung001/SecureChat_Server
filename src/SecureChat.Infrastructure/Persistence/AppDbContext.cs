using Microsoft.EntityFrameworkCore;
using SecureChat.Domain.Entities;

namespace SecureChat.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageKey> MessageKeys => Set<MessageKey>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── User ──────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.Username).HasMaxLength(32).IsRequired();
            b.Property(u => u.PasswordHash).HasMaxLength(60).IsRequired();
            b.Property(u => u.Email).HasMaxLength(255);
            b.Property(u => u.DisplayName).HasMaxLength(64).IsRequired();

            b.HasIndex(u => u.Username)
             .IsUnique()
             .HasDatabaseName("IX_User_Username");

            // filtered: NULL 이메일은 유니크 제외
            b.HasIndex(u => u.Email)
             .IsUnique()
             .HasFilter("\"Email\" IS NOT NULL")
             .HasDatabaseName("IX_User_Email");
        });

        // ── Room ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Room>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Name).HasMaxLength(64);

            b.HasOne<User>()
             .WithMany()
             .HasForeignKey(r => r.CreatedById)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(r => r.Members)
             .WithOne(rm => rm.Room)
             .HasForeignKey(rm => rm.RoomId);

            b.HasMany(r => r.Messages)
             .WithOne()
             .HasForeignKey(m => m.RoomId);
        });

        // ── RoomMember ────────────────────────────────────────────────────
        modelBuilder.Entity<RoomMember>(b =>
        {
            b.HasKey(rm => new { rm.RoomId, rm.UserId });

            b.HasOne(rm => rm.User)
             .WithMany()
             .HasForeignKey(rm => rm.UserId)
             .OnDelete(DeleteBehavior.Restrict);

            // LastReadMessageId: nullable FK, 메시지 삭제 시 NULL 처리
            b.HasOne<Message>()
             .WithMany()
             .HasForeignKey(rm => rm.LastReadMessageId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(rm => rm.UserId)
             .HasDatabaseName("IX_RoomMember_UserId");
        });

        // ── Message ───────────────────────────────────────────────────────
        modelBuilder.Entity<Message>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Iv).HasMaxLength(24).IsRequired();
            b.Property(m => m.HmacTag).HasMaxLength(24).IsRequired();

            b.HasOne<User>()
             .WithMany()
             .HasForeignKey(m => m.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(m => m.Keys)
             .WithOne(mk => mk.Message)
             .HasForeignKey(mk => mk.MessageId);

            b.HasIndex(m => new { m.RoomId, m.SentAt })
             .HasDatabaseName("IX_Message_RoomId_SentAt");

            // partial: DateTime.MaxValue('9999-12-31...') 는 인덱스 제외 → 만료 스캔 최적화
            b.HasIndex(m => m.ExpiresAt)
             .HasFilter("\"ExpiresAt\" < '9999-12-31'")
             .HasDatabaseName("IX_Message_ExpiresAt");
        });

        // ── MessageKey ────────────────────────────────────────────────────
        modelBuilder.Entity<MessageKey>(b =>
        {
            b.HasKey(mk => new { mk.MessageId, mk.RecipientUserId });
            b.Property(mk => mk.EncryptedAesKey).HasMaxLength(512).IsRequired();

            b.HasOne<User>()
             .WithMany()
             .HasForeignKey(mk => mk.RecipientUserId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(mk => mk.RecipientUserId)
             .HasDatabaseName("IX_MessageKey_RecipientUserId");
        });

        // ── Contact ───────────────────────────────────────────────────────
        modelBuilder.Entity<Contact>(b =>
        {
            b.HasKey(c => new { c.OwnerUserId, c.ContactUserId });
            b.Property(c => c.Nickname).HasMaxLength(64);

            b.HasOne(c => c.Owner)
             .WithMany()
             .HasForeignKey(c => c.OwnerUserId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.ContactUser)
             .WithMany()
             .HasForeignKey(c => c.ContactUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── FriendRequest ─────────────────────────────────────────────────
        modelBuilder.Entity<FriendRequest>(b =>
        {
            b.HasKey(fr => fr.Id);
            b.Property(fr => fr.Message).HasMaxLength(200);

            b.HasOne(fr => fr.FromUser)
             .WithMany()
             .HasForeignKey(fr => fr.FromUserId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(fr => fr.ToUser)
             .WithMany()
             .HasForeignKey(fr => fr.ToUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
