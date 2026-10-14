# plan-036-app-runtime

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

첨부 자료의 모든 단계를 순서대로 완성한다. Phase 9에서 중앙 Runtime을 실제 WPF 앱에 연결한다.

## Goal

ImageSet와 native package, 기존 sprite-sheet/표정/의상/손동작을 공통 CharacterRuntime과 RenderFrame 경계로 연결한다. frozen bitmap 준비·보관함 IO는 worker에서 수행하고 MainViewModel의 캐릭터 조정 책임은 Runtime VM으로 옮긴다. 두 창과 후속 편집 미리보기는 같은 Runtime/renderer를 사용한다.

## Non-Goals

Phase 10의 화면 개편은 후속 계획에서 수행한다. 마이크 처리·음성 저장/전송·개인 PNG 공개·기존 설정 삭제는 하지 않는다.

## Context Map

- [전체 계약](../../product/voxpet-2-migration.md)
- [패키지 규격](../../product/voxpet-package-format.md)
- src/VoxPet.App/ViewModels/CharacterViewModel.cs, ImageRuntimeViewModel.cs, MainViewModel.cs
- src/VoxPet.App/Views/MotionSurface.cs, CharacterView.xaml
- src/VoxPet.Core/Character/CharacterRuntime.cs, Rendering/RenderComposer.cs

## Constraints

Core에 WPF/Win32/NAudio를 넣지 않는다. tick에 파일/JSON 처리를 넣지 않는다. 기존 QA 공개 API와 saved settings·원본 이미지를 보존한다. UI 외부 body bounce DIP와 기존 연결 mesh/tear/240ms transition을 유지한다. 전역 입력 예약 키와 기본 mouse off를 유지한다.
전용 codex/plan-036-app-runtime / .worktree/plan-036-app-runtime에서 작업한다. 서브에이전트 없이 QA 이후 자체 리뷰한다.

## Implementation Plan

- [x] 공통 Runtime VM/bitmap source와 RenderFrame 전용 WPF renderer를 구현한다.
- [x] ImageSet 실행을 LegacyImageSetSource/Runtime으로 전환하고 기존 명령·설정을 유지한다.
- [x] LegacySpriteSheetAdapter/Source/Importer, pure legacy geometry/composition과 expression transition을 연결한다.
- [x] native .voxpet 보관함 import/restore/selection과 안전 decode·input lifetime을 연결한다.
- [x] MainViewModel 조정 책임 축소와 두 창 공유를 검증한다.
- [x] Core/Windows WPF/OBS 회귀와 synthetic native package 시험을 수행한다.
- [x] QA → 자체 리뷰 → 완료 이동/미러 → main 병합/push → branch -d → worktree 정리를 수행한다.

## QA Plan

verify_base/verify_app/diff check, SDK10.0.401 locked restore/format/Release build/Core tests. App 변경 후 task branch를 CI에 올려 Windows smoke·OBS를 통과하고 JSON/TRX/화면 artifact를 확인한 뒤 main에 병합한다. public/synthetic fixture만 전송한다. macOS에서는 WPF 실행 불가이며 실제 마이크/물리 입력은 기존 실기 체크리스트를 따른다.

## Review Plan

QA 후 단일 에이전트 자체 리뷰. runtime/renderer 경계, legacy pixel·시간·저장 상태 보존, worker/frozen/cache, 종료 중 입력/로드, native 파일 실패 복구와 개인정보를 검토한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-14 | UI 시각 개편보다 Runtime/renderer 연결과 legacy 회귀를 먼저 검증한다. | Phase 9/10 순서와 기존 동작 보존 계약을 따른다. |

| 2026-10-15 | legacy PNG/mesh/vector migration pixel QA의 채널 평균 오차≤1/255, 변경 byte≤1%를 사전 기준으로 둔다. JSON에 사례별 오차를 기록하고 기존 독립 color-centroid/얼굴 보존 QA도 필수로 통과한다. | 같은 WPF의 opaque DrawingImage/triangle 경계 raster 차이는 한정하되 모션/배치 변화는 실패시킨다. |

