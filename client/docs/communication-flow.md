# SecureChat — 통신 구조

클라이언트 ↔ 서버 ↔ 클라이언트 간 통신이 어떤 단계로 이뤄지는지 정리한 문서.
모든 메시지 평문은 **클라이언트에서만 존재**하고, 서버는 암호문(ciphertext)과 메타데이터만 본다.

---

## 1. 전체 레이어 개요

```
┌─────────────────────────┐                              ┌─────────────────────────┐
│      Client A (송신)     │                              │      Client B (수신)     │
│   Windows Forms App     │                              │   Windows Forms App     │
├─────────────────────────┤                              ├─────────────────────────┤
│   Forms / Controls      │                              │   Forms / Controls      │
│   Services              │                              │   Services              │
│   Crypto (AES+RSA)      │                              │   Crypto (AES+RSA)      │
│   Storage (DPAPI)       │                              │   Storage (DPAPI)       │
│    └ identity.key       │                              │    └ identity.key       │
│    └ token.dat          │                              │    └ token.dat          │
└───────┬─────────────────┘                              └─────────────────▲───────┘
        │                                                                  │
        │       HTTPS + WSS (TLS)                                          │
        │                                                                  │
        ▼                                                                  │
┌───────────────────────────────────────────────────────────────────────────────┐
│                    SecureChat Server (Railway)                                │
│        https://securechatserver-production.up.railway.app                     │
├───────────────────────────────────────────────────────────────────────────────┤
│  REST API  (/api/...)              │   SignalR Hub  (/hubs/chat)              │
│  ─ 인증/회원                        │   ─ 실시간 메시지 송수신                  │
│  ─ 공개키 등록·조회                  │   ─ 상태 이벤트 (Delivered/Read)         │
│  ─ 방·연락처·친구요청 CRUD            │   ─ 친구요청 알림, 방 멤버 변경           │
│  ─ 메시지 히스토리                   │                                          │
├───────────────────────────────────────────────────────────────────────────────┤
│  Database  ── 암호문(ciphertext) + 메타데이터만 저장. 평문은 절대 안 봄         │
└───────────────────────────────────────────────────────────────────────────────┘
```

두 개의 통신 채널이 동시에 열려 있다.

| 채널 | 프로토콜 | 용도 |
|------|----------|------|
| REST API | HTTPS | 인증, 공개키 등록·조회, 방/연락처/친구요청 CRUD, 메시지 히스토리 |
| SignalR Hub | WSS (WebSocket) | 실시간 메시지 송수신, 상태 이벤트, 알림 푸시 |

두 채널 모두 JWT(Bearer)로 인증한다.

---

## 2. 인증 & 키 등록 (앱 첫 실행 / 로그인 시)

```
Client A                                  Server
   │                                        │
   │  POST /api/auth/login                  │
   │  { username, password }                │
   │ ─────────────────────────────────────▶ │
   │                                        │
   │  200 OK { accessToken (JWT), user }    │
   │ ◀───────────────────────────────────── │
   │                                        │
   │  [로컬] DPAPI로 JWT 저장 (token.dat)    │
   │  [로컬] RSA-2048 키쌍 생성 (최초만)      │
   │  [로컬] 개인키 DPAPI 암호화 (identity.key)│
   │                                        │
   │  PUT /api/users/me/public-key          │
   │  { publicKeyPem }                      │
   │  Authorization: Bearer <JWT>           │
   │ ─────────────────────────────────────▶ │
   │                                        │
   │                            서버는 공개키만 저장
   │                            (서버는 절대 개인키를 모름)
```

- 개인키는 **DPAPI(CurrentUser scope)** 로 암호화돼 `%LOCALAPPDATA%\SecureChat\identity.key`에 저장.
  → 같은 Windows 계정에서만 복호화 가능. 다른 PC/계정으로는 못 옮김.
- 서버는 공개키 최초 1회만 등록 허용 (`409 Conflict` 시 기존 키 유지).

