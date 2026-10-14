# plan-030-motion-engine

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

모든2.0 단계를 연속 진행한다. 이번 계획은 Phase 3 MotionEngine이며 기존 CharacterAnimator를 호환 wrapper로 유지한다.

## Goal

Voice/Idle/Blink를 composable source로 분리하고 CharacterPose로 합성한다. 호출자가 제공한 시간과 seeded Random으로 재현하며 기존 입/몸/눈 반응과 큰 유한 시간 동작을 보존한다.

## Non-Goals

오디오 처리, App/UI 개편, 동작 우선순위 엔진, Legacy 제거는 하지 않는다.

## Context Map

- [개발 계약](../../product/voxpet-2-migration.md), [앱 설계](../../architecture/application.md)
- `src/VoxPet.Core/Services/CharacterAnimator.cs`, `Character/CharacterPose.cs`, `tests/VoxPet.Core.Tests/CharacterTests.cs`

## Constraints

WPF/NAudio/파일/JSON/wall-clock 독립. 캐릭터 선택·오디오·설정 변경 없음.
`codex/plan-030-motion-engine` / `.worktree/plan-030-motion-engine`. QA → 자체 리뷰 → 완료 이동/미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] MotionEngine/IMotionSource/VoiceMotion/IdleMotion/BlinkMotion 및 유한 설정 계약 구현.
- [x] CharacterAnimator를 새 엔진의 호환 wrapper로 변경.
- [x] 기본/선택형 head motion, seeded blink, source 합성, 거대한 시간·비유한 입력 및 기존 경계 회귀 시험.
- [x] QA 후 리뷰와 완료 수명 절차.

## QA Plan

macOS arm64 / SDK10.0.401. locked restore, format verify, 전체 솔루션 Release build, 전체 Core tests, verify_base/verify_app, stage diff check.
Core 기존 시험 전부 통과. WPF 실행·마이크/OBS 실기는 macOS에서 미실행.

## Review Plan

QA 후 단일 에이전트 자체 리뷰. 원래3단계 threshold, idle sin의 주기 감소, blink 간격·duration 및 Random 소비 순서, 파일/음성 경계와 호환 wrapper 출력을 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-14 | 호환 wrapper는 기존 Voice5/Idle1/Head0/Blink2~6초·140ms를 명시 설정으로 사용한다. | 신규 기본 옵션과 Legacy 출력 계약을 분리하면서 동일 엔진을 사용한다. |
| 2026-10-14 | 모션 Y는 렌더 좌표의 음수 bounce이고 rotation은 degree다. | 기존 App의 Bounce=-BodyBounce와 일치한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-14 | 설계 | Phase 2 push·정리 완료 후 전용 worktree와 계획 작성. |

| 2026-10-14 | 검증 | Core277/277, locked restore/format/build/하네스 통과. |
| 2026-10-14 | 심사 | QA 후 자체 리뷰. 기존3단계/6DIP/seeded blink 계약과 source 합성·유한값 경계 확인. |

## Completion Notes

Phase 3 완료. MotionEngine과4종 source/input 계약을 추가하고 CharacterAnimator를 호환 wrapper로 변경했다. Core277/277(기존267+신규10), locked restore/format verify/Release build(경고·오류0)/문서·자산 검사 통과. 기존 공식 수식과 Random 소비 순서의60초 비교, 큰 유한 시간, 지연 blink/중단 interval와 모션 합성을 확인했다. 오디오/App/사용자 설정은 변경하지 않았다. QA 이후 자체 리뷰 미해결 결함 없음. Phase 4 액션 엔진으로 이어간다. 완료 기록 검사 후 main 통합·push·정리를 수행한다.
