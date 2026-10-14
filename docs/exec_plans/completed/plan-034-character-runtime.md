# plan-034-character-runtime

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

모든2.0 단계를 순서대로 완성한다. 이번 계획은 Phase 7 중앙 CharacterRuntime이다.

## Goal

RuntimeInput/InputSnapshot → TriggerEngine → ActionEngine/MotionEngine → Rig/Attachment/RenderComposer → RenderFrame을 연결한다. 렌더러가 입/표정/동작/부착 상속·우선순위를 결정하지 않도록 Core가 최종 출력값을 만든다.

## Non-Goals

UI/오디오 개편, ZIP 파일 처리, Legacy 삭제는 하지 않는다. 기존 UI는 이후 App 단계에서 연결한다.

## Context Map

- [개발 계약](../../product/voxpet-2-migration.md)
- `src/VoxPet.Core/Character`, `Motion`, `Actions`, `Triggers`, `Attachments`, `Rendering`
- `Legacy/LegacyImageSetAdapter.cs`, `Services/ImageSetPlayer.cs`, `tests/VoxPet.Core.Tests/ImageSetTests.cs`

## Constraints

Core WPF/NAudio/Win32 독립. caller monotonic time/delta/유한 VoiceLevel. tick에는 파일/JSON 없음. immutable definitions/출력 snapshot. 기존 App/방송창/설정 유지.
`codex/plan-034-character-runtime` / `.worktree/plan-034-character-runtime`. QA → 자체 리뷰 → 완료 이동/미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] MotionProfile/RuntimeInput/InputSnapshot/MouthController/CharacterRuntime/RenderComposer 구현.
- [x] RenderFrame/Layer에 정확한 affine·logical 크기·viewport 이동 metadata를 추가하고 기존 생성 API 유지.
- [x] 자동 요청 예약/명시 입력 우선, expression/attachment 명령, action target별 motion 우선순위 결합.
- [x] idle→voice→action→idle,1~16입 frame,층별 priority/상속/효과/결정성 integration 시험.
- [x] v1 제작 틀의 frozen layer/asset/order/offset/timing·숨김·입 장식과 신규 Runtime 결과 비교.
- [x] QA 이후 리뷰와 완료 Git 수명.

## QA Plan

SDK10.0.401/macOS arm64: locked restore, format verify, 솔루션 Release build, 전체 Core tests, verify_base/verify_app, stage diff check. 기존 UI 실행 경로는 아직 유지하며 Windows/OBS workflow로 회귀를 추적한다.

## Review Plan

QA 후 단일 에이전트 자체 리뷰. renderer의 판단 없는 출력, lower-priority motion의 action target 덮어쓰기 차단, 자동 요청 coalesce/공정 예약, package 형식 독립성, v1 composition 동등성과 수치/데이터 경계를 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-14 | v1 호환 body bounce는 viewport 이동 metadata로 따로 보존한다. | 기존 CharacterView는 Viewbox 바깥에서6DIP 이동하므로 logical Canvas 배율에 중복 적용하지 않는다. |
| 2026-10-14 | 자동 요청은 action별 하나를 FIFO 예약하고 명시 입력은 즉시 arbitration한다. | 여러 interval/always가 한 tick에 몰려 마지막 action만 반복되는 회귀를 피한다. |
| 2026-10-14 | affine matrix와 이미지 logical width/height를 RenderLayer에 전달한다. | 비균일 부모 scale/rotation과 legacy 의상 크기를 renderer 추론 없이 그대로 표시한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-14 | 조율 | Phase 6 QA309/309·리뷰·main push·정리 완료 후 계획과 전용 worktree 작성. |

| 2026-10-14 | 제작 | CharacterRuntime/RenderComposer/MouthController와 출력 affine/크기/viewport·mesh 계약 구현. |
| 2026-10-14 | 검증 | Core317/317(기존309+신규8), locked restore/format verify/Release build 경고0·오류0, 하네스 통과. |
| 2026-10-14 | 심사 | QA 후 자체 리뷰. renderer 판단 없는 출력·priority·자동 큐 bounded·v1 frozen composition 동등성 확인. |

## Completion Notes

Phase 7 완료. Core317/317, locked restore/format verify/Release build 경고0·오류0, 문서/자산 검사와 자체 리뷰 통과. idle→voice→action→idle,1~16 mouth 및 반올림, action target priority, expression/attachment/effect/크기·지연·결정성을 시험했다. 기존 ImageSetPlayer와 제공 제작 틀의 프레임별 자산/순서/좌표와 head/arm 숨김·소매/무기를 비교했다.

Runtime은 file format을 모르며 Trigger/Action/Motion/Rig/Attachment를 조정한다. 자동 예약은 action별 한 node이며 명시 입력 선점 시 즉시 제거해 큐가 누적되지 않는다. RenderFrame은 affine matrix/logical size/opacity/z-order와 viewport 이동을 포함한다. 기존 Viewbox 바깥 bounce와 얼굴 z-order는 definition policy로 보존했다.

기존 App/오디오/설정은 아직 그대로이며 Phase 9에서 연결한다. 연결 격자가 필요한 garment는 pure geometry provider가 없으면 active 동작 출력을 거부한다. mesh 계약은 Core에 제공했고 실제 legacy mesh provider/pixel renderer는 App 통합 단계에서 구현한다. 이를 준비되지 않은 renderer가 임의로 추론하지 않는다.

완료 기록 검증 후 main 병합/push·정리를 수행한다. Phase 8 패키지 loader/validator/export codec으로 이어간다.
