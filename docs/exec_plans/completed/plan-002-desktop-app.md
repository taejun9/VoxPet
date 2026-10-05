# plan-002-desktop-app

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

실제 사용할 수 있도록 프로젝트의 기능 구현 및 버그 테스트를 진행한다.

## Goal

Windows용 VoxPet MVP 전체 구현, 재현 가능한 수치·수명 테스트, 빌드 및 배포 경로 제공. Windows 실기와 OBS 확인 전에는 실사용 검증 완료라고 보고하지 않는다.

## Non-Goals

음성 저장/전송, STT/감정, Live2D/Spine, 웹캠, 원격 서비스. 투명 OBS alpha 보장과 자동 업데이트는 포함하지 않는다.

## Context Map

제품/설계/개발/QA/개인정보 문서, NAudio release/2.x WasapiCapture 공식 소스, Microsoft .NET 지원 정책.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-002-desktop-app` / `.worktree/plan-002-desktop-app` 사용.
QA → 리뷰 → completed 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거.
README 원문 보존. 기존 설계 문서는 변경점을 추가하거나 필요한 부분을 편집한다.

## Implementation Plan

- [x] SDK 준비 및 Core/App/Tests 솔루션과 pinned 의존성 생성
- [x] PCM/RMS/gate/smoothing/timeout/캐릭터 수치 계약 구현
- [x] WASAPI 장치 목록·Start/Stop·오류·세션/리소스 수명 구현
- [x] PNG 자산·미리보기·blink·슬라이더·독립 방송창·설정 보존 구현
- [x] 합성 신호/가짜 캡처 버그 테스트 및 Windows CI/배포 경로 생성
- [x] QA 이후 자체 리뷰 및 문서 동기화

## QA Plan

macOS arm64: 로컬 SDK로 Core 테스트, WPF 교차 컴파일 및 win-x64 publish 시도. `python3 harness/scripts/verify_base.py`, `git diff --check` 필수.
수치: silence/sine/full scale/clipping, PCM 8/16/24/32/float, 반대 위상, invalid 입력/설정, gate 경계, smoothing 주기 독립, stale/Stop reset, blink/body 경계.
수명: fake capture Start/Stop 반복/동시 요청/오류/이전 세션 callback, 종료 정리.
Windows CI: restore/build/test/publish, headless WPF smoke. 실제 마이크·권한·제거·OBS·DPI·장시간 QA는 체크리스트로 제공하고 수행 여부를 별도로 기록한다.

## Review Plan

단일 에이전트의 QA 이후 자체 리뷰. 수식·스레드·자원·개인정보·패키징·문서 계약을 읽고 발견 사항을 수정/재검증한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-05 | roadmap 002~005의 MVP를 하나의 계획으로 구현 | 사용자 요청이 전체 실사용 기능 구현이며 기존 계획은 예시 번호임 |
| 2026-10-05 | .NET 10 LTS, NAudio 2.2.1 고정 | .NET 8의 지원 종료가 임박함; 2.x API 계약을 유지하면서 최신 LTS로 시작 |
| 2026-10-05 | Windows 실기 완료 조건 유지 | 현재 호스트는 macOS이며 WPF/WASAPI 실제 실행 불가 |
| 2026-10-05 | SDK 10.0.401 및 win-x64/single-file restore 속성 고정 | 잠금 모드 publish에서 RID/ILLink 참조 차이를 발견; 빌드/배포 restore 계약을 일치시킴 |
| 2026-10-05 | Windows QA를 위한 작업 브랜치 checkpoint commit/push 허용 | main 통합 전 원격 runner에서 WPF 실행 근거를 얻어야 함; main 완료 절차는 QA/리뷰 이후 유지 |
| 2026-10-05 | QA Python을 UTF-8로 실행, 기본 캐릭터를 보라색으로 변경 | Windows CI의 cp1252 한국어 출력 실패 수정; green screen과 캐릭터 색상 간섭 예방 |
| 2026-10-05 | NAudio AudioClient를 사용하는 bounded capture worker로 종료 오류 처리 보완 | 공식 WasapiCapture 소스의 finally client.Stop 예외가 장치 제거 후 completion을 건너뛸 수 있어, capture/stop 모두 catch 후 완료 이벤트를 보장 |
| 2026-10-05 | 입력 반응 범위 고급 설정 노출 | 고정 normalize min=-50에서는 조용한 마이크(-65 dBFS 등)가 gate를 낮춰도 반응하지 않음; min/max 순서와 기본값 복구를 UI smoke로 검증 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-05 | 지도 | main 깨끗함 및 원격 동기화 확인, 지정 worktree 생성. Git 쓰기는 sandbox escalation 후 성공 |
| 2026-10-05 | 제작 | Core/App/Tests, WASAPI shared input, PNG/MVVM/방송창/설정/고급 입력 범위 구현 |
| 2026-10-05 | 검증 | macOS: locked restore, Release build 경고/오류 0, 최종 70개 테스트 통과, win-x64 publish |
| 2026-10-05 | 검증 | Windows CI ad1d266: 70개 테스트/Release build/publish/WPF smoke 통과, 화면 확인 |
| 2026-10-05 | 심사 | QA 이후 자체 리뷰; overflow/종료 재시도/native 이중 예외/Closing 재진입/인코딩/조용한 입력 수정과 재검증 |
| 2026-10-05 | 정리 | README 원문 보존, 사용자 안내/실기 제한/검증 명령 동기화. 이 계획은 구현 및 자동 QA 범위 완료이며 전체 실사용 목표는 Windows 마이크/OBS 실기 후 판정 |

## Completion Notes

구현 및 자동 QA 범위를 완료했다. 전체 실사용 목표는 실제 마이크/OBS 실기 결과가 아직 없으므로 완료로 판정하지 않는다.

- macOS arm64 SDK 10.0.401: `dotnet restore VoxPet.sln --locked-mode`, `dotnet build VoxPet.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false -nodeReuse:false`, `dotnet test tests/VoxPet.Core.Tests/VoxPet.Core.Tests.csproj -c Release --no-build`: 최종 70/70 통과, 경고/오류 0.
- `python3 harness/scripts/verify_base.py`, `python3 harness/scripts/verify_app.py`, `git diff --check`: 통과. 완료 기록 변경 후 재실행한다.
- [Windows CI](https://github.com/taejun9/VoxPet/actions/runs/37265531490): commit ad1d266에서 locked restore, build, 70 tests, self-contained publish, WPF 바인딩/합성 반응/blink/범위/reset/Stop/일반 창 종료 smoke 모두 통과.
- 실패 및 조치: MMDeviceCollection IDisposable 가정 수정, SDK/RID/ILLink 잠금 계약 정렬, 라이선스 CRLF 공백 정규화, cp1252 출력 UTF-8 전환, WPF Closing 재진입 및 Dispatcher 경고 수정. 종료 패치 한 checkpoint가 빌드 실패 상태로 먼저 올라간 실수를 기록했고 후속 커밋과 Windows CI로 수정 검증했다.
- 코드 자체 리뷰: PCM 채널 에너지/수치 범위/시간 상수, 250ms stale, session callback 격리, native Stop 이중 예외 완료 보장, 자원 유지와 재시도, 로컬 처리/설정 저장 범위 확인.
- 남은 제한: 실제 장치 권한/USB 제거/포맷, DPI/장시간, OBS alpha/chroma key는 [실기 체크리스트](../../quality/windows-checklist.md)에서 미실행 상태로 유지한다. 사용자에게 Windows 환경 유무를 질의했다.
- 자체 포함 EXE, 라이선스, manifest와 독립 사용 안내를 배포 artifact로 생성했다. 향후 기능(STT/Live2D/에디터/클릭 통과/Browser Capture)은 이번 MVP 범위가 아니다.
- 완료 기록 후 main ff-only 병합/push, local branch 삭제/worktree 제거를 진행한다. 실패 시 이후 Git 절차를 중단하고 보고한다.
