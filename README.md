# SecureChat — Client

_Windows 데스크탑 E2E 암호화 채팅 클라이언트. 메시지는 기기를 떠나기 전에 암호화됩니다._

- **서버 레포**: [SecureChat_Server](https://github.com/chyoung001/SecureChat_Server)
- **배포 서버**: `https://securechatserver-production.up.railway.app`

---

## 1. 구현의도

일반 메신저를 쓰면서 들었던 의문

- 서버가 해킹되면 내 메시지도 노출되는 거 아닐까?
- 운영사가 마음먹으면 내 대화를 읽을 수 있지 않을까?

> 서버가 침해되더라도 메시지 내용을 알 수 없고,  
> 운영자조차 읽을 수 없는 채팅 앱을 직접 만들어보자.

암호화를 직접 구현하면서 E2E가 실제로 어떻게 동작하는지 이해하고 싶었던 것도 큰 이유였습니다.

---

## 2. SecureChat이 뭔가요?

**Windows Forms 기반 E2E 암호화 채팅 클라이언트**

| 기능 | 설명 |
|------|------|
| E2E 암호화 | 메시지가 기기를 떠나기 전 AES-256-GCM 암호화 알고리즘으로으로 암호화. 서버는 암호문만 저장 |
| 공개키 지문 검증 | 상대방 공개키를 직접 비교해 MITM 공격 탐지 (TOFU~ 방식) |
| TTL 자동 삭제 | 지정 시간 후 서버·클라이언트 양쪽에서 메시지 삭제 |
| 메시지 상태 추적 | 전송 중 → 서버 수신 → 기기 도달 → 읽음까지 4단계 상태 표시 |
| 친구/연락처 관리 | 친구 요청, 수락/거절, 차단 상태 유지 |
| 그룹 채팅 | 방 생성, 멤버 초대/강퇴, 어드민 위임 |

---

## 3. 기술 스택

| 항목 | 기술 | 역할 |
|------|------|------|
| 런타임 | .NET 8 (Windows) | Windows Forms 네이티브 실행 |
| UI 프레임워크 | Windows Forms | 채팅 UI, 커스텀 컨트롤 |
| 실시간 통신 | SignalR Client 8 | 메시지 송수신, 상태 이벤트 |
| 암호화 | AES-256-GCM + RSA-2048-OAEP | E2E 암호화, 키 래핑 |
| 키/토큰 저장 | Windows DPAPI (`ProtectedData`) | 개인키·JWT 로컬 암호화 저장 |
| DI / 설정 | `Microsoft.Extensions.*` | 서비스 등록, `appsettings.json` 로드 |

---

## 4. 실행 방법

### 사전 요구사항

- Windows 10 / 11
- .NET 8 Desktop Runtime

### 서버 URL 설정

`appsettings.json`의 `ServerUrl`을 서버 주소로 지정합니다.  
Railway 배포 서버를 그대로 사용하려면 수정 없이 바로 실행할 수 있습니다.

```json
{
  "ServerUrl": "https://securechatserver-production.up.railway.app",
  "HubPath": "/hubs/chat"
}
```

### 실행

```bash
dotnet run
```

또는 Visual Studio 2022에서 `F5`.

---

## 5. 아키텍처

UI와 로직, 통신을 레이어로 분리했습니다.

```
Forms / Controls (UI)
    ↕ 이벤트 / 메서드 호출
Services (비즈니스 로직)
    ↕
Storage / Crypto (로컬 저장, 암호화)
```

| 레이어 | 역할 |
|--------|------|
| **Forms / Controls** | 화면 렌더링, 사용자 입력 처리 |
| **Services** | 서버 HTTP 통신, SignalR 연결, 메시지 암복호화 |
| **Storage** | DPAPI 기반 개인키·JWT·지문 로컬 영속화 |
| **Crypto** | AES-GCM 암호화, RSA 키 래핑, 키쌍 생성 |

인터페이스(`IAuthService`, `IChatService` 등)로 실제 구현과 Mock을 교체할 수 있어서, 서버 없이도 UI 개발과 테스트가 가능합니다.

---

## 6. 핵심 기능 1 — E2E 암호화

### 왜 AES + RSA를 함께 쓰나?

RSA는 느려서 큰 데이터를 직접 암호화하기 어렵습니다.  
그래서 **빠른 AES로 메시지를 암호화하고, 느린 RSA로 AES 키만 암호화**하는 방식을 씁니다.  
각 수신자의 공개키로 AES 키를 개별 래핑하기 때문에, 그룹 채팅에서도 각자의 개인키로만 복호화할 수 있습니다.

### 암호화 흐름

```
[송신 — 클라이언트 A]
1. AES-256 키 K + IV(12 bytes) 랜덤 생성
2. Ciphertext, GcmTag = AES-GCM-Encrypt(plaintext, K, IV)
3. EncryptedAesKey_A = RSA-OAEP(K, A.publicKey)  ← 내 이력 복호화용
4. EncryptedAesKey_B = RSA-OAEP(K, B.publicKey)
5. Hub.SendEncryptedMessage({Ciphertext, IV, GcmTag, Keys: [A용, B용]})

[서버]
멤버십 검증 → DB 저장(암호문만) → 수신자별 EncryptedAesKey 필터링해 푸시

[수신 — 클라이언트 B]
K = RSA-OAEP-Decrypt(EncryptedAesKey_B, B.privateKey)
plaintext = AES-GCM-Decrypt(Ciphertext, K, IV, GcmTag)
GCM 인증 실패 시 → "변조된 메시지" 표시, 평문 렌더링 금지
```

### 구현 코드

```csharp
// E2ECrypto.cs — 메시지 암호화
public static EncryptedPayload Encrypt(string plaintext, string recipientPublicKeyPem)
{
    using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
    aes.Encrypt(iv, plaintextBytes, ciphertext, tag);

    var encryptedKey = EncryptAesKey(key, recipientPublicKeyPem); // RSA-OAEP
    return new EncryptedPayload(iv, ciphertext, tag, encryptedKey);
}
```

---

## 7. 핵심 기능 3 — TTL 자동 삭제

메시지 전송 시 TTL(초)을 지정하면 서버와 클라이언트 양쪽에서 자동 삭제됩니다.

| TTL 값 | 동작 |
|--------|------|
| `0` | 영구 보관 |
| `5` ~ `604800` (7일) | 설정 시간 후 서버·클라이언트 모두 삭제 |

삭제는 두 곳에서 독립적으로 처리됩니다.

- **서버**: 백그라운드 워커가 30초 주기로 만료된 메시지 일괄 삭제
- **클라이언트**: `MessageBubbleControl`에 로컬 타이머를 심어 만료 시 즉시 UI에서 제거

```csharp
// MessageBubbleControl.cs — TTL 타이머
_ttlTimer = new System.Windows.Forms.Timer { Interval = 1000 };
_ttlTimer.Tick += (_, _) =>
{
    var remaining = (_expiresAt - DateTime.UtcNow).TotalSeconds;
    if (remaining <= 0) { Expired?.Invoke(this, EventArgs.Empty); _ttlTimer.Stop(); }
};
```

---

## 8. 핵심 기능 4 — 메시지 상태 추적

메시지를 보내면 4단계 상태가 순서대로 업데이트됩니다.

| 아이콘 | 상태 | 시점 |
|--------|------|------|
| `···` (회색) | `Sending` | 전송 시도 중 (낙관적 UI) |
| `✓` (회색) | `Sent` | SignalR Hub가 메시지 ID 반환 |
| `✓✓` (회색) | `Delivered` | 상대방 기기에서 `AckDelivery` 호출 |
| `✓✓` (파란색) | `Read` | 상대방이 채팅방을 열어 `MarkAsRead` 호출 |
| `⚠️` | `Failed` | 전송 실패 |
| `🕒` | `Expired` | TTL 만료 |

낙관적 UI로 로컬 임시 ID를 먼저 생성하고, 서버 응답이 오면 서버 발급 ID로 교체합니다.

```csharp
// 임시 로컬 ID로 먼저 표시
var localId = Guid.NewGuid().ToString();
_messageStore.AddMessage(new ChatMessage { MessageId = localId, Status = MessageStatus.Sending });

// 서버 응답 후 실제 ID로 교체
var serverId = await _chatService.SendMessageAsync(...);
_messageStore.ReplaceMessageId(localId, serverId);
```

---

## 9. 키 관리

- **RSA-2048 키쌍**은 최초 로그인 시 기기에서 생성됩니다.
- **개인키**는 DPAPI로 암호화해 `%LOCALAPPDATA%\SecureChat\identity.key`에 저장됩니다.  
  같은 Windows 계정에서만 복호화 가능하며, 다른 기기에서는 열 수 없습니다.
- **공개키**는 서버에 최초 1회만 등록됩니다. 키 교체는 서버 측 관리자 개입이 필요합니다.
- **JWT**도 DPAPI로 `%LOCALAPPDATA%\SecureChat\token.dat`에 저장돼 앱 재시작 시 자동 로그인됩니다.

---

## 10. 트러블슈팅

### 문제 1. SignalR 이벤트를 UI 스레드에서 처리하기

SignalR 수신 콜백은 백그라운드 스레드에서 실행됩니다.  
Windows Forms 컨트롤은 UI 스레드에서만 수정할 수 있어서, 직접 호출하면 크로스 스레드 예외가 발생합니다.

```
SignalR 콜백 (백그라운드 스레드)
    ↓
Control.Invoke 없이 UI 수정 시도
    ↓
InvalidOperationException: 크로스 스레드 작업
```

**해결** — 앱 시작 시 UI `SynchronizationContext`를 캡처해두고, 모든 UI 업데이트를 `Post`로 마샬링합니다.

```csharp
// SyncContextHelper.cs
public static void Post(Action action) =>
    _uiContext?.Post(_ => action(), null) ?? action();

// 사용
SyncContextHelper.Post(() => AddMessageBubble(message));
```

---

### 문제 2. 낙관적 UI와 서버 에코 메시지 중복

메시지를 보내면 클라이언트가 먼저 로컬에 추가하고(낙관적 UI), 잠시 뒤 서버가 같은 메시지를 SignalR로 브로드캐스트합니다.  
두 번 추가되면 같은 메시지가 화면에 두 개 표시됩니다.

**해결** — `InMemoryMessageStore.AddMessage`에서 `MessageId` 기준으로 중복을 걸러냅니다.  
로컬 임시 ID는 서버 응답 후 실제 ID로 교체하므로, 에코가 도착해도 이미 동일한 ID가 존재해 무시됩니다.

```csharp
// InMemoryMessageStore.cs
if (list.Any(m => m.MessageId == message.MessageId))
    return; // 중복 무시
list.Add(message);
```

---

## 12. 프로젝트 구조

```
SecureChat/
├── Crypto/
│   └── E2ECrypto.cs                  # AES-256-GCM 암호화, RSA-OAEP 키 래핑
├── Storage/
│   ├── LocalKeyStore.cs              # RSA 키쌍 DPAPI 저장
│   ├── TokenStorage.cs               # JWT DPAPI 영속화
│   └── VerifiedKeyStore.cs           # 검증된 공개키 지문 저장
├── Services/
│   ├── ApiHttpClient.cs              # 싱글톤 HttpClient + Bearer 토큰 자동 주입
│   ├── HttpAuthService.cs            # 로그인/회원가입/공개키 서버 등록
│   ├── HttpRoomService.cs            # 방 CRUD, 멤버 관리
│   ├── HttpContactService.cs         # 연락처 추가/차단/삭제
│   ├── HttpFriendRequestService.cs   # 친구 요청 송수신/수락/거절
│   ├── SignalRChatService.cs         # SignalR 연결, E2E 암복호화, 이벤트 처리
│   └── InMemoryMessageStore.cs       # 메시지 캐시 (중복 제거, TTL 만료 관리)
├── Models/                           # ChatMessage, ChatRoom, Contact, FriendRequest 등
├── Controls/
│   ├── ChatPanel.cs                  # 채팅 패널 (메시지 렌더링, 히스토리 로딩)
│   ├── MessageBubbleControl.cs       # 메시지 버블 (상태 아이콘, TTL 타이머)
│   ├── ContactsPanel.cs              # 연락처 패널
│   ├── RoomListPanel.cs              # 방 목록 (커스텀 스크롤)
│   └── RoomListItemControl.cs        # 방 목록 항목 (미리보기, 읽지 않은 수 배지)
├── Forms/
│   ├── LoginForm.cs                  # 로그인 / 회원가입
│   ├── MainForm.cs                   # 메인 (방 목록, 연락처 탭, 친구 요청 배지)
│   ├── CreateRoomDialog.cs           # 방 생성 다이얼로그
│   ├── AddFriendDialog.cs            # 친구 추가
│   ├── FriendRequestDialog.cs        # 친구 요청 수락/거절
│   └── RoomInfoPanel.cs              # 방 정보 (멤버, 어드민 관리, 나가기)
└── Common/
    └── SyncContextHelper.cs          # SignalR 이벤트 → UI 스레드 마샬링
```

---

## 13. 보안 주의사항

- `appsettings.json`의 `ServerUrl`은 반드시 HTTPS를 사용하세요.
- Release 빌드에서는 자체 서명 인증서 우회 코드가 컴파일에서 제외됩니다 (`#if DEBUG`).
- 개인키 파일(`identity.key`)을 직접 백업하거나 공유하지 마세요.
