# VoxPet 2.0 단계별 완료 근거

첨부 텍스트의 Master Prompt·Phase 1~10·최종 migration review와10화면 와이어프레임을 기준으로 실제 코드, 완료 계획/리뷰, 전체 테스트, Windows/OBS 결과를 대조한다. [최종 계획](../exec_plans/completed/plan-038-migration-review.md)의 전체 QA와 QA 이후 자체 리뷰로 완료 근거를 확정했다. 문서 존재만으로 구현 완료를 추정하지 않는다.

## 단계별 모델·기능과 검증

각 계획은 대응하는 docs/reviews/plan-NNN-…-review.md를 가진다. 기반 모델/engine은 Core이며 App 연결과 시각 개편은9/10단계로 분리했다.

| 단계 / 완료 계획 | 명시 산출물·동작 | 코드와 검증 |
|---|---|---|
| [1 Foundation](../exec_plans/completed/plan-028-runtime-foundation.md) | CharacterDefinition/Part/State/Pose, AnchorDefinition/Transform2D, RenderFrame/Layer, VoxPetManifest | Core Character/Rendering/Packages. RuntimeFoundationTests: 각 수치의 비유한 값·유한 단위값·양수 Canvas,512기본/비정사각, 부모 누락·cycle·deep tree, string ID, frame 모든 필드/안정 z-order, collection snapshot, format/character version 분리, 선택 문서. ImagePart/ImageAnchor 보존. |
| [2 Legacy Adapter](../exec_plans/completed/plan-029-legacy-adapter.md) | LegacyImageSetAdapter, ICharacterDefinitionSource/LegacyImageSetSource, mouth/expression/pose/action/attachment 정의 | LegacyAdapterTests: 공개 제작 틀의 body/head/arm/leg, 모든 PNG·입/눈·Anchor/offset, outfit/weapon·숨김 의존·시간 경계와 mutable 원본 분리. ImageSet/ImageSetPlayer 보존. |
| [3 Motion](../exec_plans/completed/plan-030-motion-engine.md) | MotionEngine/IMotionSource/VoiceMotion/IdleMotion/BlinkMotion, CharacterAnimator wrapper | MotionEngineTests: 입/body/head/회전·breathing/sway·seeded blink·caller time/delta·지연 경계·deterministic full poses. CharacterTests의 기존3단계/6DIP/blink 계약 유지. 오디오 처리 변경 없음. |
| [4 Action](../exec_plans/completed/plan-031-action-engine.md) | ActionDefinition/Playback/TimelineEvent/Timeline/Engine,7종 실행 event와 Sound 예약, priority0/10/20/30/50/80/100 | ActionEngineTests:8종 payload/효과 만료, 우선순위·중단·System, once/loop/hold/idle·지연 tick·동일 replay·포즈 숨김·invalid time. LegacyHandMotionSchedulerAdapter와 기존 scheduler API/1.4초/rewind·stop 보존. |
| [5 Trigger](../exec_plans/completed/plan-032-trigger-engine.md) | TriggerDefinition/Event/Engine, hotkey/mouse/voiceThreshold/interval/manual, ActionBinding/Settings | TriggerEngineTests: modifiers/repeat/down-up·Windows pulse, rising edge/mute 재무장, interval coalesce, manual/mouse/disabled/dedup/replay, Legacy binding 보존. Win32는 App HandMotionInputs/HotkeyTriggerProvider/NativeCharacterInputs이며 기존 표정/음소거 키 유지. |
| [6 Attachment](../exec_plans/completed/plan-033-attachment-system.md) | AttachmentDefinition/Layer/Transform/Inheritance/System, RigDefinition/RigMotionSolver, LegacyCostumeAdapter/AttachmentStore | AttachmentSystemTests·AffineTransformTests·AttachmentCompatibilityTests:14표준/custom Anchor, 부모 exact affine, position/rotation/scale/mouth 독립·Follow, onePiece/top/bottom 충돌·복수 장식, **MoustacheFollowsMouthAndHeadWhileGlassesExcludeMouthMotion**, pose/의존·기존 wardrobe bytes/settings 복구. |
| [7 Runtime](../exec_plans/completed/plan-034-character-runtime.md) | CharacterRuntime/RuntimeInput/InputSnapshot/RenderComposer, final RenderFrame | CharacterRuntimeTests: idle→voice→action→idle,1~16입/curve/rounding, 자동 공정 대기열·manual priority, 표정/부착/effect/size, 숨김 의존, 원래 template composition 동등성, deterministic copied replay. renderer 판단을 Core로 이동. |
| [8 Package](../exec_plans/completed/plan-035-voxpet-package.md) | VoxPetPackageLoader/Validator/Exception/Exporter/Source, ZIP+JSON+PNG | VoxPetPackageTests: minimal package·전체 지원 모델/PNG/frame 왕복, absolute/drive/UNC/traversal/중복 NFC·case/symlink·확장자, archive/entry/JSON/asset/decoded 제한, bad JSON/중복속성/PNG CRC·zlib scanline/실제 길이/central directory, inventory 손상·restart. 추출하지 않는 loader와 atomic export. |
| [9 App](../exec_plans/completed/plan-036-app-runtime.md) | CharacterRuntimeViewModel/Coordinator, worker package library/native VM·입력, frame-only MotionSurface/RenderFrameDrawing, Legacy source·processor/geometry | RuntimeIntegrationQa:24개 legacy pixel 비교, native decode/frozen/두 창 scene·voice/action/idle·hotkey/mouse/등록 해제·원본 삭제 후 관리 복사본 restart·손상 보존·garment. 기존 전체 Windows·OBS 회귀. UI 시각 개편은 다음 단계로 분리. |
| [10 Editor](../exec_plans/completed/plan-037-character-editor.md) | 생성→Canvas→파츠→Anchor→얼굴/입/표정→동작→부착물→같은 Runtime preview→검증→export,10화면 | CharacterDocumentTests·CharacterEditorQa: 모든 지원 필드/custom refs/사용하지 않는 원본 PNG bytes 보존, 실제 mouse parent-local drag/cycle 실패 보존,16입·full-sprite 모든 bank,8종 timeline/trigger/pose·재정렬 pending 입력, attachment preview, atomic export/reimport와 native broadcast frame exact.4크기×8페이지 접근성/대비,10실제 desktop 화면. |

