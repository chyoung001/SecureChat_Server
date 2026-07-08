# SecureChat — 모노레포

E2E 암호화 메신저. 클라이언트(Windows Forms)와 백엔드(ASP.NET Core)를 하나의 저장소로 통합했습니다.

| 디렉터리 | 설명 | 스택 |
|---|---|---|
| [client/](client/) | 데스크톱 클라이언트 | .NET 8 · Windows Forms |
| [server/](server/) | 백엔드 API + 실시간 서버 | ASP.NET Core 8 · SignalR · EF Core 8 · SQLite |

## 문서

| 파일 | 내용 |
|---|---|
| [ANALYSIS.md](ANALYSIS.md) | 프로젝트 상태 분석 |
| [ROADMAP.md](ROADMAP.md) | 잔여 작업 우선순위 |
| [client/README.md](client/README.md) | 클라이언트 실행 방법 |
| [server/README.md](server/README.md) | 서버 실행 방법 · API 명세 |
| [server/CLAUDE.md](server/CLAUDE.md) | 백엔드 코딩 지침 (Claude Code) |
| [server/PLAN.md](server/PLAN.md) | 백엔드 전체 설계 명세 |

## 빌드

```bash
# 서버
dotnet build server/SecureChat.sln

# 클라이언트
dotnet build client/SecureChat.sln
```

## 배포 (Railway)

백엔드는 [server/Dockerfile](server/Dockerfile) 기준으로 Railway에 배포됩니다.
모노레포 전환에 따라 Railway 서비스 설정에서 **Root Directory = `server`** 로 지정해야 합니다.

## 핵심 보안 설계

- **E2E 암호화**: 서버는 평문을 절대 알지 못하며 `Iv / Ciphertext / HmacTag / EncryptedAesKey`만 저장·중계
- **키 관리**: RSA-2048 키쌍은 클라이언트 생성, 개인키는 DPAPI로 로컬 암호화 저장, 공개키는 서버에 최초 1회만 등록
- **인증**: JWT HS256 + `TokenVersion` 검증(로그아웃/재로그인 시 기존 토큰 무효화)
- **TTL**: 만료 메시지는 백그라운드 워커가 30초 주기로 삭제
