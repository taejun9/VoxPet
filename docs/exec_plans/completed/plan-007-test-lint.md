# plan-007-test-lint

## Status

completed

## Owner

project_lead / quality_runner / review_judge

## User Request

모든 테스트와 린트를 clean 상태로 만들고 실제 프로젝트 화면으로 테스트한다.

## Goal

현재 commit의 locked restore, Release build(경고/오류 0), Core 전체 테스트, 문서/자산 검증, 공백 및 .NET format 검사를 수행한다.
Windows에서 실제 WPF 앱을 실행한 smoke/layout QA와 화면 PNG를 확인하고 실패를 수정한다.

## Non-Goals

macOS에서 Windows 앱 실행을 가장하기, 합성 입력을 실제 마이크로 주장하기, UI 복제품 제작, 음성 저장/전송, 사용자 OBS 설정 변경.

## Context Map

docs/quality/development.md, docs/quality/rules.md, docs/quality/windows-checklist.md,
harness/scripts/qa.ps1, .github/workflows/windows.yml, .github/workflows/obs.yml,
src/VoxPet.App/Views/LayoutQa.cs, src/VoxPet.App/Views/MainWindow.xaml.cs, tests/VoxPet.Core.Tests.

## Constraints

No Exec Plan, No Work. `codex/plan-007-test-lint` / `.worktree/plan-007-test-lint` 사용.
QA → 자체 리뷰 → completed 이동/리뷰 미러 → main ff-only 병합/push → branch -d → worktree 제거.
실패한 Git 단계는 원인을 해결한 뒤에만 재시도한다. 실제 Windows 장치 접근 없이 마이크 실기 통과를 기록하지 않는다.

## Implementation Plan

- [x] 현재 검증 명령/실제 UI QA 경로와 환경 확인
- [x] 로컬 전체 테스트·빌드·린트 실행 및 필요한 수정
- [x] Windows 실제 앱 창 smoke/layout 및 OBS 화면 실행·증거 확인
- [x] QA 이후 자체 리뷰·문서 완료 기록; 완료 커밋 뒤 지정된 Git 수명 절차 수행

## QA Plan

SDK 10.0.401로 locked restore, Release build, Core tests, `dotnet format VoxPet.sln --verify-no-changes --no-restore`,
`python3 harness/scripts/verify_base.py`, `python3 harness/scripts/verify_app.py`, stage 후 `git diff --check --cached`.
Windows CI `qa.ps1 -Publish -Smoke`: 실제 WPF 창/입력/Start·Stop/데모/방송창/resize/scroll 검사, JSON failures=0 및 PNG 직접 확인.
OBS CI는 실제 방송창을 WGC로 캡처한 green/transparent 이미지와 합성 변화 결과를 검사한다.
실제 마이크, 사용자 GPU, 실제 DPI 변경/입력 60분은 Windows 실기 접근이 없으면 미검증으로 남긴다.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰로 변경 범위, 테스트 근거, 스레드/오디오/개인정보 계약과 문서 정확성을 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-05 | macOS에서 Core/교차 빌드/린트, Windows CI에서 실제 WPF 앱 화면 QA | 현재 호스트에서 WPF 실행 불가; 실제 앱 실행/화면 증거 요구를 Windows runner로 검증 |
| 2026-10-05 | 검증 checkpoint 브랜치를 push하고 Windows/OBS workflow 실행 | main 통합 전에 현재 변경의 실제 Windows QA 필요 |
| 2026-10-05 | SDK formatter로 기존 C# 공백 위반 수정 및 qa.ps1에 format gate 추가 | 전체 린트 검사에서 WHITESPACE 오류 발견; 재발 방지를 Windows 필수 QA에 포함 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-05 | 지도 | main clean 및 origin/main=11b8dee 확인. 첫 fetch의 sandbox FETCH_HEAD 제한은 승인된 실행으로 해결하고 전용 branch/worktree 생성 |
| 2026-10-05 | 검증 | NuGet 네트워크/CLI home 및 테스트 소켓 sandbox 제한을 실행 환경 조정으로 해결. locked restore 통과, Release 경고/오류0, Core71/71 통과. 첫 format 검사에서 기존 공백 위반을 재현 |
| 2026-10-05 | 제작 | C# 8개 파일의 WHITESPACE179건을 SDK formatter로 수정. 공백 제거 전후 내용 동일 확인. qa.ps1에 format/analyzer gate 추가, 개발·품질 명령 동기화 |
| 2026-10-05 | 검증 | 수정 후 macOS SDK10.0.401 format report=[], Release 경고/오류0, Core71/71(실패0/skip0), 문서/자산/공백 및 Python 하네스 구문 검사 통과 |
| 2026-10-05 | 검증 | 312916f [Windows37294330257](https://github.com/taejun9/VoxPet/actions/runs/37294330257) 통과: format/build/publish, Core71/71 TRX, WPF smoke failures0/failedChecks=[], 4창크기×15컨트롤/3startup bounds/작은창 wheel 검사 통과. 실제 WPF 생성 PNG4크기 직접 확인 |
| 2026-10-05 | 검증 | 312916f [OBS37294330480](https://github.com/taejun9/VoxPet/actions/runs/37294330480) 통과: OBS32.2.2/WindowsServer2025 build26100 WGC green+Chroma Key(남은green0%/alpha66.04%) 및 native alpha65.08%/각7프레임 변화. native 캡처 PNG3장 직접 확인. BitBlt 유효 프레임 없음은 기존 제한으로 유지 |
| 2026-10-05 | 심사 | QA 이후 단일 에이전트 자체 리뷰: 소스 변경은 공백만, 오디오 수명/렌더링/설정 동작 변경 없음. format 실패 시 qa.ps1이 빌드 전에 중단하며 검사 완화/의존성 갱신 없음. README 보존 및 개인정보 계약 유지 |
| 2026-10-05 | 정리 | 증거는 root artifacts/qa/plan007에 TRX/format reports/Windows·OBS logs/화면 PNG/summary.json 보존. completed 계획과 review 미러 작성 |

## Completion Notes

C# 서식179건을 수정하고 전체 format 검사를 반복 가능한 필수 QA에 추가했다.
macOS/Windows Core71/71, 빌드 경고/오류0, format 및 문서/자산/공백 검사와 Windows 실제 WPF smoke/layout·OBS WGC 화면 검증을 통과했다.
자동 QA와 실제 앱 화면 증거 검토를 완료했다. 완료 커밋 뒤 main ff-only 병합/push, branch -d/worktree 제거를 수행한다.
실제 마이크/권한/USB제거/실제 DPI 변경/실제 입력60분은 이 작업에서 수행하지 않았다. BitBlt는 CI에서 유효 프레임이 없어 지원 경로 WGC와 구분한다.
GitHub Actions의 기존 Node20/Node API deprecation 알림은 앱 빌드·린트 오류와 구분하며 action runtime 업그레이드는 별도 작업으로 남긴다.