## 10화면의 정상 사용자 경로

| 와이어프레임 | 실제 화면·조작 | 근거 |
|---|---|---|
|1 대시보드 | StudioShell 홈: 현재 캐릭터/마이크 선택·미터·시작/중지/데모·음소거/상태·빠른 동작/방송창 | editor-home desktop, Main 고수준 명령·기존 microphone/lifetime QA |
|2 선택/관리 | 캐릭터 검색·cards·추가/등록·metadata·선택/편집/export·관리 복사본 삭제 | editor-library desktop, StudioShellViewModel/NativeCharacterViewModel/CharacterPackageLibrary 및 native 저장/재시작 QA |
|3 기본 정보 | metadata/thumbnail/Canvas 설정과 적용, 새 문서/가져오기 기본 페이지 | editor-basic, editor_metadata_apply/import_opens_basic_information |
|4 부위/이미지 | 목록/PNG/ID/type/side/Anchor/Z/visible/transform·추가/삭제 | editor-parts, part_visibility_runtime/png_prepare/bad_png/pending 보호 |
|5 Anchor | 목록·시각 점/라벨·부모/local transform·실제 drag | editor-anchors, parent_local_transaction/real_mouse_anchor_drag/cycle 보존 |
|6 얼굴 |1~16입 목록/PNG/curve/offset, 음량 simulation·blink/idle/voice, 전체 시트 bank | editor-face, mouth16/slider/full_sprite_* |
|7 표정 | 목록·ID/name·open/closed/brows PNG/얼굴 Anchor/Z | editor-expressions, expression_edit/full_sprite_expression_* |
|8 동작 | ID/name/duration/priority/playback/trigger·pose·8종 timeline·재생/seek | editor-actions, timeline_payload_*/scrub/reordered_events_apply_to_correct_objects |
|9 부착물 |14분류·PNG layer/Anchor/transform/placement/상속/pose/dependencies·착용 | editor-attachments, attachment_same_runtime_preview + Core 충돌/상속 회귀 |
|10 미리보기/export | 투명 checkerboard의 같은 Runtime, 음량/표정/동작/부착 simulation·검증/export | editor-export, all_supported_fields_exact/original_png_bytes/runtime_frame_exact/preview_broadcast_frame_exact |

캐릭터 PNG 시트/기존 설정은 방송 / 설정에서 계속 조작한다. 새로운 Dashboard/library/editor는 기본 앱 경로이며 smoke 전용 helper 화면이 아니다. 테스트 helper는 이전 설정 회귀를 위한 명시 navigation을 제공한다. 사용자 순서는 [사용 설명서](../user-guide.md)에 있다.

