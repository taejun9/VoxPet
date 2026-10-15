# plan-037-character-editor

## Status

active

## Owner

project_lead / plan_keeper

## User Request

첨부 와이어프레임과 단계별 텍스트의 모든 작업을 이어서 완성한다. Phase 10에서 실제 Dashboard/library/Character Editor workflow를 구현한다.

## Goal

와이어프레임10화면의 기능을 WPF 화면으로 제공한다. 생성 → Canvas → 파츠 PNG → 시각 Anchor → 얼굴/입/표정 → 동작/timeline/trigger → 부착물 → 동일 Runtime preview → 검증 → .voxpet export가 작동한다. 기존 패키지 import/edit/export의 모든 지원 필드와 원본 PNG bytes를 보존한다.

## Non-Goals

별도 preview animation engine, JSON 텍스트만 편집하는 대체 UI, 웹사이트/새 그림 생성, 음성 저장·전송·추적, Legacy 파일/설정 삭제를 추가하지 않는다. 최종 전체 migration audit는 이 계획 완료 후 별도 계획으로 수행한다.

## Context Map

- [전체10화면 계약](../../product/voxpet-2-migration.md)
- [Runtime/App 구조](../../architecture/voxpet-2-runtime.md)
- [패키지 codec/규격](../../product/voxpet-package-format.md)
- src/VoxPet.App/ViewModels/CharacterRuntimeViewModel.cs, NativeCharacterViewModel.cs
- src/VoxPet.App/Services/CharacterPackageLibrary.cs, src/VoxPet.App/Views/RenderFrameDrawing.cs
- src/VoxPet.Core/Character, Actions, Triggers, Attachments, Packages

## Constraints

codex/plan-037-character-editor / .worktree/plan-037-character-editor. 계획 우선, 단일 에이전트 QA 이후 자체 리뷰. 원본/기존 설정/원본 개인 PNG를 보존한다. 공개 CI는 public/synthetic fixture만 쓴다. 기존 microphone/표정·의상·손동작 설정·global keys/방송·OBS 회귀를 유지한다. 이미지/JSON/ZIP 작업은 worker, tick에는 메모리 Core Runtime만 사용한다.

## Implementation Plan

- [x] 불변 지원 모델을 보존하는 Editor document/draft, 변경 section만 교체하는 apply/validation과 asset 작업 수명을 구현한다.
- [x] 기본 정보/버전/제작자/설명/thumbnail·Canvas 편집과 새 캐릭터 생성/패키지 import를 제공한다.
- [x] 파츠 추가/표시/ID/type/Anchor/Z-index/transform/PNG 교체·삭제를 제공한다.
- [x] 시각 Anchor point/라벨/부모·local 좌표/drag 배치와 계층 오류 안내를 제공한다.
- [x] mouth1~16·curve/frame offsets, eyes/brows/표정·blink 및 full-sprite bank 편집/보존을 제공한다.
- [x] action ID/name/priority/duration/playback·trigger와8종 timeline event 편집·scrub/play preview를 제공한다. Sound는 예약으로 표시하며 재생하지 않는다.
- [x] attachment14분류/충돌/layers/anchor/placement·inherit/Follows·pose/dependencies와 실제 착용 preview를 제공한다.
- [x] 같은 CharacterRuntime/RenderFrameDrawing을 쓰는 voice/expression/action/attachment preview·검증·atomic export를 구현한다.
- [x] Dashboard/nav·마이크 상태/빠른 동작·방송 출력과 library 검색/cards/select/edit/export/delete를 연결한다.
- [x] import→edit→export→reload의 모든 지원 모델·metadata/PNG와 실제 runtime frame을 비교한다.
- [ ] local Core/build/format/harness, Windows editor workflow/10화면·DIP/layout/screen evidence 및 기존전체 WPF/OBS 회귀를 수행한다.
- [ ] QA → 자체 리뷰 → 완료 이동/미러 → main 병합/push → branch -d → worktree 정리를 마친다.

## QA Plan

SDK10.0.401 locked restore/format verify/Release solution build/Core tests, verify_base/verify_app/diff check. Windows task branch CI로 실제 editor 새 생성·import·각 section 변경·parent drag·mouth16·timeline/attachment preview·validate/export/reimport 및 종료/잘못된 PNG·worker 실패 복구를 검사한다. 기본 UI를 Dashboard로 제공하고 기존 설정 화면 QA는 명시 navigation helper로 열어 보존한다. public/synthetic10화면 screenshot을 내려받아 시각 검토한다. 기존 두 창 공유·hotkey/음소거·설정/표정/wardrobe/손동작·OBS/WGC/alpha는 계속 필수다.

