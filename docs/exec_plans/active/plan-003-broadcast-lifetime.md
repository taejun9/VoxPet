# plan-003-broadcast-lifetime

## Status

active

## Owner

project_lead / quality_runner / review_judge

## User Request

실사용 가능한 전체 구현과 버그 검증 목표를 이어간다.

## Goal

설정창 최소화와 무관하게 방송창을 유지하고, 방송창 닫기/재열기 및 메인 종료 정리를 실제 Windows UI에서 검증한다.
전체 실사용 목표의 실제 마이크/OBS/장시간 조건은 유지한다.

## Non-Goals

실제 마이크/OBS 없이 해당 실기 완료 주장, 향후 STT/에디터/Live2D 기능, 사용자 음성 저장/전송.

## Context Map

MainWindow.ShowCharacter의 Owner 설정, ShutdownAsync의 명시적 방송창 종료, App.RunSmokeAsync, Windows CI.
[Microsoft Window.Owner](https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.owner?view=windowsdesktop-10.0) 공식 설명은 owner 최소화 시 owned 창도 최소화됨을 명시한다.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-003-broadcast-lifetime` / `.worktree/plan-003-broadcast-lifetime` 사용.
QA → 자체 리뷰 → 완료 이동/리뷰 미러 → main ff-only 병합/push → branch -d → worktree 제거.
README 보존. 오디오/개인정보 계약 유지. 미검증 실기 상태를 합격으로 바꾸지 않는다.

## Implementation Plan

- [x] Win32 가시성/최소화 확인으로 기존 방송창 문제 재현
- [x] 독립 방송창 및 명시적 종료/재열기 구현 검토와 수정
- [ ] Windows 회귀/기존 70개 테스트/배포 검증
- [ ] 자체 리뷰/문서 동기화/완료 기록과 Git 정리

## QA Plan

Windows UI smoke에서 실제 설정창을 최소화하고 native IsWindowVisible/IsIconic으로 방송창 지속을 확인한다.
두 창의 상태 snapshot 공유, 방송창 닫기/재열기, 메인 종료 후 방송창 닫힘을 검사한다.
기존 코드가 새 검사에서 실패하는 증거를 확보한 후 수정 코드에서 통과해야 한다.
`python3 harness/scripts/verify_base.py`, `python3 harness/scripts/verify_app.py`, `git diff --check`, SDK 10.0.401 Release build/Core test, Windows locked restore/build/test/publish/smoke.

## Review Plan

QA 후 단일 에이전트 자체 리뷰. Owner 제거 시 자동 close 의존이 없어도 ShutdownAsync가 자원/방송창을 정리하는지 확인한다.
실제 마이크/OBS 성공으로 확대 해석하지 않는다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-05 | 다음 MVP 버그 수정은 plan-003에 둠 | plan-002는 구현/자동 QA 범위 완료이며 실제 남은 문제를 새로운 계획으로 추적 |
| 2026-10-05 | 작업 브랜치 checkpoint push로 Windows 재현/수정 검증 | macOS 로컬에서 WPF 실행 불가, main 통합은 Windows QA/리뷰 이후 |

| 2026-10-05 | 이미 최소화된 방송창에서 열기 명령은 Normal 복구 후 Activate | 열기 명령으로 사용자가 기존 방송창을 다시 볼 수 있도록 함; 실제 native 가시성 검사 추가 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-05 | 지도 | 깨끗하고 origin/main과 같은 main 확인 후 지정 worktree 생성 |
| 2026-10-05 | 검증 | [수정 전 CI](https://github.com/taejun9/VoxPet/actions/runs/37266351482): 70개 테스트/build/publish 통과, native UI 검사 중 broadcast_survives_main_minimize만 실패 |
| 2026-10-05 | 제작 | Owner 관계 제거. 기존 ShutdownAsync 명시적 종료 및 상태 공유 유지 |

## Completion Notes

진행 중. 실제 Windows 마이크/OBS/장시간 환경에 대한 사용자 답변은 아직 없으며 전체 목표는 미완료다.