## Master 보존·설계 계약

| 요구 | 현재 근거와 판정 |
|---|---|
| 기존 sprite-sheet/ImageSet/표정/wardrobe/손동작 | source/adapters/compat wrappers를 유지. LegacyAdapterTests, LegacyRuntimeIntegrationTests, ImageSet/Expression/Costume/HandMotion/MouthFrame tests와 WPF dedicated QA·restart/손상 복구 통과 |
| 기존 microphone/OBS/broadcast/user settings | AudioCaptureService/Analyzer/Processor/Session 경계를 유지. Audio/Session/CaptureLoop/Settings/Usability/Calibration/WindowPlacement tests, unsigned/signed startup·설정/트레이·hotkey·두 창·OBS 회귀 통과. 물리 실기는 별도 |
| Core에 WPF/NAudio/Win32 없음 | Core net10.0 프로젝트 및 source 검색 확인. App만 NAudio/Win32 입력·WPF renderer/preparation 소유 |
| tick에 FS/JSON 없음 | CharacterRuntime/engine/composer/geometry와 runtime VM/Coordinator의 메모리 경로 확인. loader/store/PNG/JSON/export는 worker, UI Tick은 snapshot/숫자/time을 전달 |
| audio callback 동기 UI dispatcher 없음 | AudioCaptureService는 숫자 LevelAvailable, AudioSession은 최신 snapshot, UI timer가 읽는다. PCM은 짧은 worker buffer이며 저장·전송 없음 |
| deterministic caller time/Random·불변 정의 | Motion/Trigger/Action/Runtime replay와 snapshot tests. Stopwatch는 App, Core engine은 caller monotonic time. wall-clock은 animation 판단에 없음 |
| 경로 신뢰 금지·package root 제한 | PackagePath/loader/PNG validator·크기/개수/CRC/실제 해제 검증. 압축 컨테이너에서만 읽고 임의 위치 추출 없음 |
| 기존 harness·phase별 Plan→test→review→commit | plan028~037과 대응 review·Git 수명 기록, verify_base/verify_app/format/build/Core/Windows/OBS 경로 유지 |
| Legacy 무검증 삭제 금지 | [타입별 참조/삭제 판단](../architecture/legacy-migration-review.md). 기존 DTO/API/설정·파일·migration·QA oracle을 보존하며 이번 최종 검토는 삭제하지 않음 |
| Sound/CHZZK/OBS websocket product/plugin·카메라·추적 제외 | Sound는 예약 무동작·WAV package 거부. 일반 앱에 해당 네트워크/촬영 기능 추가 없음. OBS QA의 일회용 runner websocket 조작은 제품 기능과 구분 |

## 최종 산출물과 검증 범위

[Runtime pipeline](../architecture/voxpet-2-runtime.md)은 Input→TriggerEngine→ActionEngine→MotionEngine→CharacterRuntime→RenderFrame→Renderer 및 실제 내부 조정/worker·UI 수명/preview·broadcast 관계를 설명한다. [.voxpet 규격](voxpet-package-format.md)은 필수/선택 JSON·PNG, metadata/version·모델·지원 event/trigger/attachment/full-sprite·자원/경로 제한·편집 왕복을 설명한다. [Legacy 검토](../architecture/legacy-migration-review.md)는 기존 타입마다 참조·대체·호환 목적·삭제 가능성을 기록한다.

최종 검토96bfbcc의 Windows38670581601은 Core355/355·WPF1744/0·desktop250·signed/unsigned9사례씩을 통과했다. 동일 src/harness tree의 OBS38668937607(d9c55de)에서 WGC/chroma/native alpha/같은 HWND 전환이 통과했다. 정확한 실행·source identity와 QA 이후 자체 리뷰는 [완료 리뷰](../reviews/plan-038-migration-review-review.md)에 기록한다. Windows CI의 public/synthetic·가짜 장치·합성 입력 검사는 실제 개인 PNG/물리 마이크·권한·USB·혼합 DPI/사용자 GPU·실제 입력 장시간의 합격을 주장하지 않는다. 기존 [실기 체크리스트](../quality/windows-checklist.md)의 제한은 유지한다. 편집 초안은 내보내기로 보존하며 미지원 JSON 확장 속성은 지원 데이터 보존 계약에 포함하지 않는다.
