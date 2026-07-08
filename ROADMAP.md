# SecureChat 로드맵

> 마지막 업데이트: 2026-05-18
> 현재 상태 분석: [ANALYSIS.md](ANALYSIS.md)

---

## 완료

| ID | 항목 |
|---|---|
| D1 | 공개키 회전 정책 — 최초 1회만 허용, 이미 등록 시 409 Conflict |
| D2 | 인증서 우회 코드 — `#if DEBUG` 블록 격리 |
| D3 | 공개키 캐시 — fingerprint 기반 무효화 |
| D4 | 자기 에코 메시지 로컬 저장 |
| D5 | TokenStorage — DPAPI 디스크 영속화 |
| D6 | 회원가입 FluentValidation |
| D7 | 싱글톤 HttpClient + BearerTokenHandler |
| D8 | fingerprint — DER 기반으로 변경 |
| D9 | Hub 이벤트 5종 수신 핸들러 추가 |
| D10 | `MarkAsRead` 자동 호출 |
| D11 | 공개키 없는 멤버 — 전송 차단 + UI 경고 |
| D12 | `MarkVerified` — DPAPI 로컬 저장 |
| D13 | 메시지 히스토리 로딩 |
| — | 전체 백엔드 구현 (Controller 5개, ChatHub, 서비스 6개, MessageExpirationWorker) |
| — | `ChatForm_Load` / `MainForm_Load` async void 예외 처리 |
| — | 클라이언트 `.gitignore` 추가, 서버/클라이언트 README 작성 |

---

## 남은 작업

### 🔴 버그 / 기능 누락

| 항목 | 위치 | 설명 |
|---|---|---|
| `ExceptionHandlingMiddleware` | 서버 `Api/` | 미작성. 현재 500 에러 시 스택 트레이스 노출 가능 |
| `FriendRequest.Cancel()` | 서버 `Domain/Entities/FriendRequest.cs` | `Cancelled` enum 값은 있으나 도메인 메서드 없음 |
| 보낸 친구 요청 목록 | 클라이언트 `HttpFriendRequestService` | 서버 `GET /outgoing`은 있으나 클라이언트 미구현 |
| 공개키 불일치 UI 알림 | 클라이언트 `HttpAuthService.cs:144` | 서버↔로컬 키 다를 때 로그만 남기고 사용자에게 알림 없음 |

### 🟡 미완성 기능

| 항목 | 위치 | 설명 |
|---|---|---|
| `SettingsForm` | 클라이언트 `Forms/SettingsForm.cs` | 껍데기만 있음. 프로필 편집(`PATCH /api/users/me`) + 로그아웃 구현 필요 |
| TTL 카운트다운 UI | 클라이언트 `Controls/MessageBubbleControl.cs` | 만료 임박 시각적 표시 없음 |
| 공개키 회전 감지 UI | 클라이언트 | 상대 fingerprint 변경 시 "재인증 필요" 경고 없음 |

### 🟢 운영 강화 (MVP 이후)

| 항목 | 설명 |
|---|---|
| 로그인 실패 Rate Limiting | 현재 무제한 시도 가능 |
| HSTS 헤더 | HTTPS Strict Transport Security |
| 패스워드 변경 API + UI | `PATCH /api/auth/password` |
| 계정 삭제 API | `DELETE /api/users/me` |
| SignalR 무한 백오프 재연결 | 현재 4회(1s→5s→10s→30s) 후 종료 |
| 통합 테스트 | 현재 테스트 없음 |
| Mapster 제거 | Application에 설치되어 있으나 미사용 |
