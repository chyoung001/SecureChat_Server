# SecureChat — Client

> E2E 암호화 채팅 클라이언트 (Windows Forms). **메시지는 기기를 떠나기 전에 암호화됩니다.**

- **서버 레포**: [securechat-server](https://github.com/your-username/securechat-server) *(링크 업데이트 필요)*

---

## 기술 스택

| 영역 | 기술 |
|------|------|
| 런타임 | .NET 8 (Windows) |
| UI 프레임워크 | Windows Forms |
| 실시간 통신 | SignalR Client 8 |
| 암호화 | AES-256-GCM + RSA-2048-OAEP |
| 키 저장 | Windows DPAPI (`ProtectedData`) |
| 설정 | `Microsoft.Extensions.Configuration` |
| DI | `Microsoft.Extensions.DependencyInjection` |

---

## 실행 방법

### 1. 사전 요구사항

- Windows 10 / 11
- .NET 8 SDK 또는 .NET 8 Desktop Runtime
- [securechat-server](https://github.com/your-username/securechat-server)가 먼저 실행 중이어야 함

### 2. 서버 URL 설정

`appsettings.json`의 `ServerUrl`을 실행할 서버 주소로 변경합니다.

```json
{
  "ServerUrl": "https://your-server-address",
  "HubPath": "/hubs/chat"
}
```

> ⚠️ 배포 서버 주소는 추후 업데이트 예정입니다.

### 3. 실행

```bash
dotnet run
```

또는 Visual Studio 2022에서 `F5`.

---

## 프로젝트 구조

```
SecureChat/
├── Crypto/
│   └── E2ECrypto.cs          # AES-256-GCM 암호화, RSA-OAEP 키 암호화
├── Storage/
│   ├── LocalKeyStore.cs      # RSA 키쌍 DPAPI 저장 (%LOCALAPPDATA%\SecureChat)
│   ├── TokenStorage.cs       # JWT DPAPI 영속화
│   └── VerifiedKeyStore.cs   # 수동 검증한 상대방 공개키 지문 저장
├── Services/
│   ├── ApiHttpClient.cs      # 싱글톤 HttpClient + BearerTokenHandler
│   ├── HttpAuthService.cs    # 로그인/회원가입/로그아웃/공개키 등록
│   ├── HttpRoomService.cs    # 방 CRUD, 멤버 관리
│   ├── HttpContactService.cs # 연락처 추가/삭제/차단/지문 검증
│   ├── HttpFriendRequestService.cs  # 친구 요청 송수신/수락/거절
│   └── SignalRChatService.cs # SignalR 연결, 메시지 암복호화, 이벤트 처리
├── Models/                   # ChatMessage, ChatRoom, Contact, FriendRequest 등
├── Controls/
│   ├── ChatPanel.cs          # 채팅 패널 (메시지 렌더링, 히스토리 로딩)
│   ├── MessageBubbleControl.cs  # 메시지 버블 (상태 아이콘, TTL 타이머)
│   └── ContactsPanel.cs      # 연락처 패널
├── Forms/
│   ├── LoginForm.cs          # 로그인 / 회원가입
│   ├── MainForm.cs           # 메인 (방 목록, 탭 필터, 친구 요청 배지)
│   ├── ChatForm.cs           # 채팅방
│   └── SettingsForm.cs       # 설정 (추후 구현 예정)
└── Common/
    └── SyncContextHelper.cs  # SignalR 이벤트 → UI 스레드 마샬링
```

---

## E2E 암호화 흐름

```
[송신 — 클라이언트 A]
1. 상대방 공개키 조회 (GET /api/users/{id}/public-key, fingerprint 캐시)
2. AES-256 키 K + IV(12 bytes) 랜덤 생성
3. Ciphertext, GcmTag = AES-GCM-Encrypt(plaintext, K, IV)
4. EncryptedAesKey_A = RSA-OAEP(K, A.publicKey)  ← 자신의 이력 복호화용
5. EncryptedAesKey_B = RSA-OAEP(K, B.publicKey)
6. Hub.SendEncryptedMessage({Ciphertext, IV, GcmTag, Keys: [A용, B용]})

[서버]
멤버십 검증 → DB 저장 → 수신자별 EncryptedAesKey만 필터링해 푸시

[수신 — 클라이언트 B]
K = RSA-OAEP-Decrypt(EncryptedAesKey_B, B.privateKey)
plaintext = AES-GCM-Decrypt(Ciphertext, K, IV, GcmTag)
GCM 인증 실패 시 → "변조된 메시지" 표시, 평문 렌더링 금지
```

---

## 키 관리

- **RSA-2048 키쌍**은 최초 로그인 시 기기에서 생성됩니다.
- **개인키**는 Windows DPAPI로 암호화되어 `%LOCALAPPDATA%\SecureChat\identity.key`에 저장됩니다 (같은 Windows 계정에서만 복호화 가능).
- **공개키**는 서버에 **최초 1회만** 등록됩니다. 키 교체는 관리자 개입이 필요합니다.
- **JWT**도 DPAPI로 `%LOCALAPPDATA%\SecureChat\token.dat`에 저장되어 앱 재시작 시 자동 로그인됩니다.

---

## 메시지 상태

| 아이콘 | 상태 | 설명 |
|--------|------|------|
| 🕐 (회색) | `Sending` | 전송 중 (낙관적 UI) |
| ✓ (회색) | `Sent` | 서버 수신 완료 |
| ✓✓ (회색) | `Delivered` | 상대방 기기 수신 완료 |
| ✓✓ (파란색) | `Read` | 상대방이 읽음 |
| ⚠️ | `Failed` | 전송 실패 (재시도 버튼) |
| 🕒 | `Expired` | TTL 만료로 삭제됨 |

---

## TTL (자동 삭제)

메시지 전송 시 TTL(초)을 설정할 수 있습니다.

| TTL 값 | 동작 |
|--------|------|
| `0` | 영구 보관 |
| `5` ~ `604800` (7일) | 설정 시간 후 서버·클라이언트 모두에서 삭제 |

서버 백그라운드 워커(30초 주기)와 클라이언트 로컬 타이머가 이중으로 만료를 처리합니다.

---

## 공개키 지문 검증 (TOFU)

상대방의 공개키 지문(SHA-256)을 직접 비교해 MITM 공격을 탐지할 수 있습니다.

1. 채팅방 정보 → 멤버 프로필에서 지문(fingerprint) 확인
2. 다른 경로(전화 등)로 상대방의 지문과 비교
3. 일치하면 "검증됨" 표시 — 이후 지문이 변경되면 경고

---

## 보안 주의사항

- `appsettings.json`의 `ServerUrl`은 반드시 HTTPS를 사용하세요.
- **Release 빌드**에서는 자체 서명 인증서 우회 코드가 컴파일에서 제외됩니다 (`#if DEBUG`).
- 개인키 파일(`identity.key`)을 직접 백업하거나 공유하지 마세요.

---

## 남은 작업

- `SettingsForm` 구현 (프로필 편집, 로그아웃)
- 보낸 친구 요청 목록 (`GetOutgoingRequestsAsync`)
- 공개키 불일치 감지 시 사용자 알림 UI
- TTL 카운트다운 UI
