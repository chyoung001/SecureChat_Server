# SecureChat 프로젝트 상태 분석

> 마지막 업데이트: 2026-05-18
> 남은 작업: [ROADMAP.md](ROADMAP.md)

---

## 1. 구조

| 프로젝트 | 경로 | 스택 |
|---|---|---|
| **클라이언트** | [SecureChat/](SecureChat/) | .NET 8 + Windows Forms |
| **백엔드** | [SecureChat_server/](SecureChat_server/) | ASP.NET Core 8 + SignalR + EF Core 8 + SQLite |

### 백엔드 레이어
```
Domain ← Application ← Infrastructure
                    ← Api
```
- **Domain** — 외부 의존성 0, 엔티티 7개 (private 생성자 + 정적 팩토리)
- **Application** — 서비스 6개 + `Result<T>` + `IUnitOfWork` + FluentValidation
- **Infrastructure** — EF Core SQLite + BCrypt + JWT + `MessageExpirationWorker`
- **Api** — Controller 5개 + `ChatHub` + Swagger + Serilog

### 클라이언트 구조
```
SecureChat/
├── Crypto/         # E2ECrypto (AES-256-GCM + RSA-OAEP)
├── Storage/        # LocalKeyStore, TokenStorage, VerifiedKeyStore (모두 DPAPI)
├── Services/       # Http* / SignalRChatService + ApiHttpClient
├── Models/         # ChatMessage, ChatRoom, Contact, FriendRequest, ...
├── Controls/       # ChatPanel, MessageBubbleControl, ContactsPanel, ...
├── Forms/          # MainForm, ChatForm, LoginForm, SettingsForm(미구현), ...
└── Common/         # SyncContextHelper
```

---

## 2. 핵심 보안 설계

### E2E 암호화
서버는 평문을 **절대 모름**. Base64 4개 필드만 저장·중계:

| 필드 | 의미 |
|---|---|
| `Iv` | AES-GCM IV (12 bytes) |
| `Ciphertext` | AES-256-GCM 암호문 |
| `HmacTag` | GCM 인증 태그 (16 bytes) |
| `EncryptedAesKey` | 수신자별 RSA-OAEP(SHA-256)로 암호화된 AES 키 |

흐름: 클라가 메시지마다 AES-256 키 생성 → AES-GCM 암호화 → 방 멤버 N명의 공개키로 AES 키를 N번 RSA 암호화 → Hub 전송 → 서버는 수신자별 키만 필터링해 푸시.

### 키 관리
- RSA-2048 키쌍 클라이언트 생성
- 개인키는 **DPAPI**로 `%LOCALAPPDATA%\SecureChat\identity.key`에 암호화 저장
- JWT도 **DPAPI**로 `%LOCALAPPDATA%\SecureChat\token.dat`에 영속화
- 공개키는 서버에 **최초 1회만** 등록 가능 (409 Conflict로 회전 차단)

### 인증
- JWT HS256, 7일 만료, `alg=none` 차단
- 로그아웃 시 `User.TokenVersion++` → `OnTokenValidated`에서 매 요청마다 DB 비교
- SignalR은 `?access_token=` 쿼리스트링으로 JWT 인식

### TTL 만료
- `BackgroundService` 30초 주기, 한 사이클 최대 500건
- 영구 메시지는 `ExpiresAt = DateTime.MaxValue`로 부분 인덱스 활용
- 만료 시: `LastReadMessageId` 무효화 → `MessageKey` 삭제 → `Message` 삭제 → `MessageExpired` 브로드캐스트

---

## 3. 완료된 작업 이력

### D1~D13 (2026-05-17 완료)

| ID | 항목 | 조치 |
|---|---|---|
| D1 | 공개키 회전 정책 | 최초 1회만 허용. 재등록 시 409. 같은 키 재등록은 멱등 허용 |
| D2 | 인증서 우회 코드 | `#if DEBUG` 블록으로 격리. Release 빌드에서 컴파일 제외 |
| D3 | 공개키 캐시 영구화 | fingerprint 기반 무효화. `InvalidatePublicKeyCache()` 추가 |
| D4 | 자기 에코 저장 누락 | 송신 시 로컬 store 저장. messageId 중복 검사 추가 |
| D5 | TokenStorage 인메모리 | DPAPI 디스크 영속화 + 메모리 캐시 병행 |
| D6 | 회원가입 검증 부재 | FluentValidation. 비밀번호 8자+ 문자 클래스 2종. username `[a-zA-Z0-9_]{3,32}` |
| D7 | HttpClient 매번 new | 싱글톤 + `BearerTokenHandler` DelegatingHandler |
| D8 | PEM 텍스트 fingerprint | DER(`ExportSubjectPublicKeyInfo`) 기반으로 변경 |
| D9 | Hub 이벤트 5종 미수신 | `MessageStatusChanged`, `ReadReceipt` 등 핸들러 추가 |
| D10 | `MarkAsRead` 미호출 | 방 진입 시 + 새 메시지 도착 시 자동 호출 |
| D11 | 공개키 없는 멤버 silent skip | 누락 시 `InvalidOperationException` → UI 메시지박스 |
| D12 | `MarkVerified` no-op | DPAPI 로컬 파일에 `userId → fingerprint` 저장 |
| D13 | 메시지 히스토리 미로딩 | 방 진입 시 `GET /rooms/{id}/messages` 호출, 복호화 후 store 적재 |

### 추가 구현 (2026-05-17 ~ 2026-05-18)
- 전체 백엔드 완성: AppDbContext, 마이그레이션 2개, AuthController, JWT, 서비스 6개, Controller 5개, ChatHub, MessageExpirationWorker
- `ChatForm_Load` / `MainForm_Load` async void 예외 처리 추가
- 클라이언트 `.gitignore` 추가

---

## 4. 현재 미구현 / 잔여 작업

전부 [ROADMAP.md](ROADMAP.md)에 정리됨.

### 서버
- `ExceptionHandlingMiddleware` 미작성
- `FriendRequest.Cancel()` 도메인 메서드 없음
- 통합 테스트 없음

### 클라이언트
- `SettingsForm` 껍데기만 있음 ("추후 구현" placeholder)
- 보낸 친구 요청 목록 (`GetOutgoingRequestsAsync`) 미구현
- 공개키 불일치 감지 시 UI 알림 없음 (로그만)

### 운영 강화 (MVP 이후)
- 로그인 실패 Rate Limiting
- HSTS 헤더
- 패스워드 변경 / 계정 삭제 API
- SignalR 무한 백오프 재연결

---

## 5. 참고 문서

| 파일 | 내용 |
|---|---|
| [ROADMAP.md](ROADMAP.md) | 잔여 작업 우선순위 |
| [SecureChat_server/PLAN.md](SecureChat_server/PLAN.md) | 백엔드 전체 설계 명세 |
| [SecureChat_server/CLAUDE.md](SecureChat_server/CLAUDE.md) | 백엔드 코딩 지침 |
| [SecureChat_server/SETTING.md](SecureChat_server/SETTING.md) | 패키지 버전 및 트러블슈팅 이력 |
| [SecureChat_server/README.md](SecureChat_server/README.md) | 서버 실행 방법 및 API 명세 |
| [SecureChat/README.md](SecureChat/README.md) | 클라이언트 실행 방법 |