## Review Plan

완료를 가정하지 않고10화면 및 모든 workflow/model field를 실제 코드·export bytes/frame·Windows 화면/수치에 대응시켜 검토한다. 정상 사용자 앱 경로에서 조작 가능해야 한다. 초안 오류는 원본과 마지막 유효 preview를 보존하고 export를 막는다. 상세 UI/model 보존·수명·접근성·privacy·기존 regression을 별도로 검토한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-15 | Phase 9의 검증된 Runtime/worker library/Frame renderer를 재사용한다. | preview와 방송을 같은 engine으로 만들고 IO/렌더 선택의 중복을 피한다. |
| 2026-10-15 | 기존 v1 설정 화면은 navigation으로 보존하면서 실제 기본 화면은 새 Dashboard로 제공한다. | wireframe의 최종 UI를 구현하며 기존 saved settings와 QA를 유지한다. |
| 2026-10-15 | 편집은 section transaction과 명시 Apply를 사용하고 미적용 입력은 선택별로 보존한다. 작은 창은 유한 viewport와 스크롤을 사용한다. | 지원 모델/원본 PNG를 잃지 않고 모든 조작에 접근하도록 한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-15 | 설계 | Phase 9 main ee8f45c push·branch/worktree 정리 후 전용 계획 작성. Core351/Windows1535/OBS 회귀를 기반으로 한다. |

| 2026-10-15 | 제작 | CharacterDocument/section transaction, field inspector, mouth/motion/expression/action/trigger/attachment 설정과 같은 Runtime preview/scrub, 시각 Anchor drag를 구현 중이다. StudioShell 기본 Dashboard/library/editor와 기존 설정 navigation을 연결했다. |
| 2026-10-15 | 설계 | 파츠 visible field를 추가해 목록의 표시/숨김을 package/Runtime에 보존한다. 기존 파일은 기본 true이며 action visibility는 명시 override다. PNG 준비는 pending form/detail 선택을 보존하고 사용하지 않는 원본 PNG도 유지한다. |

| 2026-10-15 | 검증 | 초기 구현 Release build 경고0·오류0, Core355/355(기존351+편집 보존4), locked restore/format/문서·자산·diff 검사 통과. Windows 새 화면/사용자 workflow QA는 아직 미수행이다. |
| 2026-10-15 | 설계 | 후속 보완: full-sprite의 모든 표정 bank 선택/교체, 포즈·부착물 pose map 입력, 작은 화면 배치, pending 입력과 상세 선택 수명, 실제10화면·create/edit/export/reimport 및 기존전체 Windows QA를 요구한다. 이 항목들이 검증되기 전에는 Phase 10 완료로 판정하지 않는다. |

| 2026-10-15 | 검증 | 첫 Windows38611644232는 Core355 통과 후 normal startup fresh12 실패, OBS38611644208도 창 초기화 실패. 초기 read-only 음량 ProgressBar binding을 OneWay로 명시하고 sanitized startup/OBS error JSON artifact 수집을 보완한다. |

| 2026-10-15 | 검증 | startup/OBS 초기화 수정 후 OBS38614568856 통과. Windows38614568737는 일반 시작을 통과했지만 editor 첫 desktop capture 경계 검사에서 중단했다. 새 fixture를 작업 영역 내부로 배치하고 non-square Canvas 비율·시트 bank 깜빡임/표정 추가·삭제·ID 변경·미적용 구조 편집 보호·전체 캐릭터 thumbnail을 보완한다. |

| 2026-10-15 | 심사 | 후속 자체 코드 검토에서 작은 화면의 Home/library 스크롤, 편집 worker 종료 대기, busy 중 입력 보호, bank 선택의 실제 표정 preview 및 편집 후 착용 유지, 시간 재생·scrub,8종 timeline payload/PNG 입력을 보완했다. 사용자 안내와 native worker 경유 preview/broadcast frame 비교도 추가한다. |

| 2026-10-15 | 검증 | Windows38616497317은 editor 타임라인 좌표1항목 외 모든 검사/10화면을 통과했다. 부모 idle X를 포함한 기대값으로 수정하며 action local X=30도 별도 확인한다. 실제 화면에서 light 화면의 inherited 흰 글씨와 무한 목록 측정으로 Canvas가 아래로 밀리는 문제를 확인해 local 스타일/유한 높이/목록·preview controls 스크롤/780DIP 배치를 수정했다. |

