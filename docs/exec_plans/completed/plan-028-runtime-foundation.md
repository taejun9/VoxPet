# plan-028-runtime-foundation

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

첨부한 VoxPet 2.0 와이어프레임10화면과 단계별 마이그레이션 텍스트를 읽고 작업을 진행한다. 텍스트의 Master → Phase 1 → 단계별 검증 순서를 적용한다.

## Goal

기존 앱을 유지하면서 Phase 1의 WPF 독립 불변 모델과 단위 테스트를 추가한다. 와이어프레임과 전체10단계의 연결을 별도 제품 문서에 보존한다.

## Non-Goals

이번 단계에는 UI 변경, Legacy 제거/Adapter, 애니메이션 엔진, 패키지 ZIP 로딩/내보내기, 마이크 변경을 포함하지 않는다. 에디터 구현은 Phase 10이다.

## Context Map

- [앱 설계](../../architecture/application.md), [제품](../../product/product.md), [이미지 세트 v1](../../product/image-set-format.md), [로드맵](../../product/roadmap.md)
- `src/VoxPet.Core/Models/ImageSet.cs`, `CharacterRig.cs`, `CostumeProfile.cs`, `HandMotionProfile.cs`
- `src/VoxPet.Core/Services/CharacterAnimator.cs`, `ImageSetPlayer.cs`, `ConnectedMotion.cs`, `HandMotionScheduler.cs`
- `src/VoxPet.App/ViewModels/ImageRuntimeViewModel.cs`, 기존 Core/App QA
- 사용자 첨부: VoxPet 2.0 10화면 PNG와 개발 계약/Phase 1~10 텍스트. 이미지 원본은 저장소에 복사하지 않고 해석한 요구사항만 기록한다.

## Constraints

No Exec Plan, No Work. main 구현 금지. `codex/plan-028-runtime-foundation` / `.worktree/plan-028-runtime-foundation` 사용.
Core는 WPF/NAudio 독립. 음성/개인 그림/설정 파일의 외부 전송 없음. 기존 ImagePart/ImageAnchor와 Legacy 구현 보존.
QA → 자체 리뷰 → completed 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] 와이어프레임10화면, 단계별 범위, 보존 계약을 제품 문서에 기록.
- [x] CharacterDefinition/CharacterPart/CharacterState/CharacterPose/AnchorDefinition/Transform2D 추가.
- [x] RenderFrame/RenderLayer/VoxPetManifest 추가.
- [x] 유한 수치/기본512×512·비정사각 캔버스/문자열ID/Anchor 참조·중복·부모 cycle 검증.
- [x] 입력 컬렉션을 복사해 모델 변경 누출 방지, asset은 패키지 상대 참조만 허용.
- [x] 신규 모델 전체 테스트와 기존 회귀 실행.
- [x] QA 이후 자체 리뷰, 완료 계획 이동과 리뷰 미러. Git 수명은 완료 기록 커밋 후 순서대로 실행한다.

## QA Plan

macOS arm64, 기존 임시 `/private/tmp/voxpet-sdk019`의 저장소 지정 SDK10.0.401 사용. NuGet/CLI 임시 경로를 사용한다.
`dotnet restore VoxPet.sln --locked-mode`, `dotnet format VoxPet.sln --verify-no-changes --no-restore`, `dotnet build VoxPet.sln -c Release -m:1 -p:UseSharedCompilation=false -nodeReuse:false`, `dotnet test tests/VoxPet.Core.Tests/VoxPet.Core.Tests.csproj -c Release`.
`python3 harness/scripts/verify_base.py`, `python3 harness/scripts/verify_app.py`, `git diff --check` 및 stage 후 검사.
신규 모델의 비유한 값, 참조 무결성, 수치 범위, 불변성, 버전 분리와 모델 계약을 시험한다. 기존 Core 테스트가 전부 통과해야 한다. WPF 교차 빌드는 실행 검증과 구분하며 macOS에서 Windows/WASAPI/OBS 실기는 수행할 수 없다.

