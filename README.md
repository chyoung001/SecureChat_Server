# SecureChat — Backend

> E2E 암호화 채팅 서버. **서버는 메시지 평문을 절대 알 수 없습니다.**

- **클라이언트 레포**: [securechat-client](https://github.com/your-username/securechat-client) *(링크 업데이트 필요)*

---

## 기술 스택

| 영역 | 기술 |
|------|------|
| 런타임 | .NET 8 (LTS) |
| 웹 프레임워크 | ASP.NET Core 8 |
| 실시간 통신 | SignalR |
| ORM | Entity Framework Core 8 + SQLite |
| 인증 | JWT Bearer (HS256, 7일 만료) |
| 비밀번호 | BCrypt (cost=12) |
| 로깅 | Serilog (콘솔 + 일별 파일) |
| 검증 | FluentValidation |

---

## 아키텍처

```
Domain ← Application ← Infrastructure
                    ← Api
```

| 레이어 | 역할 |
|--------|------|
| `SecureChat.Domain` | 외부 의존성 0. 엔티티 7개 (private 생성자 + 정적 팩토리) |
| `SecureChat.Application` | 비즈니스 로직. EF Core·ASP.NET 몰라야 함 |
| `SecureChat.Infrastructure` | EF Core, BCrypt, JWT, SignalR, BackgroundWorker |
| `SecureChat.Api` | Controller, Hub, Middleware, DI 진입점 |

---

## E2E 암호화 설계

서버는 아래 5개 Base64 필드만 저장하고 중계합니다.

| 필드 | 내용 |
|------|------|
| `Iv` | AES-GCM IV (12 bytes, 매 메시지마다 랜덤 생성) |
| `Ciphertext` | AES-256-GCM 암호문 |
| `HmacTag` | GCM 인증 태그 (16 bytes) |
| `EncryptedAesKey` | 수신자별 RSA-OAEP(SHA-256) 암호화된 AES 키 |

**흐름**: 클라이언트가 AES-256 키 생성 → AES-GCM 암호화 → 방 멤버 N명의 공개키로 AES 키를 각각 RSA 암호화 → Hub 전송 → 서버는 수신자별 개인 키만 골라 푸시.

---

## 실행 방법

### 1. 사전 요구사항

- .NET 8 SDK
- EF Core CLI: `dotnet tool install --global dotnet-ef --version "8.*"`

### 2. JWT 시크릿 설정 (User Secrets)

```bash
dotnet user-secrets set "Jwt:SecretKey" "최소-32자-이상의-랜덤-문자열" --project src/SecureChat.Api
```

> `appsettings.json`의 `"REPLACE_WITH_USER_SECRETS"` 값은 절대 직접 바꾸지 마세요.

### 3. DB 마이그레이션

```bash
dotnet ef database update \
  --project src/SecureChat.Infrastructure \
  --startup-project src/SecureChat.Api
```

### 4. 실행

```bash
dotnet run --project src/SecureChat.Api
```

서버가 시작되면:
- REST API: `https://localhost:7127`
- SignalR Hub: `https://localhost:7127/hubs/chat`
- Swagger UI: `https://localhost:7127/swagger` (개발 환경에서만)

---

## API 엔드포인트 요약

### Auth `/api/auth`
| Method | Path | 인증 | 설명 |
|--------|------|------|------|
| POST | `/register` | ✗ | 회원가입 → 201 + JWT |
| POST | `/login` | ✗ | 로그인 → 200 + JWT |
| POST | `/logout` | ✓ | TokenVersion++ → 204 |
| GET | `/me` | ✓ | 내 프로필 |

### Users `/api/users`
| Method | Path | 설명 |
|--------|------|------|
| GET | `/search?q=` | username prefix 검색 |
| GET | `/{userId}` | 공개 프로필 |
| GET | `/{userId}/public-key` | 공개키 + SHA-256 지문 |
| PUT | `/me/public-key` | 공개키 등록 (최초 1회, 회전 불가) |
| PATCH | `/me` | 프로필 수정 |

### Rooms `/api/rooms`
| Method | Path | 설명 |
|--------|------|------|
| GET | `/` | 내 방 목록 |
| POST | `/` | 그룹방 생성 |
| POST | `/direct` | 1:1 방 생성 또는 기존 반환 (멱등) |
| GET | `/{roomId}/messages` | 커서 페이지네이션 |
| POST | `/{roomId}/invite` | 멤버 초대 (관리자 전용) |
| DELETE | `/{roomId}/leave` | 방 나가기 |
| DELETE | `/{roomId}/members/{userId}` | 멤버 강퇴 (관리자 전용) |
| POST | `/{roomId}/transfer-admin` | 방장 위임 |

### Contacts `/api/contacts`
| Method | Path | 설명 |
|--------|------|------|
| GET | `/` | 연락처 목록 |
| POST | `/` | 연락처 추가 |
| DELETE | `/{userId}` | 연락처 삭제 |
| PATCH | `/{userId}/block` | 차단 토글 |

### Friend Requests `/api/friend-requests`
| Method | Path | 설명 |
|--------|------|------|
| GET | `/incoming` | 받은 요청 (Pending) |
| GET | `/outgoing` | 보낸 요청 (Pending) |
| POST | `/` | 요청 전송 |
| PATCH | `/{id}/accept` | 수락 → 양방향 Contact 자동 생성 |
| PATCH | `/{id}/reject` | 거절 |

---

## SignalR Hub (`/hubs/chat`)

JWT를 `?access_token=` 쿼리스트링으로 전달합니다.

### Client → Server
| 메서드 | 설명 |
|--------|------|
| `JoinRoom(roomId)` | 방 그룹 참여 |
| `LeaveRoom(roomId)` | 방 그룹 이탈 |
| `SendEncryptedMessage(request)` | 메시지 저장 + 브로드캐스트, messageId 반환 |
| `AckDelivery(messageId)` | 수신 확인 → 발신자에게 Delivered 이벤트 |
| `MarkAsRead(roomId, lastReadMessageId)` | 읽음 처리 |
| `NotifyScreenCapture(roomId)` | 화면 캡처 감지 알림 |

### Server → Client
| 이벤트 | 대상 | 설명 |
|--------|------|------|
| `ReceiveEncryptedMessage` | `room:{id}` | 새 메시지 (수신자별 키 필터링) |
| `MessageStatusChanged` | 발신자 | Delivered 상태 전환 |
| `ReadReceipt` | 방 멤버 | 누적적 읽음 처리 |
| `MessageExpired` | `room:{id}` | TTL 만료 (배치) |
| `RoomInvited` | `user:{id}` | 방 초대 알림 |
| `UserJoinedRoom` / `UserLeftRoom` | `room:{id}` | 멤버 변경 |
| `FriendRequestReceived` | `user:{id}` | 친구 요청 수신 |
| `FriendRequestResponded` | `user:{id}` | 친구 요청 응답 |
| `PeerScreenCaptured` | `room:{id}` | 상대 화면 캡처 감지 |
| `KickedFromRoom` | `user:{id}` | 강퇴 알림 |
| `AdminTransferred` | `room:{id}` | 방장 변경 |
| `SessionInvalidated` | `user:{id}` | 세션 무효화 (강제 로그아웃) |

---

## 보안 정책

- **공개키 회전 금지**: `PUT /users/me/public-key`는 최초 1회만 허용. 재등록 시 409 Conflict. 키 교체가 필요하면 관리자 개입 필요.
- **JWT 로그아웃 무효화**: `User.TokenVersion`을 로그아웃마다 증가. 매 요청마다 DB 값과 비교하여 탈취된 토큰 차단.
- **E2E 무결성**: AES-256-GCM의 인증 태그로 1바이트 변조 시 복호화 실패.
- **외부인 키 끼워넣기 방지**: `SendEncryptedMessage` 시 `keys` 배열의 모든 수신자가 방 멤버인지 서버가 검증.
- **Raw SQL 금지**: EF Core LINQ만 사용 (SQL 인젝션 방어).

---

## 마이그레이션 이력

| 이름 | 날짜 | 내용 |
|------|------|------|
| `InitialCreate` | 2026-05-15 | 전체 스키마 생성 |
| `AddLastReadAt` | 2026-05-17 | `RoomMember.LastReadAt` 컬럼 추가 |

새 마이그레이션 생성:
```bash
dotnet ef migrations add {MigrationName} \
  --project src/SecureChat.Infrastructure \
  --startup-project src/SecureChat.Api
```

---

## 남은 작업

- `ExceptionHandlingMiddleware` 추가
- `FriendRequest.Cancel()` 도메인 메서드
- 로그인 실패 Rate Limiting
- 통합 테스트
