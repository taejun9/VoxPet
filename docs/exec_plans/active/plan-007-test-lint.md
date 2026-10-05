# plan-007-test-lint

## Status

active

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

- [ ] 현재 검증 명령/실제 UI QA 경로와 환경 확인
- [ ] 로컬 전체 테스트·빌드·린트 실행 및 필요한 수정
- [ ] Windows 실제 앱 창 smoke/layout 및 OBS 화면 실행·증거 확인
- [ ] QA 이후 자체 리뷰·문서 완료 기록 및 Git 수명 완료

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

## Completion Notes

검증 진행 중. 실제 테스트 결과 및 남은 제한은 수행 후 기록한다.