관련 코드: [HttpAuthService.cs:40-67](../Services/HttpAuthService.cs#L40-L67), [HttpAuthService.cs:123-162](../Services/HttpAuthService.cs#L123-L162), [LocalKeyStore.cs](../Storage/LocalKeyStore.cs)

---

## 3. SignalR 연결 수립

```
Client A                                  Server
   │                                        │
   │  WebSocket Upgrade /hubs/chat          │
   │  ?access_token=<JWT>                   │
   │ ═════════════════════════════════════▶ │
   │                                        │
   │              JWT 검증 후 연결 유지 (WSS)
   │  ◀══════ 영구 연결 (양방향) ═══════════▶ │
   │                                        │
   │   서버 → 클라이언트 푸시 이벤트:          │
   │   ─ ReceiveEncryptedMessage             │
   │   ─ MessageStatusChanged                │
   │   ─ ReadReceipt                         │
   │   ─ MessageExpired                      │
   │   ─ RequestReceived / RequestResponded  │
   │   ─ RoomInvited / KickedFromRoom        │
   │   ─ UserJoined/Left/AdminTransferred    │
   │   ─ SessionInvalidated                  │
```

- 끊김 시 자동 재연결: `[1s, 5s, 10s]` 세 번 시도 후 종료.
- 클라이언트 → 서버 호출: `SendEncryptedMessage`, `JoinRoom`, `LeaveRoom`, `MarkAsRead`, `AckDelivery`, `NotifyScreenCapture`.

관련 코드: [SignalRChatService.cs:73-167](../Services/SignalRChatService.cs#L73-L167)

---

## 4. 메시지 송신 흐름 (핵심)

```
[Client A — 송신자]                   [Server]                    [Client B — 수신자]

 사용자가 메시지 입력
        │
        ▼
 ① 낙관적 UI: localId(GUID) 생성
    화면에 "Sending..." 으로 표시
        │
        ▼
 ② GET /api/rooms/{roomId}              ────▶  방 멤버 목록 조회
                                                 (방 멤버: A, B, ...)
        │ ◀────  members = [A, B]
        │
        ▼
 ③ 각 멤버의 공개키 fetch
    GET /api/users/{B}/public-key       ────▶
        │ ◀──── { publicKeyPem, fingerprint }
        │  (캐시에 저장)
        │
        ▼
 ④ E2E 암호화 (로컬에서만)
    ─ AES-256 키 K, IV(96bit) 랜덤 생성
    ─ ciphertext, tag = AES-GCM(plain, K, IV)
    ─ encKey_A = RSA-OAEP(K, A.public)  ← 본인 이력 복호화용
    ─ encKey_B = RSA-OAEP(K, B.public)  ← B용
        │
        ▼
 ⑤ Hub.Invoke("SendEncryptedMessage", {
       roomId, iv, ciphertext, hmacTag,
       ttlSeconds, keys: [encKey_A, encKey_B]
   })                                   ════▶  ▼
                                          멤버십 검증
                                          DB에 ciphertext + keys 저장
                                          (평문은 모름)
                                                 │
                                                 ▼
                                          각 멤버에게 본인용 키만 필터해 푸시
                                                 │
   ⑥ 본인에게도 echo                            │
   ◀════ "ReceiveEncryptedMessage" ═══════════════┤
        │                                          │
        ▼                                          │
   localId → serverId 교체                          │
   상태: Sending → Sent                            │
                                                   ▼
                                          ════▶ "ReceiveEncryptedMessage"
                                                 │
                                                 ▼
                                          ⑦ 복호화 (B의 개인키 사용)
                                             K = RSA-Decrypt(encKey_B, B.private)
                                             plain = AES-GCM-Decrypt(...)
                                             ─ GCM 인증 실패 시 평문 렌더링 거부
                                                 │
                                                 ▼
                                          ⑧ Hub.Invoke("AckDelivery", msgId)
   ◀════ "MessageStatusChanged" (Delivered) ══════┤
        │
        ▼
   상태: Sent → Delivered (✓✓ 회색)
                                                 ▼
                                          ⑨ B가 채팅창 활성화
                                             Hub.Invoke("MarkAsRead", roomId, lastMsgId)
   ◀════ "ReadReceipt" ════════════════════════════┤
        │
        ▼
   상태: Delivered → Read (✓✓ 파란색)
```

### 단계별 요약

| 단계 | 위치 | 동작 |
|------|------|------|
| ① | Client A | localId 생성, 낙관적 UI 표시 |
| ② | REST | 방 멤버 목록 조회 |
| ③ | REST | 멤버별 공개키 조회 (캐시 사용) |
| ④ | Client A | AES 키/IV 생성, AES-GCM 암호화, 멤버별 RSA-OAEP 키 래핑 |
| ⑤ | SignalR | `SendEncryptedMessage` 호출 |
| ⑥ | SignalR | 송신자에게 echo → localId를 serverId로 교체 |
| ⑦ | Client B | 본인 개인키로 AES 키 복호화 → AES-GCM 복호화 |
| ⑧ | SignalR | `AckDelivery` → 송신자 `Delivered` 상태 |
| ⑨ | SignalR | `MarkAsRead` → 송신자 `Read` 상태 |

관련 코드: [SignalRChatService.cs:178-234](../Services/SignalRChatService.cs#L178-L234), [SignalRChatService.cs:339-414](../Services/SignalRChatService.cs#L339-L414), [E2ECrypto.cs](../Crypto/E2ECrypto.cs)

---

## 5. 메시지 상태 추적 (4단계)

| 아이콘 | 상태 | 시점 | 트리거 |
|--------|------|------|--------|
| `···` | `Sending` | 전송 시도 중 (낙관적 UI) | 로컬 localId 생성 즉시 |
| `✓` | `Sent` | 서버가 messageId 발급 | `ReceiveEncryptedMessage` echo |
| `✓✓` 회색 | `Delivered` | 상대방 기기 도달 | 상대방의 `AckDelivery` |
| `✓✓` 파랑 | `Read` | 상대방이 채팅방 열람 | 상대방의 `MarkAsRead` |
| `⚠️` | `Failed` | 전송 실패 | SignalR Invoke 예외 |
| `🕒` | `Expired` | TTL 만료 | `MessageExpired` 이벤트 |

---

## 6. 데이터 흐름 — 누가 무엇을 보는가

```
┌──────────────────────────────────────────────────────────────────┐
│                          평문 (plain text)                        │
│                                                                  │
│   Client A 메모리만 ●──────────────────────────●  Client B 메모리만 │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
            │                                          ▲
            │ AES-GCM 암호화                            │ AES-GCM 복호화
            ▼                                          │
┌──────────────────────────────────────────────────────────────────┐
│              암호문 (ciphertext + tag + iv + encKeys)             │
│                                                                  │
│   Client A ──────▶  Server (저장)  ──────▶  Client B              │
│                       │                                          │
│                       └─ DB: 영구 보관 (TTL이 있으면 만료 후 삭제)  │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

| 항목 | 서버가 볼 수 있는가? |
|------|---------------------|
| senderId, roomId, sentAt, ttlSeconds, 메시지 크기 | ✅ 본다 (메타데이터) |
| ciphertext, iv, tag, encryptedAesKey | ✅ 보지만 전부 암호문 |
| **평문 내용** | ❌ 절대 모름 |
| **AES 키 K** | ❌ 각 수신자 개인키로만 복호화 가능 |

---

## 7. 두 가지 메시지 흐름 경로

### (A) 실시간 경로 (양쪽 모두 온라인)

```
Client A  ──SignalR(SendEncryptedMessage)──▶  Server  ──Push──▶  Client B
```

### (B) 히스토리 경로 (방 진입 시)

```
Client B  ──GET /api/rooms/{id}/messages?limit=50──▶  Server
Client B  ◀──  암호문 목록 (DESC by sentAt)        ──  Server
Client B  → 각 메시지를 본인 개인키로 복호화 → store에 적재
```

관련 코드: [SignalRChatService.cs:273-329](../Services/SignalRChatService.cs#L273-L329)

---

## 8. 키와 토큰의 저장 위치

| 자산 | 위치 | 보호 |
|------|------|------|
| RSA 개인키 | `%LOCALAPPDATA%\SecureChat\identity.key` | DPAPI (CurrentUser) |
| RSA 공개키 | `%LOCALAPPDATA%\SecureChat\identity.pub` | 평문 (공개 가능) |
| JWT | `%LOCALAPPDATA%\SecureChat\token.dat` | DPAPI (CurrentUser) |
| 검증된 fingerprint | `%LOCALAPPDATA%\SecureChat\verified-keys.dat` | DPAPI (CurrentUser) |
| AES 세션 키 | 메모리만 (메시지마다 일회용) | 디스크에 안 남음 |

---

## 9. 핵심 포인트

1. **암호화는 항상 클라이언트에서만 수행** — 서버를 통과하는 데이터는 평문이 된 적이 한 번도 없다.
2. **이중 채널** — REST(상태 변경/조회) + SignalR(실시간 이벤트). JWT는 양쪽 모두에 사용.
3. **각 수신자별 키 래핑** — 그룹 채팅에서 멤버 N명이면 EncryptedAesKey가 N개. 서버는 수신자별로 본인 것만 골라 푸시.
4. **낙관적 UI + 서버 echo 기반 ID 교체** — 즉시 보이지만 서버 응답 후 정합성 맞춤.
5. **TTL 자동 삭제** — 서버(30초 주기 워커)와 클라이언트(로컬 타이머) 양쪽에서 독립 처리.
