# plan-038-migration-review

## Status

active

## Owner

project_lead / plan_keeper

## User Request

첨부 자료의 Phase 1~10을 완료한 뒤 전체 repository migration review와 Runtime/패키지 문서 확정, 전체 QA를 수행한다.

## Goal

Legacy 각 클래스의 현재 참조·대체·호환 목적·삭제 가능성을 실제 코드로 보고한다. 기존 character/settings/migration/backward compatibility/QA oracle에 필요한 코드는 삭제하지 않는다. Input → TriggerEngine → ActionEngine → MotionEngine → CharacterRuntime → RenderFrame → Renderer를 문서화하고 .voxpet 규격을 확정한다. 첨부 명세의 모든 단계와 산출물을 완료 근거에 대응시켜 전체 목표 완료 여부를 감사한다.

## Non-Goals

신규 기능/그림·음성 저장·전송·웹캠·추적, 검증 없는 Legacy 삭제, 실제 마이크/사용자 GPU 실기 합격 주장을 추가하지 않는다.

## Context Map

- [마이그레이션 계약](../../product/voxpet-2-migration.md)
- [Runtime](../../architecture/voxpet-2-runtime.md), [패키지 규격](../../product/voxpet-package-format.md)
- [Phase 10 완료](../completed/plan-037-character-editor.md), [리뷰](../../reviews/plan-037-character-editor-review.md)
- src/VoxPet.Core/{Models,Services,Legacy,Character,Motion,Actions,Triggers,Attachments,Rendering,Packages}
- src/VoxPet.App/{Services,ViewModels,Views}, tests/VoxPet.Core.Tests

## Constraints

codex/plan-038-migration-review / .worktree/plan-038-migration-review. 단일 에이전트 QA 후 자체 리뷰. 기존 파일/설정/개인 PNG와 다른 worktree를 보존한다. 새 문서만으로 완료를 추정하지 않고 코드·테스트·실제 Windows/OBS evidence를 읽는다. main 직접 구현 커밋과 force 삭제/push를 하지 않는다.

## Implementation Plan

- [x] 첨부 명세의 전체 요구·10단계·최종 산출물과 완료 계획/테스트/코드를 대조한다.
- [x] Legacy 모델·서비스·App 경계의 실제 참조와 대체/호환 목적/삭제 가능성을 기록한다.
- [x] Runtime pipeline·thread/IO/lifetime·editor/broadcast 관계와 .voxpet format 문서를 확정한다.
- [ ] 전체 local locked restore/format/build/Core/harness/diff 및 Windows 전체 QA를 수행한다. source가 동일한 마지막 OBS 결과의 commit/JSON도 확인한다.
- [ ] QA 후 자체 리뷰, 완료 계획 이동/리뷰 미러와 main 병합/push·branch -d·worktree 정리를 수행한다.

## QA Plan

SDK10.0.401 locked restore, format verify, Release solution build, Core tests, verify_base/verify_app/diff check. 최종 branch의 Windows CI는 signed/unsigned startup·WPF·실제 desktop coverage까지 실행한다. Phase 10 d9c55de의 OBS38668937607 및 본 계획의 src/harness OBS source identity를 비교하고 WGC/chroma/native alpha/같은 HWND 결과 JSON을 직접 확인한다. 변경이 있으면 OBS를 다시 실행한다. Core의 WPF/NAudio/Win32 및 tick IO/JSON·wall-clock·audio callback UI 경계를 코드로 확인한다.

## Review Plan

QA 후 자체 리뷰로 첨부 명세의 named type/기능/보존 항목과 각각의 authoritative evidence를 점검한다. 불확실한 항목을 완료로 표시하지 않고 보완한다. 삭제 가능성은 production 참조와 test oracle 참조를 분리해 판단한다. 실기 제한을 자동 시험과 구분한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-15 | Phase 10의 main b883426 push·정리 이후 최종 리뷰를 별도 계획으로 수행한다. | 첨부 자료의 순서와 독립된 QA/완료 절차를 따른다. |
| 2026-10-15 | 삭제를 전제로 하지 않고 전체 Legacy 참조와 보존 이유를 먼저 확인한다. | 기존 파일·설정·호환성 oracle을 유지한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-15 | 설계 | 전용 worktree/브랜치와 최종 완료 감사 계획 작성. Phase 10 Core355·WPF1744/0·desktop250·Windows/OBS 통과를 기준으로 실제 근거를 재검토한다. |

| 2026-10-15 | 심사 | 기존67개 public 타입과 QA 전용 renderer, 새 Legacy bridge의 참조/대체/보존 이유를 정리했다. ImageSetPlayer는 production에서 대체됐으나 테스트 oracle이므로 보존한다. 첨부의 전체10단계·10화면·Master invariant를 코드/test/완료 계획에 매핑했다. Runtime/편집·방송 pipeline 및 pose ID/name의 format 의미를 문서에 명확히 했다. |
| 2026-10-15 | 검증 | locked restore/format/analyzer/Release build 경고0·오류0, Core355/355 및 verify_base/verify_app/diff check 통과. src/harness/.github는 Phase 10 main b883426과 동일하며 OBS38668937607의 d9c55de source identity와 JSON WGC/alpha 결과를 재확인한다. 최종 Windows branch QA를 이어서 실행한다. |

## Completion Notes

진행 중. 전체 요구사항의 완료 근거와 Git 수명이 확정되기 전에는 전체 목표를 완료로 표시하지 않는다.
