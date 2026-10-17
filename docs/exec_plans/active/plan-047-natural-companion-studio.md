# plan-047-natural-companion-studio

## Status

active

## Owner

project_lead / harness_builder

## User Request

파츠 경계가 드러나지 않게 수정하고 표정 메뉴를 복구한다. 펠리 헤어와 얼굴장식/모자를 제외한 의상 전체를 실제 두 체형에 맞춘다. 의상을 카테고리로 분류한다. 총 발사·무기 휘두르기가 알아보이는 자연스러운 동작과 파지/무기를 구현하고 전체를 테스트한다.

## Goal

별도 인형을 추가하지 않고 늘보군·펠리 원본을 보존한다. 분리/관절의 화면 크기별 이음새를 줄이고 표정·카테고리를 일반 사이드바에서 직접 조작한다. 모든 의상/헤어의 실제 착용을 검토하며 손과 무기의 접점·지지 손·조준/발사/반동·준비/스윙/복귀·활 당김/놓기가 연속 동작에서 보이게 만든다. 새 Windows 테스트 ZIP과 근거를 전달한다.

## Non-Goals

다른 plan024 작업트리·이전 ZIP·원본 PNG·사용자 설정/README/AGENTS를 덮어쓰지 않는다. 개인 PNG는 Git/CI/생성 도구로 전송하지 않는다. 음성 저장/원격 전송·실제 마이크 자동 시작·요청 없는 서브에이전트를 추가하지 않는다. 사람 캐릭터로 두 원본을 교체하거나 실사 작화를 강요하지 않는다.

## Context Map

- [앱 설계](../../architecture/application.md), [개발 환경](../../quality/development.md), [QA 규칙](../../quality/rules.md), [개인정보](../../privacy/principles.md)
- [현재 샘플](../../guides/studio-samples.md), [패키지](../../product/voxpet-package-format.md)
- CompanionPartImages/CompanionParts/RenderFrameDrawing/JointSkinning
- StudioShell/StudioShellViewModel/ExpressionViewModel/CompanionEquipmentViewModel
- generate_companion_art.cjs/package_companion_samples.py/LegacySpriteSheetRuntime

## Constraints

main/origin 880b34a·0/0·깨끗함과 기존 두 작업트리 보존 확인. 전용 codex/plan-047-natural-companion-studio와 .worktree/plan-047-natural-companion-studio에서 단일 에이전트로 진행한다. 개인 원본은 로컬 정렬/합성/해시로 확인하고 공개 교정 시트로 Windows를 시험한다. 기존421 Core/2515 WPF/2096 portable/437화면의 의미를 유지하거나 변경 요구에 대한 대응을 명시한다. 숫자/이름 변경만으로 자연스러운 동작을 완료했다고 판단하지 않는다.

## Implementation Plan

- [x] 현재 경계/의상/동작과 표정 UI 경로를 조사하고 실제 그림·각 동작 다중 시점의 실패를 기록한다.
- [x] 관절 겹침/렌더 경계를 수정하고 표정 메뉴와 의상 카테고리를 구현한다.
- [ ] 두 체형에 맞춘 전체 의상·펠리 측면 헤어·무기/손 파지를 제작·연결한다.
- [ ] 조준·발사·반동·스윙·복귀·양손 지지·활 동작을 구현하고 연속 프레임을 검토한다.
- [ ] 문서/자산/lint/Release/Core/Windows 실제 UI·화면·portable·OBS 전체 QA를 통과한다.
- [ ] QA 후 자체 리뷰·완료 이동/미러·새 ZIP/해시·main ff push·브랜치/작업트리 정상 정리를 수행한다.

## QA Plan

verify_base/verify_app/compileall/diff, SDK10.0.401 locked restore/format/analyzer/Release 경고0·오류0/Core 전체. 새 UI는 실제 표정 선택·슬롯/테스트/편집 복원과 카테고리별 항목/필터·캐릭터 전환을 검사한다. 경계는 정지 전체 픽셀1% 검사만으로 끝내지 않고 원본의 불투명 관절을 여러 출력 배율/동작 시점에서 비교한다. 의상은17개 각각·상의/하의/신발·겉옷/한벌 조합·세 헤어·모자/얼굴장식 보존·입/눈을 두 실제 로컬 원본에서 검토한다. Windows는 공개 교정 자료로 실제 데스크톱/미리보기·방송창/표정/편집/입력/재시작을 시험한다. 무기는 파지 접점과 지지 손, 총구·발사 직전/직후 반동/복귀, 검의 준비/스윙/마무리, 활의 당김/놓기를 최소5시점과 연속 contact sheet로 비교한다. CI 배포 ZIP의 실제 도구 재실행과 로컬 전달 ZIP의 공용 bytes/개인13PNG 해시/PE/CRC를 대조한다. 물리 마이크·GPU/DPI·장시간은 자동 합성과 구분한다.