## Review Plan

QA 완료 후 같은 에이전트가 별도 자체 리뷰한다. Phase 1 요구사항, 후속 Adapter 표현 가능성, WPF 독립성, 기존 앱/설정/마이크 무변경, 경로 참조 경계와 immutable 컬렉션을 검토한다. 서브에이전트는 사용하지 않는다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-14 | 이번 실행은 Phase 1까지 완료한다. UI 와이어프레임은 Phase 9/10 기준으로 문서화한다. | 첨부 개발 계약의 단계별 진행과 Phase 1~8 UI 보존 요구를 따른다. |
| 2026-10-14 | 생성 시 검증하는 get-only record와 복사된 읽기 전용 컬렉션을 사용한다. | 생성 후 비유한 값/잘못된 참조가 주입되는 것을 방지한다. JSON DTO/패키지 로딩은 Phase 8에서 분리한다. |
| 2026-10-14 | Anchor 좌표는 부모 기준 local 좌표, parent=null은 캔버스 기준이다. | 후속 Transform Tree 해석을 명확히 하며 기존 absolute offset 변환은 Phase 2에서 처리한다. |
| 2026-10-14 | 정상 SDK10.0.401을 가진 `/private/tmp/voxpet-sdk019`로 전환하고 restore/format/test를 승인된 실행 환경에서 재시도한다. | 최초 임시 SDK는 DeveloperCertificates DLL이 누락됐다. sandbox는 NuGet 조회와 MSBuild/test IPC를 차단한다. 필수 검사를 생략하거나 취약성 검사를 끄지 않는다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-14 | 지도 | 자료와 기존 Core/App/테스트 확인. main clean 및 origin/main 동기 확인. 다른 기존 worktree 보존. |
| 2026-10-14 | 조율 | sandbox가 최초 fetch의 FETCH_HEAD 쓰기를 제한해 중단. 승인된 실행 권한으로 동일 fetch 성공 후 지정 worktree 생성. |
| 2026-10-14 | 제작 | Core에9개 public 모델과 공통 검증 helper,10화면/10단계 개발 계약 문서, 신규62개 테스트 추가. App/Legacy/설정/마이크·자산은 변경하지 않았다. |
| 2026-10-14 | 검증 | 테스트의 null case 인자를 nullable로 수정해 xUnit1012 해소. locked restore·format/analyzer·Release build(경고/오류0)·Core261/261 통과. Python 문서79개·자산 하네스 통과. |
| 2026-10-14 | 심사 | QA 후 자체 리뷰. 요구사항·유한 수치·컬렉션 불변성·cycle/참조·경로 검증·의존성 경계 확인. 남은 제한은 리뷰에 기록. |

## Completion Notes

- Phase 1 완료. [개발 계약과 화면 기준](../../product/voxpet-2-migration.md), [자체 리뷰 미러](../../reviews/plan-028-runtime-foundation-review.md).
- Core261/261(기존199+신규62), locked restore, format/analyzer, 전체 솔루션 Release 교차 빌드(경고/오류0), 문서/자산 하네스 통과. 신규 모델에는 WPF/NAudio/Win32/파일/JSON/실시간 시계 의존성이 없다.
- 20,000단계 Anchor 계층과 self/다중/분리 cycle, 누락·중복 참조, 모든 부동소수 필드의 NaN/Infinity,0~1 범위, 경로 이탈, snapshot 외부 변경을 시험했다.
- 실제 WPF/마이크/OBS는 macOS에서 미실행. UI는 아직 와이어프레임으로 개편하지 않았고 ZIP loader·Editor·Runtime은 후속 단계다. 상대 경로 검사만으로 ZIP 안전성을 보장하지 않는다.
- 다음 단계는 LegacyImageSetAdapter와 얼굴/입/의상/동작 호환 표현 확장, 제공 ImageSetTemplate의 무손실 변환 테스트다.
- Git 통합은 이 완료 기록과 stage diff 검증 후 수행하며 실패 시 해당 단계에서 중단한다.
