# SecureChat 개선 TODO

> README의 핵심 약속 — "서버가 침해돼도, 계정이 탈취돼도, 운영자조차 못 읽음" — 을
> 클라이언트 탈취 시나리오까지 확장하기 위한 작업 목록.

---

## 🔒 보안 — README 의도 보호

### P0 (현재 의도에 직접적인 갭)

- [ ] **클라이언트 측 키 추가 보호 (패스프레이즈 래핑)**
  - 문제: DPAPI는 파일 유출만 막음. 같은 PC에서 멀웨어가 `ProtectedData.Unprotect()` 호출하면 그대로 풀림
  - 방안: `identity.key`를 사용자 패스프레이즈로 Argon2id 래핑 후 DPAPI 한번 더
  - 영향 파일: [Storage/LocalKeyStore.cs](Storage/LocalKeyStore.cs), 로그인/시작 UX
  - 라이브러리 후보: Konscious.Security.Cryptography (Argon2id)

- [ ] **키 손상/분실 복구 흐름**
  - 문제: 서버가 공개키 재등록을 409 Conflict로 영구 차단 ([Services/HttpAuthService.cs:144-151](Services/HttpAuthService.cs#L144-L151))
  - 현재: 파일 손실/탈취 시 그 계정은 영원히 봉인됨
  - 방안: 강한 재인증 + Signal식 "safety number changed" 경고로 새 키 등록 허용
  - 클라이언트 + 서버 양쪽 변경 필요

- [ ] **공개키 불일치 시 UI 알림**
  - 문제: [Services/HttpAuthService.cs:148-151](Services/HttpAuthService.cs#L148-L151) 자체 주석에 "현재 흐름에선 경고 로그만, UI 알림은 D12 작업과 함께"
  - 방안: 409 받으면 다이얼로그로 "이 PC에선 기존 메시지를 볼 수 없습니다" 명시

### P1 (의도 강화)

- [ ] **자동 잠금**
  - N분 무활동 시 메모리의 RSA 키 + 평문 메시지 `CryptographicOperations.ZeroMemory`
  - 재진입 시 패스프레이즈/Windows Hello 재인증

- [ ] **화면 캡처 차단**
  - `SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE)` (Win10 2004+)
  - [Forms/SettingsForm.cs](Forms/SettingsForm.cs)에 토글로 노출

- [ ] **Forward Secrecy 도입 (Double Ratchet)**
  - 문제: RSA 개인키 1개로 평생 모든 메시지 풂 → 키 1회 유출 = 과거 전부 노출
  - 방안: Signal Protocol 일부 또는 단순화된 ratcheting 구현
  - 학습 가치도 큼 (E2E 이해라는 README 목적과 부합)

- [ ] **JWT 토큰 보호 강화**
  - 토큰 탈취 시 메타데이터(친구 목록, 방 목록, 송수신 시각) 노출
  - 짧은 만료 + 리프레시 + 디바이스 지문 바인딩 (서버 협조)

### P2 (장기)

- [ ] TPM/Windows Hello 키 바인딩 (CNG `NCryptCreatePersistedKey`, `NCRYPT_UI_PROTECT_KEY_FLAG`)
- [ ] RSA-2048 → Curve25519/X25519 마이그레이션 검토
- [ ] 평문 메시지를 `string` 대신 `byte[]` + 사용 후 즉시 zero-out

---

## 🧩 기능 갭

- [ ] **오프라인 메시지 큐** — 현재 히스토리 로딩만, 송신 실패 시 큐잉/재시도 없음
- [ ] **공개키 지문 TOFU 자동 검증 흐름 명확화** — [Forms/VerifyDialog.cs](Forms/VerifyDialog.cs)와 [Storage/VerifiedKeyStore.cs](Storage/VerifiedKeyStore.cs)는 있는데, 언제 자동 트리거되는지 확인 필요
- [ ] **메시지 검색** — 현재 없음. 평문은 메모리에만 있어서 영구 검색 = 평문 캐시 정책 결정 필요
- [ ] **암호화된 로컬 메시지 캐시** — 현재 [Services/InMemoryMessageStore.cs](Services/InMemoryMessageStore.cs)만, 앱 재시작 시 모두 재다운로드
- [ ] **그룹방 멤버 관리 UI 점검** — [Forms/RoomInfoPanel.cs](Forms/RoomInfoPanel.cs) / [Forms/InviteMemberDialog.cs](Forms/InviteMemberDialog.cs) 동작 검증

---

## 💬 UX / 사용자 안내

- [ ] **새 PC 첫 로그인 안내** — "이전 PC의 메시지는 보호를 위해 표시되지 않습니다" 명시
- [ ] **identity.key 중요성 안내** — 분실 = 영구 손실임을 회원가입/설정에서 표시
- [ ] **연결 상태 가시화** — [Models/ConnectionState.cs](Models/ConnectionState.cs)는 있는데 UI 노출 위치 점검
- [ ] **메시지 상태 아이콘 정돈** — 4단계(Sending/Sent/Delivered/Read) UI 일관성

---

## 🛠 코드 품질 / 운영

- [ ] **로깅 점검** — 어떤 ILogger 호출에서도 평문/키/토큰이 새 나가지 않는지 grep
- [ ] **E2ECrypto 단위 테스트** — 현재 테스트 프로젝트 부재. 라운드트립 + 변조 감지 + 잘못된 키 케이스
- [ ] **공개키 캐시 무효화 정책** — [Services/SignalRChatService.cs](Services/SignalRChatService.cs)의 캐시가 키 교체 시 어떻게 갱신되는지 명확화
- [ ] **DI 인터페이스 일관성** — [Services/Mock/](Services/Mock/) 폴더와 실제 서비스의 인터페이스 동기화 확인
- [ ] **Crashreport / 텔레메트리 정책** — 만약 추가한다면 평문 누출 방지 가이드라인 필요

---

## ❓ 결정 필요 (의도 충돌 가능)

편의 vs 의도의 트레이드오프라 의식적 결정이 필요한 항목:

- [ ] 키 백업/내보내기를 허용할 것인가? (허용하면 "지정된 컴퓨터에서만" 약속이 약해짐)
- [ ] 다기기 지원할 것인가? (Signal식 — 각 디바이스가 독립 키쌍, 메시지를 N번 암호화)
- [ ] 평문 로컬 캐시를 허용할 것인가? (검색/오프라인 편의 위해)

---

## 🗺 추천 진행 순서

1. **P0 3건** — README 보안 약속을 클라이언트 탈취 시나리오까지 확장
   - 패스프레이즈 래핑 (가성비 최고)
   - 키 손상 복구 흐름 (현재 막힌 정상 시나리오 해결)
   - 409 UI 알림 (작업량 작고 자체 주석에서 이미 인지됨)
2. **자동 잠금 + 화면 캡처 차단** — UI 작업 위주, 임팩트 큼
3. **Forward Secrecy** — 학습 가치 + 보안 강화 동시
4. **나머지 기능/UX 갭** 정리
