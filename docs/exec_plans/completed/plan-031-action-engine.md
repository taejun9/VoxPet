# plan-031-action-engine

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

첨부 자료의 모든 마이그레이션을 순서대로 진행한다. 이번 계획은 Phase 4 Generic ActionEngine과 손동작 scheduler 호환이다.

## Goal

플랫폼 독립 timeline 이벤트와 우선순위/중단/once·loop/idle 복귀·hold를 재현 가능한 시간으로 실행한다. 기존 HandMotionScheduler API/설정/간격·입력 동작은 compatibility adapter 뒤에 유지한다.

## Non-Goals

Sound 재생, 입력 감지 서비스 이동, UI/오디오 변경과 Legacy 삭제는 하지 않는다.

## Context Map

- [개발 계약](../../product/voxpet-2-migration.md)
- `src/VoxPet.Core/Actions/ActionDefinition.cs`, `ActionTimelineEvent.cs`, `Character/PoseDefinition.cs`
- `Services/HandMotionScheduler.cs`, `tests/VoxPet.Core.Tests/HandMotionTests.cs`

## Constraints

Caller monotonic time만 사용. tick에는 파일/JSON/Win32/WPF/오디오 재생 없음. 동작ID는 문자열. 기존 설정 형식 보존.
`codex/plan-031-action-engine` / `.worktree/plan-031-action-engine`. QA → 자체 리뷰 → 완료 이동/미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] ActionEngine/ActionTimeline/ActionState와 우선순위 상수 구현.
- [x] pose/transform/sprite/visibility/effect/expression/attachment 이벤트 실행, Sound 예약 무동작.
- [x] priority arbitration/interrupt/loop/idle·hold/지연 tick/큰 시간/불변 출력 시험.
- [x] LegacyHandMotionSchedulerAdapter 구현 후 기존 Scheduler를 wrapper로 변경하고 모든 기존 회귀 확인.
- [x] QA 후 리뷰와 Git 수명 완료.

## QA Plan

SDK10.0.401/macOS arm64. locked restore, formatter verify, 솔루션 Release build(경고/오류0), 전체 Core tests, verify_base/verify_app, stage diff check. Windows/OBS CI는 push 이후 특정 run으로 추적한다.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰. 높은 우선순위 차단·비중단 재생, 타임라인 순서/hold/효과 만료/지연 tick·loop wrap, 전·후 scheduler 간격 및 연타·종료·설정 보존을 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-14 | 프레임 상태를 timeline 처음부터 현재 시점까지 bounded 재평가한다. | 지연 tick에서 이벤트를 빠뜨리지 않고 지난 loop를 폭주 재생하지 않으며 같은 시각은 같은 결과를 만든다. |
| 2026-10-14 | 비중단 action은 System100의 명시 요청만 강제 중단할 수 있다. 같은 우선순위의 사용자 입력은 interruptible일 때 우선한다. | idle/voice가 진행 중 무기 팔을 덮는 충돌을 차단하며 시스템 복구를 허용한다. |
| 2026-10-14 | 기존 scheduler의 간격·공정 선택·설정 bookkeeping은 adapter가 유지하고 재생 수명은 ActionEngine으로 옮긴다. | 기존 API와 동작 규칙을 보존하면서 실제 재생 엔진을 통합한다. |
| 2026-10-14 | 동작은1ms 이상이며 종료 시각의 pose 이벤트를 허용한다. | seconds 변환의 underflow를 차단하고 제공 예시의 마지막 idle 이벤트를 표현한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-14 | 설계 | Phase 3 검증·push·정리 후 Phase 4 전용 worktree/계획 작성. |

| 2026-10-14 | 검증 | Core287/287, locked restore/format/build/하네스 통과. 기존 손동작 회귀 전부 통과. |
| 2026-10-14 | 심사 | QA 후 자체 리뷰. 우선순위·중단·hold/idle·loop/지연·효과 만료와 호환 scheduler 검토. |

## Completion Notes

Phase 4 완료. Generic ActionEngine/Timeline/State/priority와 LegacyHandMotionSchedulerAdapter를 구현했다. Core287/287(기존277+신규10), locked restore/format verify/Release build(경고·오류0)/문서·자산 QA 통과. 초기 ActionState 사전 생성의 타입 추론 오류를 명시적 Dictionary 생성으로 수정 후 재검증했다. QA 이후 자체 리뷰 미해결 결함 없음. 기존 HandMotionScheduler API/설정/interval·always·hotkey 규칙 보존. Sound는 예약 무동작. 완료 기록 검사 후 main 통합·정리를 수행한다. 다음 Phase 5 TriggerEngine으로 진행한다.
