# 🔐 SecureChat — E2E 암호화 채팅 백엔드

![Platform](https://img.shields.io/badge/Platform-Railway-blueviolet)
![Framework](https://img.shields.io/badge/.NET-8.0-purple)
![ORM](https://img.shields.io/badge/EF_Core-8.0-blue)
![Auth](https://img.shields.io/badge/Auth-JWT_HS256-orange)
![Realtime](https://img.shields.io/badge/Realtime-SignalR-green)

> **서버가 메시지 평문을 절대 알 수 없는** E2E 암호화 채팅 서버입니다.  
> 모든 메시지는 클라이언트에서 암호화되어 도착하고, 서버는 암호문만 저장·중계합니다.

- 🚀 **배포 주소**: `https://securechatserver-production.up.railway.app`
- 💻 **클라이언트 레포**: [SecureChat-client](https://github.com/your-username/securechat-client)
- 📖 **Swagger UI**: `https://securechatserver-production.up.railway.app/swagger`

---

## 1. 기존 채팅 앱과 무엇이 다른가? 

- 서버 운영자가 메시지를 읽을 수 있는 구조
- TTL 메시지를 클라이언트 타이머에만 의존하는 구조

> 서버가 메시지를 **물리적으로** 알 수 없고,  
> TTL 메시지는 **서버가 강제로** 삭제하는 채팅 백엔드를 만들고 싶었습니다.

---

## 2. 기술 스택

| 영역 | 기술 | 버전 |
|------|------|------|
| 런타임 | .NET | 8.0 (LTS) |
| 웹 프레임워크 | ASP.NET Core | 8.0 |
| 실시간 통신 | SignalR | 8.0 |
| ORM | Entity Framework Core + SQLite | 8.0.27 |
| 인증 | JWT Bearer (HS256, 7일 만료) | 8.0.27 |
| 비밀번호 해싱 | BCrypt.Net-Next (cost=12) | 4.2.0 |
| 로깅 | Serilog | 10.0.0 |
| 검증 | FluentValidation | 12.1.1 |
| 배포 | Railway (Docker) | — |

---

## 3. 아키텍처

```
Domain  ←  Application  ←  Infrastructure
                        ←  Api
```

의존성이 항상 안쪽 레이어를 향합니다. `Domain`은 외부 패키지를 전혀 모릅니다.

| 레이어 | 역할 | 규칙 |
|--------|------|------|
| `SecureChat.Domain` | 엔티티 7개, Enum | 외부 패키지 참조 0개 |
| `SecureChat.Application` | 비즈니스 로직, 서비스 인터페이스 | EF Core·ASP.NET 몰라야 함 |
| `SecureChat.Infrastructure` | EF Core, BCrypt, JWT, SignalR 구현체 | 외부 라이브러리는 여기에만 |
| `SecureChat.Api` | Controller, Hub, Middleware | 비즈니스 로직 없음 |

### 엔티티 패턴

```csharp
// 항상 private 생성자 + 정적 팩토리 메서드
public class User : EntityBase
{
    private User() { }  // EF Core용

    public static User Create(string username, string passwordHash)
        => new User { Username = username, PasswordHash = passwordHash, ... };
}
```

---

## 4. E2E 암호화 설계

서버는 아래 필드만 저장합니다. 평문 복호화 로직은 서버에 존재하지 않습니다.

| 필드 | 설명 |
|------|------|
| `Iv` | AES-GCM IV (12 bytes, 메시지마다 랜덤) |
| `Ciphertext` | AES-256-GCM 암호문 |
| `HmacTag` | GCM 인증 태그 (변조 감지) |
| `EncryptedAesKey` | 수신자별 RSA-OAEP로 암호화된 AES 키 |

### 메시지 송수신 흐름

```
[클라이언트 A — 송신]
1. AES-256-GCM 키 K + IV 랜덤 생성
2. (ciphertext, hmacTag) = AES-GCM(plaintext, K, IV)
3. encKey_A = RSA-OAEP(K, A.publicKey)  ← 본인 이력용
4. encKey_B = RSA-OAEP(K, B.publicKey)

[서버 — 트랜잭션]
Hub.SendEncryptedMessage()
  → 멤버십 검증 → keys 배열 검증
  → INSERT Message + MessageKey(A용) + MessageKey(B용)
  → Return messageId

[클라이언트 B — 수신]
K = RSA-OAEP-Decrypt(encKey_B, B.privateKey)
plaintext = AES-GCM-Decrypt(ciphertext, K, iv, hmacTag)
// HMAC 실패 시 → "변조된 메시지" 표시, 평문 렌더링 금지
```

---

## 5. 메시지 데이터 파이프라인

메시지 하나가 전송되고 만료될 때까지 서버를 거치는 전체 흐름입니다.

```
┌─────────────────────────────────────────────────────────────────┐
│  클라이언트 A (송신)                                              │
│                                                                   │
│  평문 입력                                                        │
│    │                                                              │
│    ▼                                                              │
│  AES-256-GCM 암호화 (K, IV 랜덤 생성)                            │
│    │   ├─ ciphertext, hmacTag                                     │
│    │   └─ encKey_A = RSA-OAEP(K, A.pubKey)  ← 본인 이력용        │
│    │   └─ encKey_B = RSA-OAEP(K, B.pubKey)                       │
│    │                                                              │
│    ▼                                                              │
│  SignalR Hub.SendEncryptedMessage()  ──────────────────────────► │
└─────────────────────────────────────────────────────────────────┘
                                          │
                    ┌─────────────────────▼──────────────────────┐
                    │  서버 (ChatHub)                              │
                    │                                              │
                    │  1. JWT 검증 (Context.UserIdentifier)        │
                    │  2. 방 멤버십 확인                           │
                    │  3. keys 배열 수신자 전원 멤버 여부 검증     │
                    │  4. TtlSeconds 허용 범위 확인                │
                    │        │                                     │
                    │        ▼  SQLite 트랜잭션                    │
                    │  INSERT Messages                             │
                    │  INSERT MessageKeys × N (수신자 수만큼)      │
                    │  UPDATE Rooms.LastMessageAt                  │
                    │        │                                     │
                    │        ▼                                     │
                    │  Broadcast → room:{roomId}                   │
                    │  (각 수신자에게 본인 encKey만 필터링하여 전송) │
                    └──────────────────────────────────────────────┘
                          │                         │
           ┌──────────────▼──────┐     ┌────────────▼──────────────┐
           │  클라이언트 A (수신)  │     │  클라이언트 B (수신)        │
           │                     │     │                            │
           │  encKey_A로 복호화   │     │  encKey_B로 복호화         │
           │  → 본인 전송 이력 표시│     │  K = RSA-OAEP-Decrypt()   │
           │                     │     │  평문 = AES-GCM-Decrypt()  │
           │  Status: Sent ✓     │     │  HMAC 실패 → 변조 경고     │
           └─────────────────────┘     └────────────────────────────┘
```
---

## 6. DB 스키마

```
┌──────────┐       ┌─────────────┐       ┌──────────┐
│  User    │──┐    │ RoomMember  │    ┌──│  Room    │
│──────────│  │    │─────────────│    │  │──────────│
│ Id (PK)  │  └───►│ UserId (FK) │◄───┘  │ Id (PK)  │
│ Username │       │ RoomId (FK) │       │ Name     │
│ PassHash │       │ IsAdmin     │       │ IsDirect │
│ Email    │       │ JoinedAt    │       │CreatedById│
│ PublicKey│       │LastReadMsgId│       └────┬─────┘
│ TokenVer │       └─────────────┘            │
└────┬─────┘                                  │
     │                              ┌─────────▼──────┐
     │   ┌──────────────────────────│   Message      │
     │   │                          │────────────────│
     │   │  ┌───────────────────────│ Id (PK)        │
     │   │  │  MessageKey           │ RoomId (FK)    │
     │   │  │  ─────────────────    │ SenderId (FK)  │
     │   │  └─►MessageId (FK,PK)   │ Iv             │
     │   └────►RecipientUserId(FK) │ Ciphertext     │
     │         EncryptedAesKey     │ HmacTag        │
     │                             │ TtlSeconds     │
     │                             │ ExpiresAt      │
     │                             └────────────────┘
     │
     │   ┌──────────────┐     ┌─────────────────┐
     ├──►│   Contact    │     │  FriendRequest  │
     │   │──────────────│     │─────────────────│
     │   │OwnerUserId   │     │ Id (PK)         │
     │   │ContactUserId │     │ FromUserId (FK) │
     │   │ IsBlocked    │     │ ToUserId (FK)   │
     └──►│ Nickname     │     │ Status          │
         └──────────────┘     │ Message         │
                              └─────────────────┘
```

### 관계 요약

| 관계 | 카디널리티 | 설명 |
|------|-----------|------|
| User ↔ Room | M:N | `RoomMember`가 중간 테이블 |
| Message → Room | N:1 | 메시지는 하나의 방에 속함 |
| Message → User | N:1 | 발신자 참조 |
| MessageKey → Message | N:1 | 수신자 수만큼 키 생성 |
| MessageKey → User | N:1 | 수신자 참조 |
| Contact → User | N:1 (×2) | OwnerUserId, ContactUserId |
| FriendRequest → User | N:1 (×2) | FromUserId, ToUserId |

---

## 7. 메시지 상태 전이

클라이언트 측 상태 머신입니다. 서버는 `Sent` 이후 상태만 이벤트로 알립니다.

```
                    [사용자 전송]
                         │
                         ▼
                    ┌─────────┐
                    │ Sending │  ← optimistic UI, 로컬에만 존재
                    └────┬────┘
           ┌─────────────┴─────────────┐
    Hub 응답 성공                  타임아웃(10초) / Hub 예외
    messageId 수신                       │
           │                             ▼
           ▼                        ┌────────┐
       ┌───────┐   재시도(최대 3회)  │ Failed │──► 재시도 버튼 표시
       │  Sent │◄───────────────────└────────┘
       └───┬───┘
           │  상대방이 AckDelivery() 호출
           │  서버 → 발신자: MessageStatusChanged(Delivered)
           ▼
     ┌───────────┐
     │ Delivered │
     └─────┬─────┘
           │  상대방이 MarkAsRead() 호출
           │  서버 → room: ReadReceipt { lastReadMessageId }
           │  lastReadMessageId ≥ 이 메시지 Id
           ▼
       ┌────────┐
       │  Read  │
       └────────┘

  ※ 어떤 상태에서든 TTL 만료 시
           │
           ▼
      ┌─────────┐
      │ Expired │  ← MessageExpired 이벤트 or 로컬 타이머
      └─────────┘
```

### UI 표시 규칙

| 상태 | 아이콘 | 색상 |
|------|--------|------|
| `Sending` | 🕐 시계 | 회색 |
| `Sent` | ✓ 1개 | 회색 |
| `Delivered` | ✓✓ 2개 | 회색 |
| `Read` | ✓✓ 2개 | 파란색 |
| `Failed` | ⚠️ | 빨간색 + 재시도 버튼 |
| `Expired` | — | 메시지 제거 또는 "🕒 만료됨" |

---

## 8. 재연결 시나리오

SignalR은 네트워크 끊김을 자동 감지하고 재연결을 시도합니다.  
재연결 성공 후 미수신 메시지를 REST API로 동기화합니다.

```
[네트워크 끊김 감지]
        │
        ▼
  Reconnecting 상태
  재시도 간격: 1s → 5s → 10s → 30s
        │
        ├─ 성공 ──────────────────────────────────┐
        │                                         │
        └─ 4회 모두 실패                           ▼
                │                         [재연결 성공]
                ▼                                 │
          Disconnected                            ▼
          사용자에게 수동 재연결 안내    GET /api/rooms
                                       (방 목록 + UnreadCount 동기화)
                                                  │
                                                  ▼
                                   GET /api/rooms/{id}/messages
                                          ?after={lastReceivedAt}
                                   (재연결 전 미수신 메시지 수신)
                                                  │
                                                  ▼
                                   Hub.JoinRoom(currentRoomId)
                                   (현재 방 그룹 재참여)
                                                  │
                                                  ▼
                                          정상 상태 복구 ✓
```

### Reconnecting 중 발송 시도

```
사용자가 메시지 전송 시도
        │
        ▼
OutboundQueue에 보관 (Status: Sending)
        │
재연결 성공 후
        │
        ▼
Queue FIFO 순서로 재전송
        │
        ├─ 성공 → Status: Sent
        └─ 3회 실패 → Status: Failed (재시도 버튼)
```

---

## 9. 로컬 실행

### 사전 요구사항

- .NET 8 SDK
- EF Core CLI: `dotnet tool install --global dotnet-ef --version "8.*"`

### JWT 시크릿 설정

```bash
dotnet user-secrets set "Jwt:SecretKey" "최소-32자-이상의-랜덤-문자열" --project src/SecureChat.Api
```

> `appsettings.json`의 `"REPLACE_WITH_USER_SECRETS"` 값은 직접 수정하지 마세요.

### DB 마이그레이션 + 실행

```bash
dotnet ef database update \
  --project src/SecureChat.Infrastructure \
  --startup-project src/SecureChat.Api

dotnet run --project src/SecureChat.Api
```

실행 후 접속:
- REST API: `https://localhost:7127`
- SignalR Hub: `https://localhost:7127/hubs/chat`
- Swagger UI: `https://localhost:7127/swagger`

---

## 10. API 엔드포인트

공통: 모든 응답 JSON. 인증 필요 시 `Authorization: Bearer {jwt}`. 오류는 RFC 7807 ProblemDetails.

### Auth `/api/auth`
| Method | Path | 인증 | 설명 |
|--------|------|:----:|------|
| POST | `/register` | ✗ | 회원가입 → 201 + JWT |
| POST | `/login` | ✗ | 로그인 → 200 + JWT |
| POST | `/logout` | ✓ | TokenVersion++ → 204 |
| GET | `/me` | ✓ | 내 프로필 |

### Users `/api/users`
| Method | Path | 설명 |
|--------|------|------|
| GET | `/search?q=` | username prefix 검색 (3자 이상) |
| GET | `/{userId}` | 공개 프로필 |
| GET | `/{userId}/public-key` | 공개키 + SHA-256 지문 |
| PUT | `/me/public-key` | 공개키 등록 |
| PATCH | `/me` | 프로필 수정 |

### Rooms `/api/rooms`
| Method | Path | 설명 |
|--------|------|------|
| GET | `/` | 내 방 목록 (UnreadCount + LastMessage 포함) |
| POST | `/` | 그룹방 생성 |
| POST | `/direct` | 1:1 방 생성 또는 기존 반환 (멱등) |
| GET | `/{roomId}/messages` | 커서 페이지네이션 (`?before=` / `?after=`) |
| POST | `/{roomId}/invite` | 멤버 초대 (관리자 전용) |
| DELETE | `/{roomId}/leave` | 방 나가기 |

### Contacts · FriendRequests
| Method | Path | 설명 |
|--------|------|------|
| GET | `/api/contacts` | 연락처 목록 |
| POST | `/api/contacts` | 연락처 추가 |
| PATCH | `/api/contacts/{userId}/block` | 차단 토글 |
| POST | `/api/friend-requests` | 친구 요청 전송 |
| PATCH | `/api/friend-requests/{id}/accept` | 수락 → 양방향 Contact 자동 생성 |
| PATCH | `/api/friend-requests/{id}/reject` | 거절 |

---

## 11. SignalR Hub (`/hubs/chat`)

JWT를 `?access_token=` 쿼리스트링으로 전달합니다. (SignalR 클라이언트는 헤더 대신 쿼리스트링 사용)

### Client → Server
| 메서드 | 설명 |
|--------|------|
| `JoinRoom(roomId)` | 방 그룹 참여, 멤버십 검증 |
| `LeaveRoom(roomId)` | 방 그룹 이탈 |
| `SendEncryptedMessage(request)` | 메시지 저장 + 브로드캐스트, messageId 반환 |
| `AckDelivery(messageId)` | 수신 확인 → 발신자에게 Delivered 이벤트 |
| `MarkAsRead(roomId, lastReadMessageId)` | 읽음 처리 |
| `NotifyScreenCapture(roomId)` | 화면 캡처 감지 알림 |

### Server → Client
| 이벤트 | 대상 | 설명 |
|--------|------|------|
| `ReceiveEncryptedMessage` | `room:{id}` | 새 메시지 (수신자별 키 필터링) |
| `MessageStatusChanged` | 발신자만 | Delivered 상태 전환 |
| `ReadReceipt` | 방 멤버 | 누적적 읽음 처리 |
| `MessageExpired` | `room:{id}` | TTL 만료 배치 알림 |
| `RoomInvited` | `user:{id}` | 방 초대 알림 |
| `FriendRequestReceived` | `user:{id}` | 친구 요청 수신 |
| `FriendRequestResponded` | `user:{id}` | 친구 요청 응답 |
| `PeerScreenCaptured` | `room:{id}` | 상대 화면 캡처 감지 |

---


## 12. Railway 배포

Docker 기반으로 Railway에 배포합니다. 앱 시작 시 `MigrateAsync()`로 DB를 자동 생성합니다.

### 필수 환경 변수

| Key | 설명 |
|-----|------|
| `Jwt__SecretKey` | HS256 서명 키 (32자 이상 필수) |
| `Cors__AllowedOrigins__0` | 허용할 프론트엔드 URL |

### Volume 설정

SQLite 데이터 유지를 위해 `/data` 경로에 Persistent Volume을 마운트합니다.  
`appsettings.Production.json`의 DB 경로: `Data Source=/data/securechat.db`

---

## 13. 트러블슈팅

### HTTPS 인증서 오류 (컨테이너 시작 실패)

**증상**: `Unable to configure HTTPS endpoint. No server certificate was specified.`

**원인**: `launchSettings.json`의 `ASPNETCORE_URLS=https://localhost:7001`이 컨테이너 환경에서 읽혀 HTTPS를 시도했으나 인증서 없음.

**해결**:
1. `.dockerignore`에 `**/Properties/launchSettings.json` 추가
2. `Dockerfile`의 `CMD`에서 Railway의 `$PORT` 환경 변수를 직접 바인딩

```dockerfile
CMD ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet SecureChat.Api.dll"]
```

3. `Program.cs`에서 Production 환경의 HTTPS 리다이렉션 제거 (Railway 프록시가 처리)

```csharp
if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();
```

### JWT 키 크기 오류 (로그인 500)

**증상**: `IDX10720: key size must be greater than: '256' bits, key has '200' bits`

**원인**: Railway 환경 변수 `Jwt__SecretKey`에 `appsettings.json` 기본값인 `REPLACE_WITH_USER_SECRETS`(25자=200비트)가 그대로 사용됨.

**해결**: Railway Variables에 `Jwt__SecretKey` 키로 32자 이상의 랜덤 문자열 등록.  
(변수명 주의: `__`는 언더바 두 개, .NET 계층형 설정 바인딩 형식)

### 컨테이너 재시작마다 DB 초기화

**증상**: 재배포 후 기존 유저 데이터 소실, `Applying migration 'InitialCreate'` 로그 반복.

**원인**: Railway Persistent Volume 미마운트 상태에서 컨테이너 재시작 시 새 임시 파일시스템이 생성됨.

**해결**: Railway 대시보드 → Volumes → `/data` 경로에 Volume 마운트.

---

## 14. 프로젝트 구조

```
SecureChat_server/
├── src/
│   ├── SecureChat.Domain/
│   │   ├── Common/EntityBase.cs
│   │   ├── Entities/          # User, Room, RoomMember, Message, MessageKey, Contact, FriendRequest
│   │   └── Enums/             # FriendRequestStatus, MessageStatus
│   ├── SecureChat.Application/
│   │   ├── Auth/              # IAuthService, DTOs
│   │   ├── Common/            # IUnitOfWork, ICurrentUser, Result<T>
│   │   └── Abstractions/      # IPasswordHasher, IJwtTokenService, IRealtimeNotifier
│   ├── SecureChat.Infrastructure/
│   │   ├── Persistence/       # AppDbContext, Configurations, Migrations
│   │   ├── Identity/          # JwtTokenService, BCryptPasswordHasher
│   │   ├── Realtime/          # SignalRRealtimeNotifier
│   │   └── BackgroundJobs/    # MessageExpirationWorker
│   └── SecureChat.Api/
│       ├── Controllers/       # Auth, Users, Rooms, Contacts, FriendRequests
│       ├── Hubs/ChatHub.cs
│       └── Program.cs
├── Dockerfile
└── README.md
```
