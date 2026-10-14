# plan-029-legacy-adapter

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

첨부 자료 순서대로 Phase 2부터 에디터와 최종 리뷰까지 모두 이어서 완성한다. 이번 계획은 Phase 2 호환 Adapter다.

## Goal

기존 ImageSet을 원본 모델 참조 없이 불변 CharacterDefinition으로 변환하고 제공 제작 틀의 파츠/입/표정/Anchor/의상/무기/프레임 시간을 보존한다.

## Non-Goals

UI 연결, Legacy 제거, 모션/액션 엔진 실행, ZIP 로딩은 후속 단계에서 한다.

## Context Map

- [개발 계약](../../product/voxpet-2-migration.md), [이미지 세트 v1](../../product/image-set-format.md)
- `src/VoxPet.Core/Models/ImageSet.cs`, `Services/ImageSetPlayer.cs`, `Character/CharacterDefinition.cs`
- `src/VoxPet.App/Assets/ImageSetTemplate/imageset.json`, `tests/VoxPet.Core.Tests/ImageSetTests.cs`

## Constraints

Core WPF/NAudio 독립. 기존 ImageSet/ImageSetPlayer·사용자 설정 보존. 문자열ID/유한 수치/불변 snapshot.
`codex/plan-029-legacy-adapter` / `.worktree/plan-029-legacy-adapter`. QA → 자체 리뷰 → 완료 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] Adapter에 필요한 mouth/expression/pose/action/attachment 정의를 추가하고 CharacterDefinition에 연결한다.
- [x] LegacyImageSetAdapter와 ICharacterDefinitionSource/LegacyImageSetSource를 구현한다.
- [x] 제작 틀의 모든 자산·프레임·선택·숨김·Anchor 이동 및 deep-copy를 시험한다.
- [x] 기존 회귀/QA 이후 자체 리뷰와 Git 수명을 완료한다.

## QA Plan

macOS arm64 / `/private/tmp/voxpet-sdk019` SDK10.0.401, CLI/NuGet cache는 tmp 사용.
locked restore, dotnet format verify, 솔루션 Release build(경고/오류0), 전체 Core tests.
`python3 harness/scripts/verify_base.py`, `python3 harness/scripts/verify_app.py`, stage 후 `git diff --cached --check`.
Windows 실행은 macOS에서 불가능하며 변경 없는 App은 교차 빌드와 기존 테스트로 확인한다.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰. Legacy 참조를 신규 정의 안에 보관하지 않는지, 모든 Files와 프레임 timing/교체·숨김/소매·무기 pose·Anchor·입 threshold가 표현되는지 검토한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-14 | Adapter가 필요로 하는 action/attachment 데이터 정의는 이번 단계에 추가하고 실행 엔진은 원래 단계에서 구현한다. | Phase 2의 무손실 표현 완료 조건에 필요한 모델이며 Legacy 객체를 그대로 감싸는 우회를 피한다. |
| 2026-10-14 | Legacy 입3/8 threshold와 full-canvas PNG 좌표를 명시적 호환 정책으로 보존한다. | 신규 기본1~16프레임 정책으로 기존 출력 경계가 바뀌지 않게 한다. |
| 2026-10-14 | Legacy Eye/Face/Mouth는 head의 독립 자식이며 frame pose는 고유ID와 기존 pose 이름을 함께 갖는다. | 기존 Anchor.Head+각 offset 규칙과 같은 이름을 반복하는 서로 다른 프레임을 보존한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-14 | 조율 | main clean/origin 동기 확인 후 전용 worktree 생성. 모든 후속 단계 진행 승인 반영. |

| 2026-10-14 | 검증 | Core267/267, build0warning/0error, locked restore/format/하네스 통과. 타입 모호성·서식 위반을 해결한 뒤 재검증했다. |
| 2026-10-14 | 심사 | QA 후 자체 리뷰: assets/timing/숨김/Anchor/깊은 복사·원본 모델 보존 확인. 미해결 결함 없음. |

## Completion Notes

Phase 2 완료. 제공 제작 틀의 전체 Assets,6파츠/표정/입 threshold·offset/의상/무기 pose/프레임 시작과 총 시간을 보존했다. 신규 정의는 원본 mutable 객체를 보관하지 않는다. Core267/267(기존261+신규6), locked restore, format/analyzer, Release build(경고·오류0), 문서/자산 QA 통과. QA 후 자체 리뷰. Windows 실행은 미실행이며 UI/Legacy/오디오 호출 경로는 유지한다. 후속 Phase 3은 MotionEngine이다. 완료 기록 검증 후 main 통합과 정리를 순서대로 수행한다.