| 2026-10-15 | 검증 | Windows38618365658의4크기×8페이지 Canvas/접근성/입력 대비와 preview/broadcast frame 비교가 통과했다.8종 이벤트 검사에서 값이 같은 Transform 이벤트의 IndexOf가 첫 행을 바꾸는 오류6건을 찾아 reference index로 수정한다. 화면의 navigation/checkbox/disabled button에는 ContentTemplate의 명시 색상을 적용한다. OBS38618365620 통과. |

| 2026-10-15 | 심사 | Home/library도 유한 viewport 크기로 측정하고 Home 오른쪽 입력/빠른 동작 영역을 스크롤한다. 동일 좌표 Anchor의 라벨은 겹치지 않게 배치하며 화면 QA의 head Z fixture를 얼굴보다 아래에 두어 표정이 가려지지 않게 한다. |

## Requirement Evidence

| 요구 | 구현 경로 | 검증 근거 |
|---|---|---|
| metadata/Canvas/thumbnail/parts/visible | CharacterDocument, Editor Page0/1, PNG worker transaction | CharacterDocumentTests4, editor_metadata_apply/part_visibility_runtime/png_prepare_keeps_pending_form/bad_png_preserves_document |
| 부모 Anchor/시각 drag | EditorCanvas, MoveAnchorAsync, RigMotionSolver inverse | editor_anchor_parent_local_transaction/real_mouse_anchor_drag/parent_cycle_preserves_valid_preview |
| mouth/face/expression/full-sprite | Editor Page3/4, SpriteBank, MotionFields | editor_mouth_16_frames/mouth_slider_same_runtime/full_sprite_* |
| action/playback/trigger/pose/8종 event | Editor Page5/details/Seek, Core Actions/Triggers | editor_action_timeline_scrub_frame/pose_override_edit/timeline_payload_*/all_event_types_reload |
|14분류 attachment/레이어/상속/pose map/착용 | Editor Page6/details, AttachmentSystem | editor_attachment_same_runtime_preview + 기존 AttachmentSystem/Legacy adapter Core 회귀 |
| 동일 Runtime/export 왕복 | CharacterRuntimeViewModel, common RenderFrameDrawing, exporter | editor_export_reload_all_supported_fields_exact/original_png_bytes/runtime_frame_exact/preview_broadcast_frame_exact |
| Dashboard/library/navigation/기존 설정 | StudioShell, NativeCharacterViewModel, CharacterPackageLibrary | editor-home/library10화면, 기존 Native/Startup/Settings/Input/OBS 회귀 및 명령·관리 경로 자체 검토 |
| 작은 창·가독성 | 유한 layout/목록·inspector/preview scroll, local templates | editor_bounded_canvas/input_reachable/input_contrast/navigation_contrast,4크기×8페이지 |

최종 이벤트 identity 보완 코드의 전체 Windows/OBS 결과와 실제10화면 시각 검토는 아래 완료 기록에 확정한 뒤 QA 항목을 체크한다.

| 2026-10-15 | 심사 | 여러 timeline 행의 미적용 입력과 시간 재정렬을 검토해 순번 key 대신 immutable event 객체별 weak identity를 사용한다. trigger/pose row key도 분리한다. Attachment700ms 미적용 → Pose900ms 적용/재정렬 → Attachment 적용이 정확한 객체에 반영되는 회귀를 추가한다. 최종 QA는 이 수정까지 포함한다. |

| 2026-10-15 | 검증 | 화면 코드5acdf90 Windows38666460058/OBS38666460020 통과. WPF1740고유검사·실패0, 실제 desktop250장.10화면 시각 검토에서 card DataTemplate 텍스트는 implicit style 경계의 영향을 받아 명시 foreground로 수정한다. QA head Z는 원래 Z+1/+2로 바꿔 눈·입이 가려지지 않게 한다. 마지막 event identity 검사와 함께 재검증한다. |

| 2026-10-15 | 심사 | 최종 코드600d541 Windows38667692310/OBS38667692366 통과: Core355, WPF1743/0, desktop250, unsigned/signed9사례씩 정상 종료·파일 보존.10화면을 직접 검토한 후 기본 정보의 빈 목록과 미리보기의 불필요 inspector를 숨기고 상세 편집은 동작/부착물 페이지에서만 제공하도록 마지막 화면 정리를 한다. |

## Completion Notes

진행 중. 이 계획과 최종 migration audit가 모두 검증되기 전에는 전체 목표를 완료로 표시하지 않는다.
