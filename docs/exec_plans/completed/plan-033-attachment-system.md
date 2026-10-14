# plan-033-attachment-system

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

첨부 자료의 모든2.0 단계를 이어서 완성한다. 이번 계획은 Phase 6 Attachment System이다. 전체 목표는 Phase 10 에디터와 최종 마이그레이션 리뷰까지 유지한다.

## Goal

표준14종/custom Anchor의 부모 변환을 합성하고 부착물의 position/rotation/scale/mouth motion 상속을 독립적으로 적용한다. 한벌/상·하의 충돌과 기존 의상 저장/배치/입·머리·손 연결을 보존한다. 입에 붙은 콧수염이 실제 mouth motion을 따르는 테스트를 반드시 포함한다.

## Non-Goals

기존 wardrobe 파일 삭제/재작성, UI 개편, 마이크 변경, PNG 원본 수정/전송, ZIP 패키지 로딩은 하지 않는다. 중앙 Runtime은 Phase 7에서 연결한다.

## Context Map

- [개발 계약](../../product/voxpet-2-migration.md), [앱 설계](../../architecture/application.md)
- `src/VoxPet.Core/Attachments/AttachmentDefinition.cs`, `AttachmentLayer.cs`, `AttachmentInheritance.cs`
- `src/VoxPet.Core/Models/CostumeProfile.cs`, `Services/CostumeStore.cs`, `Character/AnchorDefinition.cs`, `Character/Transform2D.cs`
- `src/VoxPet.App/Services/CostumeImageLoader.cs`, `ViewModels/CostumeViewModel.cs`, `Views/MotionSurface.cs`
- `tests/VoxPet.Core.Tests/CostumeTests.cs`, `HandMotionTests.cs`, `LegacyAdapterTests.cs`

## Constraints

Core WPF/NAudio/Win32 독립. tick에는 파일/JSON 처리 없음. 불변 definitions, 수치 유한, asset 상대 경로. Legacy enum 숫자와 저장 형식 보존.
`codex/plan-033-attachment-system` / `.worktree/plan-033-attachment-system`. QA → 자체 리뷰 → 완료 이동/미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] AttachmentTransform/AttachmentSystem과 표준 category/conflict 계약 구현.
- [x] 부모 Anchor transform tree의 static/dynamic 합성, custom Anchor, 독립 상속 옵션과 mouth motion 경계 구현.
- [x] LegacyCostumeAdapter와 보관함 호환 source/store 경계 구현. 원본 CostumeStore와 파일 읽기 유지.
- [x] 기존 BaseWidth/Height/X/Y/Scale/Follow/BehindCharacter/Enabled·머리·입·손·garment 연결 표현 보존.
- [x] 콧수염 mouth 이동, 부모 rotation/scale, 상속 옵션0%, custom Anchor, onePiece 충돌, 기존 wardrobe 재읽기/원본 byte 보존 시험.
- [x] Core 회귀와 QA 이후 리뷰, 완료 기록 및 Git 수명 절차.

## QA Plan

macOS arm64/SDK10.0.401(`/private/tmp/voxpet-sdk019`), tmp CLI/NuGet cache. locked restore, format verify, 솔루션 Release build(경고/오류0), 전체 Core tests, verify_base/verify_app, stage diff check. 필요한 Windows WPF/OBS 검사는 작업 브랜치의 CI gate를 사용하며 합성 자료만 전송한다.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰. 수치/부모 계층/상속 component·mouth motion·충돌·Legacy 설정 무손실 보존과 파일/음성 데이터 경계를 검토한다. pixel/렌더 통합 증명은 Phase 7/9에서도 수행한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-14 | 기존 JPG/BMP/GIF 보관 이미지와 wardrobe.json은 그대로 읽고 새 정의는 관리 asset 참조로 변환한다. | 현재 CostumeImageLoader는 PNG 외 형식도 지원하므로 PNG-only 신규 패키지 규격으로 기존 저장값을 깨뜨리지 않는다. |
| 2026-10-14 | 기존 full-canvas 이미지와 의상 logical 크기/위치를 구분하는 배치 metadata를 보존한다. | ImageSet 레이어는 공통 Canvas이고 CostumeProfile은 BaseWidth/Height로 배치하므로 같은 기본 fit 규칙을 강제할 수 없다. |
| 2026-10-14 | 부모 계층의 비균일 scale+rotation은 affine 변환으로 정확히 합성한다. | 단순 rotation/scale 덧셈으로 계층 shear를 잃는 회귀를 피하고 renderer에는 계산 결과만 전달한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-14 | 조율 | Phase 5 Windows/Core295 및 WPF1491/0·OBS QA, 최종 리뷰, main 병합/push·작업 브랜치 정리 완료 후 전용 worktree 생성. |
| 2026-10-14 | 지도 | CostumeProfile/Store/ImageLoader/ViewModel/MotionSurface를 확인했다. 기존 BaseWidth/Height,Follow,garment의 연결 격자와 외부 Viewbox의 Bounce 계약을 후속 Runtime에서 보존해야 한다. |
| 2026-10-14 | 제작 | affine/RigPoint, AttachmentTransform 배치 metadata, LegacyCostumeAdapter와 기존 store 읽기 facade를 추가했다. 부모 상속/충돌 시스템 및 입 이동 시험은 아직 진행 중이다. |
| 2026-10-14 | 검증 | 기반 코드 단계의 locked restore/format verify/Release build 경고0·오류0, Core301/301(기존295+기반 신규6) 통과. Phase 6 전체 완료 검증은 아니다. |

| 2026-10-14 | 검증 | 전체 Core309/309, locked restore/format verify/Release build 경고0·오류0, 문서·자산 검사 통과. |
| 2026-10-14 | 심사 | QA 후 자체 리뷰: affine/부모·mouth 독립 상속·분류·저장 복구 및 원본 bytes 보존 확인. |

## Completion Notes

Phase 6 완료. Core309/309(기존295+신규14), locked restore/format verify/Release build 경고0·오류0, 문서·자산 검사와 QA 이후 자체 리뷰를 통과했다.

RigDefinition/RigMotionSolver는 부모 순서를 검증하고 static/dynamic/mouth 변환을 정확한 affine로 합성한다. custom/표준14Anchor, position/rotation/scale/mouth 독립 상속과 Follow0/50/100%, 반전·큰 수치 경계를 시험했다. 콧수염은 mouth와 head를 따르고 안경은 mouth motion을 제외하는 실제 mapped point 차이를 확인했다. onePiece/상의/하의 충돌과 복수 얼굴 장식·효과, 기존 wardrobe의 앞 항목 우선 복구를 보존한다.

LegacyCostumeAdapter와 AttachmentStore 읽기 facade는 원본 JSON/opaque 이미지 bytes를 변경하지 않고 BaseWidth/Height/X/Y/Scale/Follow/BehindCharacter/Enabled 및 garment 연결 metadata를 보존한다. 기존 CostumeStore·enum·App·오디오 경로는 유지했다. 새 데이터의 연결 격자와 최종 pixel 렌더는 Phase 7/9에서 검증한다. native inventory 저장은 패키지 codec 이후 확장하며 현재 기존 저장 형식은 계속 사용한다.

완료 기록과 stage diff 검사 후 main 병합/push·브랜치/worktree 정리를 수행한다. 전체 목표는 Phase 7~10과 최종 리뷰까지 진행 중이다.
