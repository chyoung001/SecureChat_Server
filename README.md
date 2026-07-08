# SecureChat

E2E(종단간) 암호화 메신저. 클라이언트(Windows Forms)와 백엔드(ASP.NET Core)를 하나의 저장소로 통합한 모노레포입니다.

| 디렉터리 | 설명 | 스택 |
|---|---|---|
| [client/](client/) | 데스크톱 클라이언트 | .NET 8 · Windows Forms |
| [server/](server/) | 백엔드 API + 실시간 서버 | ASP.NET Core 8 · SignalR · EF Core 8 · SQLite |

각 하위 프로젝트의 상세 실행 방법은 [client/README.md](client/README.md), [server/README.md](server/README.md)를 참고하세요.

---

## 아키텍처

백엔드는 Clean Architecture 4계층 (의존 방향: `Domain ← Application ← Infrastructure ← Api`).

- **Domain** — 외부 의존성 0, 순수 엔티티 (private 생성자 + 정적 팩토리)
- **Application** — 서비스 6개 + `Result<T>` + `IUnitOfWork` + FluentValidation
- **Infrastructure** — EF Core(SQLite) + BCrypt + JWT + `MessageExpirationWorker`
- **Api** — Controller 5개 + `ChatHub`(SignalR) + Swagger + Serilog

클라이언트는 `Crypto`(E2E) · `Storage`(DPAPI) · `Services`(HTTP/SignalR) · `Controls`/`Forms`(UI)로 구성됩니다.

---

## 핵심 보안 설계

**E2E 암호화** — 서버는 평문을 절대 알지 못하며, 다음 4개 필드만 저장·중계합니다.

| 필드 | 의미 |
|---|---|
| `Iv` | AES-GCM IV (12 bytes) |
| `Ciphertext` | AES-256-GCM 암호문 |
| `HmacTag` | GCM 인증 태그 (16 bytes) |
| `EncryptedAesKey` | 수신자별 RSA-OAEP(SHA-256)로 암호화된 AES 키 |

- **키 관리**: RSA-2048 키쌍은 클라이언트 생성. 개인키는 DPAPI로 로컬 암호화 저장, 공개키는 서버에 **최초 1회만** 등록(회전 차단, 재등록 409)
- **인증**: JWT HS256(7일). `alg=none` 차단, `ClockSkew=0`, `TokenVersion` 검증으로 로그아웃/재로그인 시 기존 토큰 즉시 무효화
- **TTL**: 만료 메시지는 백그라운드 워커가 30초 주기로 삭제(영구 메시지 제외), `MessageExpired` 브로드캐스트
- **비밀번호**: BCrypt(WorkFactor 12) + 8자 이상·문자 클래스 2종 이상 검증

---

## 빌드 · 실행

```bash
# 서버
dotnet run --project server/src/SecureChat.Api

# 클라이언트
dotnet run --project client/SecureChat.csproj
```

JWT 서명키는 환경변수 또는 user-secrets로 주입합니다(예: `Jwt__SecretKey`). 저장소에 커밋하지 마세요.

---

## 배포 (Railway)

백엔드는 [server/Dockerfile](server/Dockerfile) 기준으로 Railway에 배포됩니다.

- Railway 서비스 설정에서 **Root Directory = `server`** 로 지정 (모노레포 전환에 따른 필수 설정)
- 컨테이너 기동 시 자동 마이그레이션, SQLite는 `/data` 볼륨에 저장
- `$PORT` 주입 사용, 프로덕션에서 HTTPS 리다이렉트 비활성(엣지 TLS 종단)
- **`Jwt__SecretKey` 환경변수 설정 필수** — 미설정 시 안전하지 않은 기본값으로 서명됨

---

## 구현 상태

**완료** — 백엔드 전체(엔티티 7 · 서비스 6 · Controller 5 · `ChatHub` · `MessageExpirationWorker` · 마이그레이션 2), 클라이언트 전체 채팅 플로우(E2E 암복호화 · 히스토리 로딩 · 읽음 처리 · 키 검증).

**잔여 작업**

- `FriendRequest.Cancel()` 도메인 메서드 (`Cancelled` 경로 미완)
- 전역 예외 처리 미들웨어, 통합 테스트
- `AckDelivery` 수신자 자격 검증 추가
- Swagger 프로덕션 노출 가드
- 클라이언트 `SettingsForm`(프로필 편집·로그아웃), 공개키 회전/불일치 UI 알림
- 운영 강화: 로그인 Rate Limiting, HSTS, 비밀번호 변경/계정 삭제 API