## Review Plan

전체 QA 후 동일 에이전트의 별도 자체 리뷰로7개 요구마다 실제 출력/조작 근거를 대조한다. 회귀·thread/lifetime·개인정보·원본 보존·앱/ZIP 일치와 실기 한계를 기록한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-17 | 눈에 보이는 겹침/파지/동작 단계를 합격 기준으로 삼고 기존 녹색 검사만으로 완료하지 않는다. | 사용자가 실제 착용과 움직임의 결함을 다시 확인했다. |
| 2026-10-17 | 개인 원본은 로컬에 보존하고 새 그림 생성은 공개 소품/의상만 참고한다. | 기존 외부 전송 금지 선택을 유지하며 체형 등록은 로컬에서 한다. |

| 2026-10-17 | 파츠 대체는 신발에 적용하고 상의/겉옷은 원본 몸통을 유지한다. | 첫 합성에서 몸통을 통째로 숨기면 어깨/목에 틈이 생겨, 가방을 덮는 실제 재단·칼라와 원본 피부의 연속성을 택했다. |
| 2026-10-17 | 기존 타임라인은 step 동작을 보존하고 새 샘플만 보간한다. 손목/몸통 skinning과 두 앵커 효과는 export/editor에서 왕복한다. | 기존 native패키지 호환과 화면의 연속 파지를 함께 보장한다. |
| 2026-10-17 | 기본 smoke 제한을240초(개인 추가270초)로 늘린다. |7무기×5시점×2캐릭터와 표정/카테고리/4창크기의82장 실제 데스크톱 증거를 추가하며 기존 검사를 유지한다. |

| 2026-10-17 | 팔의 단순 행렬 혼합 대신 어깨·팔꿈치·손목 곡선을 사용한다. | 실제 다중 시점 합성에서 팔이 접힐 때 폭이 납작해지는 실패를 확인했고, 팔 단면 폭과 양 끝 앵커를 유지하는 회귀 검사를 추가했다. |

| 2026-10-17 | 기본 두 캐릭터는9종 native 동작 카드만 직접 노출하고 중복된5개 호환 버튼은 샘플이 없을 때 표시한다. | 같은 이름의 이전 동작을 누르면 새 발사/스윙 대신 구 모션이 재생되는 혼동을 막는다. 입력 세부 설정과 호환 엔진은 유지한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-17 | 제작 | 맞댄 반투명 cut 경계의6px 겹침, 표정 독립 페이지(기존 Page0~5 유지),14분류 선택을 구현했다. 신발은 visible로 resolve된 레이어만 발 파츠를 대체한다. |
| 2026-10-17 | 제작 | 타임라인 보간을 opt-in으로 추가하고 몸통/손목 앵커 사이 skinning, 활시위 두 앵커 연결을 추가했다. 무기7종을 별도 그림/파지로 구성하고 조준/두 번 발사/반동/스윙/양손/활 당김을 연결 중이다. |
| 2026-10-17 | 검증 | 초기 컴파일의 테스트 동명 타입2건과 빈 파츠 fixture2건을 발견/수정했다. 의상 첫 합성에서 발목 틈·원단 확대·펠리 목선 불일치를 찾아 조정 중이며 아직 QA 합격으로 판정하지 않았다. |
| 2026-10-17 | 설계 | 새 목표7개와 현재 main/원격/작업트리 상태를 확인하고 전용 계획을 작성했다. 이전 QA 통과를 새 요구의 합격으로 간주하지 않는다. |

## Candidate QA (Windows 대기)

- SDK10.0.401 locked restore 성공. Release 경고0/오류0. Core431/431 통과(`artifacts/test-results/plan047/local/core-candidate.trx`).
- verify_base124문서/verify_app/compileall/diff 통과. C# format verify-no-changes 통과.
- 실제 개인 원본을 업로드하지 않고190개 Core 합성 프레임을 로컬로 출력했다. 펠리 날개/발목/목선과 팔꿈치·파지의 시각 검토를 반복 중이다. 모든 그림이 좋다고 판정하는 근거로 Core 통과 수를 쓰지 않는다.
- Windows82장 추가 화면/관절 배율/표정/카테고리/실제 입력·편집·portable·OBS 검증은 다음 단계다. 아직 완료/배포 합격이 아니다.

## Completion Notes

실제 QA/리뷰 이후 결과와 제한을 기록한다.
