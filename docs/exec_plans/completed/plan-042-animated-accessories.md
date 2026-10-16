# plan-042-animated-accessories

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

전체 샘플 포함 여부를 확인하고 모자는 프로펠러가 도는 헬리콥터 모자, 망토는 날개가 펄럭이는 형태로 바꾼다. 단발·장발·폭탄머리 헤어스타일을 추가하고 전체 검증 후 Windows 시험 파일을 제공한다.

## Goal

기존14분류/9동작을 빠짐없이 유지하고3개 헤어를 추가한다. 모자/날개는 착용 중 음성이나 동작과 독립적으로 움직이며 preview/방송창이 같은 frame을 사용한다. 애니메이션 정의는 편집/내보내기/재등록에도 보존된다. 전체 QA와 검증된 새 Windows ZIP을 준비한다.

## Non-Goals

개인 PNG/설정/이전 ZIP 삭제·덮어쓰기·외부 전송, 녹음/웹캠/추적, 물리 마이크/GPU/DPI 실기 합격 주장은 하지 않는다.

## Context Map

- Core Attachments/Rendering/Packages/Character 및 App CharacterEditorViewModel/StudioShellViewModel/UnifiedStudioQa
- harness/scripts/generate_studio_samples.py / verify_screens.py, StudioSampleTests
- [샘플 안내](../../guides/studio-samples.md), [개발 환경](../../quality/development.md), [QA 규칙](../../quality/rules.md)
- [파일 형식](../../product/voxpet-package-format.md)의 실제 위치/기존 schema를 구현 전에 확인한다.

## Constraints

codex/plan-042-animated-accessories / .worktree/plan-042-animated-accessories에서 단일 에이전트 작업. plan024 작업트리2개·개인13PNG·기존 ZIP을 보존한다. UI/오디오 분리, 유한 시간·메모리/프레임 수 상한, 기존 정적 attachment JSON 호환. QA 후 자체 리뷰·완료 계획/미러·main ff/push·branch -d·worktree 제거 순서를 지킨다.

## Implementation Plan

- [x] 기존 샘플14분류/9동작과 애니메이션·편집·업그레이드 경로를 확인한다.
- [x] 선택적 PNG 프레임 반복 애니메이션과 파일 로드/검증/왕복·편집 보존을 구현한다.
- [x] 헬리콥터 모자/날개 망토와 헤어3종을 생성하고 앱의 기존 샘플 복사본에서도 새 버전을 선택하게 한다.
- [x] Core/format/analyzer/Release/하네스 및 Windows/OBS/화면/ZIP 전체 QA를 수행한다.
- [x] 시각 검토·개인 자산 보존 ZIP·자체 리뷰·완료 기록을 준비한다.

## QA Plan

새 작업트리 locked restore → format verify → Release 경고0/오류0 → Core 전체, verify_base/verify_app/compileall/diff check. 애니메이션 경계·루프·큰 시간/잘못된 정의·정적 호환·포즈 우선순위·파일 왕복/누락 자산·편집 보존을 검사한다. 샘플은 모든14분류와17항목/9동작·헤어 배타/모자 공존·모자/날개의 서로 다른 픽셀/idle 재생·해제와 방송 공유를 실제 WPF에서 확인한다. 새 Windows/OBS runner와 portable ZIP 전체 실행, 변경 화면 시각 검토, 개인13PNG/public files byte equality/PE/CRC/SHA256을 확인한다.

## Review Plan

모든 QA 후 같은 에이전트가 자체 리뷰한다. 타임라인 독립성·프레임 상한/불변 복사·기존 JSON과편집 보존·캐릭터 보관함 업그레이드/사용자 복사본 보존·모자/헤어/날개 겹침·개인정보와 ZIP을 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-16 | 기존14분류/9동작은 유지하고 헤어3종을 머리 장식으로 추가해 모자와 동시에 착용한다. | 헬리콥터 모자와 헤어를 함께 시험하고 기존 hat 분류의 배타 규칙을 보존한다. |
| 2026-10-16 | attachment layer에 선택적 반복 PNG frame 정의를 추가하고 editor에서도 프레임/속도를 수정할 수 있게 한다. | 손동작 재생과 별개로 idle에서도 모자/날개가 움직여야 하며 패키지 재등록 때도 동작해야 한다. |