| 2026-10-15 | full-sprite의 모든 mouth/eye asset을 CharacterDefinition.SpriteSheet로 표현하고 codec/RenderComposer도 같은 bank를 지원한다. | LegacySpriteSheetSource를 processor 없이는 재생할 수 없는 불완전한 정의로 두지 않는다. 기존 표정 extras/mesh만 호환 processor에 남긴다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-14 | 설계 | Phase 8 검증·main push·작업 정리 후 전용 계획을 작성했다. |

| 2026-10-15 | 제작 | ImageSet와 sprite-sheet를 중앙 Runtime/Core frame으로 연결했다. Main의 animation 조정은 CharacterRuntimeCoordinator에 옮겼다. native worker decode/보관함/선택과 입력 lifetime을 추가했다. |
| 2026-10-15 | 검증 | 중간 Release build 경고0·오류0, Core345/345(신규7), 문서/자산 검사 통과. Windows QA와 최종 formatter 검사 진행 중. |

| 2026-10-15 | 검증 | 첫 Windows run38595897122는 Core345 통과/24pixel·native 복원 검사 통과 후 frozen scene의 공유 engine 시간보다 이전 scheduler clock에서 실패했다. native mouse pulse30ms 확인도 실패했다. scheduler는 engine.LastTime도 clamp하고 click 검사는 최대500ms event 전달을 확인하도록 보완한다. OBS38595897181 성공. |
| 2026-10-15 | 심사 | scene 변경 전 WM_HOTKEY는 고유 registration ID로 차단하고 native 목록 읽기 실패도 복구한다. pixel 수치/새 frame PNG를 Windows artifact에 포함한다. |

| 2026-10-15 | 검증 | 두 번째 Windows38599080687 성공: Core349, WPF1533/0, pixel24개 최대 평균 byte오차0.00002861/변경비율0.00001907. OBS38599080563 성공. |
| 2026-10-15 | 제작 | 추가 리뷰에서 native garment가 선택되는 action의 geometry 경계를 발견해 공통 AnchorDelta/9×9 native rig provider를 연결한다. 기존 표정 선택도 native expression ID에 연결하고 실제 WPF 회귀를 추가한다. |

| 2026-10-15 | 검증 | 최종 Windows38603070570/OBS38603070843 성공. Core351/351, WPF1535/0, desktop225 coverage 및 일반/서명 startup, native garment/표정/입력/보관함·pixel24 확인. |
| 2026-10-15 | 심사 | QA 후 단일 에이전트 자체 리뷰. renderer 판단 제거·공유 engine/source·worker 준비·old clock/queued input/원본 보존을 확인했다. |

## Completion Notes

Phase 9 완료. 최종 구현1e1b57f에서 Windows [38603070570](https://github.com/taejun9/VoxPet/actions/runs/38603070570) 및 OBS [38603070843](https://github.com/taejun9/VoxPet/actions/runs/38603070843) 통과. local/Windows Core351/351, WPF1535/0, format/Release build 경고0·오류0, 하네스 및 실제225화면 coverage/일반·서명 startup 검사 통과. artifact TRX/JSON을 직접 확인하고 native garment frame을 시각 검토했다. pixel24사례 최대 평균 byte오차0.00002861/변경비율0.00001907이다.

중앙 Runtime VM/서비스, Frame-only renderer, ImageSet/sprite-sheet source, 공유 ActionEngine/legacy mesh/transition/tear와 native package 보관함·worker decode/runtime 준비·입력 lifetime을 연결했다. 같은 Frame을 두 창이 공유한다. native garment/Follow/독립 상속과 기존 표정 선택도 연결한다. 최초 Windows frozen-clock 실패는 shared engine 시간을 coalesce해 수정하고 full-sprite bank를 공통 정의/codec으로 표현했다.

QA 이후 자체 리뷰를 수행했다. native 액션/착용 조작의 새 화면과 상세 input 상태/에디터 UI는 Phase 10에서 제공한다. 기존 시트/설정/원본/개인 PNG는 보존했다. Windows 물리 마이크·혼합 DPI/실기·장시간 검증은 기존 체크리스트 한계를 유지한다. OBS WGC/alpha/chroma/same-HWND 통과, BitBlt 호환 제한은 기존대로 남는다.

완료 기록 검사 후 main 병합/push·정리를 수행한다. 전체 목표는 Phase 10 및 최종 마이그레이션 리뷰까지 active다.
