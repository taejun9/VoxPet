# plan-035-voxpet-package

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

모든2.0 단계를 완성한다. 이번 계획은 Phase 8 .voxpet ZIP+JSON+PNG 로딩·검증 및 에디터가 사용할 공통 export codec이다.

## Goal

manifest/character 필수와 선택 action/attachment/thumbnail을 bounded 메모리에서 읽는다. 원본 경로를 신뢰하지 않고 traversal/중복·절대 경로/확장자/압축 해제·개별 자산·JSON·파일수/PNG 크기를 검증한다. load→export→load가 지원 데이터를 보존한다.

## Non-Goals

임의 ZIP 추출, 실행 위치에 파일 설치, 음원 실행, UI 에디터/오디오 변경, Legacy 제거는 하지 않는다.

## Context Map

- [개발 계약](../../product/voxpet-2-migration.md)
- `src/VoxPet.Core/Packages/VoxPetManifest.cs`, Character/Actions/Attachments/Triggers models
- [Microsoft ZIP entry 경로](https://learn.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchiveentry.fullname?view=net-10.0)
- [Microsoft Windows 파일명 규칙](https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file)

## Constraints

Core WPF/NAudio 독립. Loader/codec은 tick 밖에서만 호출한다. 패키지는 추출하지 않는 메모리 abstraction. 원본 PNG bytes 보존. 단위 테스트는 합성 자료만 사용한다.
`codex/plan-035-voxpet-package` / `.worktree/plan-035-voxpet-package`. QA → 자체 리뷰 → 완료 이동/미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] VoxPetPackage/Source/Loader/Validator/Exception/Limits와 안전 경로 검증 구현.
- [x] 공통 schema2 JSON codec과 exporter 구현. 모든 지원 모델/metadata/asset bytes 왕복 보존.
- [x] ZIP preflight/파일수/실제 해제 크기/JSON duplicate/PNG signature·IHDR·dimension·CRC 검증.
- [x] malformed/traversal/duplicate/reserved/symlink/unsupported/size-bomb/PNG 및 minimal/roundtrip tests.
- [x] 공통 codec으로 native attachment inventory 저장 경계 확장.
- [x] QA 이후 리뷰와 Git 수명 완료.

## QA Plan

SDK10.0.401/macOS arm64: locked restore, format verify, 전체 솔루션 Release build, 전체 Core tests, verify_base/verify_app, stage diff check. Core 단위 테스트로 valid/malformed ZIP과 지원 모델의 왕복을 직접 비교한다.

## Review Plan

QA 후 단일 에이전트 자체 리뷰. archive metadata뿐 아니라 실제 읽은 bytes의 상한, normalized path 충돌/참조/지원 데이터 보존, runtime IO 분리와 원본·설정 보존을 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-14 | archive64MiB, 해제 합계64MiB, PNG16MiB/JSON512KiB, entry512개, PNG4096px/8M pixels·합계128MiB decoded를 기본 상한으로 둔다. | 기존16MiB 의상/PNG를 수용하면서 ZIP bomb과 디코딩 자원 사용을 제한한다. |
| 2026-10-14 | 정규화는 Unicode NFC+case-insensitive 충돌 검증이며 Windows 예약·끝 공백/점 이름도 거부한다. | Windows 대상 파일 공유의 경로 의미 차이를 막는다. |
| 2026-10-14 | ZIP64/multi-volume은 상한 내 패키지에서 필요하지 않아 거부하고 일반 directory marker는 허용한다. | 큰 central directory가 객체로 확장되기 전에 entry 수를 preflight한다. |
| 2026-10-14 | export는 동일 codec/validator로 재검증하고 arbitrary extraction은 하지 않는다. | 편집과 방송이 같은 모델/asset 계약을 사용한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-14 | 설계 | Phase 7 main push·정리 후 전용 worktree/계획 작성. 공식 경로 자료를 확인했다. |

| 2026-10-14 | 검증 | Core338/338(기존317+신규21), locked restore/format verify/Release build 경고0·오류0, 하네스 통과. |
| 2026-10-14 | 심사 | QA 후 자체 리뷰. 실제 ZIP/PNG 해제 길이·CRC·경로·참조와 지원 데이터/PNG 왕복 확인. |

## Completion Notes

Phase 8 완료. Core338/338, locked restore/format verify/Release build 경고0·오류0, 문서/자산 QA와 자체 리뷰 통과. 최초 왕복의 생략 effect transform→identity 차이를 수정하고 전체 재검증했다.

Loader/Validator/Exception/Limits/Package/Source, 경로/PNG 검사, 공통 schema2 JSON codec/atomic exporter 및 native attachment index 저장을 구현했다. arbitrary extraction은 하지 않는다. ZIP central count/실제 길이·CRC, path/normalized duplicate/device/symlink/확장자/자원 상한, JSON 중복/손상/참조, PNG CRC·실제 scanline 길이를 거부하는 시험을 통과했다. minimal package와 모든 지원 이벤트/트리거/부착물 배치·상속 및 v1 자산/Runtime 출력 왕복을 직접 비교했다.

[패키지 규격](../../product/voxpet-package-format.md)에 지원 필드/상한/보존 범위를 기록했다. WPF decode/App 관리 보관함 연결은 Phase 9, 실제 편집 workflow는 Phase 10이다. 모든 파일 처리는 tick 밖에서 호출한다. 원본 PNG bytes 및 기존 wardrobe 설정/이미지는 보존한다.

완료 기록 검사 후 main 병합/push·정리를 수행한다. 전체 목표는 Phase 9/10와 최종 리뷰까지 active다.