| 2026-10-16 | Windows 첫 실행에서 발견한 편집기 선택값의 흰 글씨를 명시적 검정 item template으로 수정하고 선택/팝업 검사를 추가한다. | 새 애니메이션 편집 진입으로 기존 편집기 선택값 대비 문제를 재현했다. 정지→반복 신규 생성의 기본 간격100ms는 fixture에서160ms를 다시 지정하도록 바로잡는다. |

| 2026-10-16 | 시각 QA에서 편집 후 메뉴 전환 시 남은 외부 스크롤 위치가 제목/미리보기를 가리는 문제를 수정한다. | Page 전환 시 상단으로 복원하고 실제 스크롤 후 이동을 재현하는 회귀 검사를 추가한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-16 | 설계 | main8c55560 깨끗함/origin과0·0 확인, 전용 작업트리/계획 작성. 기존 샘플14의상·9동작 확인. |

| 2026-10-16 | 제작 | PNG 반복 재생/codec/편집 필드·8프레임 모자·6프레임 날개·헤어3종/총17종·샘플1.1.0 업그레이드를 구현했다. 이전 복사본을 보존한다. |
| 2026-10-16 | 검증 | locked restore, Core380/380, Release 경고0/오류0, base/app/compileall/diff 통과. Windows 화면·편집 왕복 회귀 검사를 확장했다. |

| 2026-10-16 | 검증 | 첫 Windows38878311478: Core380/경고0·오류0, WPF2027통과/2실패. 편집기 선택 글자색 문제와 fixture 간격 기대를 수정한다. OBS38878311487은 source1dd8fab 일치/WGC·크로마·alpha·동일 HWND·각7프레임 통과했다. |

| 2026-10-16 | 검증 | 두 번째 Windows38879448822: Core380/WPF2035·portable1732/실화면321/unsigned·signed 각9개 모두 통과. OBS38879448821도 최종source3cc710c에서 통과. 시각 QA에서 편집→동작 이동의 스크롤 잔류를 추가로 발견해 수정·전체 재검증한다. 중간 ZIP042는 보존하고 최종 ZIP은 별도 이름을 쓴다. |

| 2026-10-16 | 검증 | 최종 dad72a1의 Windows38881016955/OBS38881016833 전체 통과. Core380/380·WPF2037/0(기존1981개 모두 유지·56개 추가)·실제 desktop321·unsigned/signed 각9개·portable1734/0. format/analyzer/Release 경고0·오류0 및 샘플78파일/50PNG CRC 통과. |
| 2026-10-16 | 심사 | 전체 QA 후 단일 에이전트 자체 리뷰와 최종72화면 시각 검토. 모자/날개·헤어/편집·상단 복원 정상, 추가 필수 수정 사항 없음. CI 공용39파일+개인13PNG byte equality/PE x64/CRC/SHA256 통과. |

## Completion Notes

의상14분류/17종·동작9종을 모두 포함한다. 헬리콥터 모자8프레임/80ms와 날개6프레임/140ms를 착용 중 독립 반복하고 단발·장발·폭탄머리를 추가했다. 헤어는 머리 장식으로 서로 교체하며 모자와 공존한다. 반복 PNG/속도 편집·검증·export/load 및 이전 샘플 보존/새1.1.0 선택을 지원한다. 편집기 선택 글씨와 편집 후 메뉴 전환 스크롤 잔류도 수정했다.

최종 시험 코드 dad72a1, [자체 리뷰](../../reviews/plan-042-animated-accessories-review.md)에 QA·수정·실기 제한을 기록했다. 이후 변경은 기록 문서뿐이다. 새 파일 `artifacts/VoxPet-Windows-test-plan042-final.zip`은52파일/95,863,839bytes, SHA256 `c45f653663f2d0438320d2a2fb5930965b0c7c6f92255abcab11a073680d571f`다. Windows에서 동봉 도구로 시험한 공용39파일을 byte-identical로 유지하고 개인13PNG만 로컬에서 그대로 추가했다. 기존 ZIP/원본을 보존하고 개인 자산을 외부로 보내지 않았다.

최종 증거는 `artifacts/test-results/plan042/final/`, 로컬 Core/샘플 증거는 `plan042/local/`, 이전 실행 증거는 `attempt1/`·`attempt2/`에 보존한다. 물리 마이크/권한/USB·개인 그림의 Windows 시각 확인·혼합 DPI/사용자 GPU·새60분 실기는 대상PC 항목이다. 최종 문서 검증 후 하네스 순서로 main ff/push·branch -d·worktree 제거를 진행한다.
