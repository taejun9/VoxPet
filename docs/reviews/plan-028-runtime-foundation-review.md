# plan-028-runtime-foundation Review

## Summary

사용자 첨부의 단계별 계약에 따라 Phase 1만 구현했다. Core 기반9개 public 모델, 공통 검증 helper, 신규 모델 테스트, 와이어프레임10화면과10단계 제품 기준을 검토했다. QA 완료 후 같은 에이전트가 수행한 자체 리뷰이며 독립 리뷰로 표기하지 않는다. [완료 계획](../exec_plans/completed/plan-028-runtime-foundation.md), [개발 계약](../product/voxpet-2-migration.md).

## QA

macOS arm64 / .NET SDK10.0.401(`/private/tmp/voxpet-sdk019`), 임시 CLI/NuGet cache. 저장소의 고정 SDK/lock/패키지를 변경하지 않았다.

| 명령/검사 | 결과 |
|---|---|
| `dotnet restore VoxPet.sln --locked-mode -m:1 -p:UseSharedCompilation=false -nodeReuse:false` | 승인된 실행 환경에서 통과. NuGet 취약성 조회를 비활성화하지 않음 |
| `dotnet format VoxPet.sln --verify-no-changes --no-restore` | 승인된 실행 환경에서 통과, 변경 필요 없음 |
| `dotnet build VoxPet.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false -nodeReuse:false` | Core/App/Tests 교차 빌드 통과, 경고0·오류0 |
| `dotnet test tests/VoxPet.Core.Tests/VoxPet.Core.Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=plan028-core.trx' -m:1 -nodeReuse:false` | 승인된 실행 환경에서261/261 통과(기존199·신규62), skipped0 |
| `python3 harness/scripts/verify_base.py` | 완료 계획 이동/리뷰 추가 후80개 문서 구조·링크·계획·리뷰·출처 통과 |
| `python3 harness/scripts/verify_app.py` | RGBA/manifest/고정 패키지/금지 API 검사 통과 |
| 신규 모델 의존성 읽기/검색 | WPF/NAudio/Win32/파일/JSON/wall-clock 의존성 없음 |
| `git diff --check` | 통과. 신규 파일은 stage 후 동일 공백 검사를 커밋 전에 추가 수행 |

최초 임시 SDK(`/private/tmp/voxpet-dotnet`)는 DeveloperCertificates DLL 누락으로 실패해 정상 SDK로 전환했다. sandbox의 NuGet 접근, format named pipe, test loopback socket 제한은 승인된 환경에서 같은 필수 명령을 재실행하여 해소했다. 첫 빌드의 xUnit1012(null InlineData의 non-nullable 인자)는 테스트 인자만 nullable로 수정하고 전체 빌드·서식·테스트를 통과했다.

## Findings

- 해결되지 않은 Phase 1 요구사항/회귀 결함은 발견하지 않았다. 수치는 생성 시 검증하고 public 프로퍼티를 get-only로 제공한다. 파츠/Anchor/레이어/부착물ID 중복과 누락 참조를 거부한다.
- cycle 검사는 반복 순회로 깊은 계층의 stack overflow를 피한다. 부모 순서가 뒤섞인 트리, 분리 cycle 및20,000단계 트리를 시험했다. 모든 컬렉션은 입력을 복사하고 읽기 전용으로 노출한다.
- Transform2D는 반전용 음수 배율을 허용하고0배율을 거부한다. Anchor는 부모 기준 local 좌표를 사용하며 별도 x/y와 transform 위치가 모호한 입력을 거부한다. RenderFrame은 zIndex 동률의 입력 순서를 보존한다.
- ImageSet/ImagePart/ImageAnchor, CharacterAnimator/CharacterRig/ConnectedMotion, Costume/HandMotion, App 코드와 기존 테스트/패키지 lock/사용자 설정 형식은 그대로 보존했다. 신규 모델은 기존 UI 호출 경로에 연결하지 않았다.
- manifest는 formatVersion2와 캐릭터 version을 분리한다. action/attachment/thumbnail 생략을 허용하고 상대 PNG/JSON 경로의 절대 경로·상위 이동·빈 segment·역슬래시·URL 형식을 거부한다. ZIP 구조/자산 존재 검증은 Phase 8 범위로 명확히 기록했다.

## Residual Risk

이번 검증은 Core 자동 시험 및 Windows 대상 교차 빌드다. 실제 WPF 화면/마이크·권한·장치/키보드/OBS/사용자 GPU는 macOS에서 미실행이며 기존 Windows 실기 제한을 해소하지 않는다. 신규 모델만으로 사용자 캐릭터를 `.voxpet`으로 읽거나 방송하거나 편집할 수 없다. 얼굴·액션·부착물 정의와 계층 합성은 이후 단계에서 확장한다. 기록된 경로 형식 검증은 완전한 패키지 보안 검증이 아니다.

## Follow-Ups

Phase 2 계획에서 LegacyImageSetAdapter와 입/표정/의상/무기/프레임 시간의 표현을 추가하고 기존 제공 ImageSetTemplate의 무손실 변환을 시험한다. 최종10화면 UI는 Runtime/Package/App 연결 검증 이후 구축한다. 기존 파일/설정 이관에 필요한 Legacy 구현은 최종 삭제 가능성 리뷰 전 제거하지 않는다.
