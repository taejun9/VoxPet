# plan-021-character-selection

## Status

active

## Owner

project_lead / plan_keeper

## User Request

기존 늘보군, 첨부1 그림 늘보군, 첨부2 늘보맨, 첨부3 늘보걸, 첨부4 측면 펠리 다섯 캐릭터를 선택할 수 있게 수정한다.

## Goal

캐릭터 탭에서 캐릭터를 선택하고 두 창에 같은 캐릭터와 입/눈 반응을 적용한다. 선택은 재실행에도 유지한다. 기존 늘보군과 사용자 표정 슬롯/의상을 보존한다.

## Non-Goals

오디오 엔진 변경, 음성 저장/원격 전송, 사용자 원본 덮어쓰기, 다른 진행 작업 plan-020 수정.

## Context Map

- src/VoxPet.App/ViewModels/CharacterViewModel.cs, MainViewModel.cs, ExpressionViewModel.cs
- src/VoxPet.App/Services/CharacterSheetLoader.cs, SettingsStore.cs
- src/VoxPet.App/Views/MainWindow.xaml, App.xaml.cs와 Windows QA
- 기존 개인 늘보군: artifacts/VoxPet-neulbo-plan019/Characters
- 네 첨부 이미지는 사용자 Downloads에서 읽기만 수행한다.
- [개발 환경](../../quality/development.md), [품질 규칙](../../quality/rules.md), [Git 절차](../../architecture/harness.md)

## Constraints

`codex/plan-021-character-selection` / `.worktree/plan-021-character-selection` 사용.
No Exec Plan, No Work. main 구현 금지. 개인 캐릭터 PNG는 기존 배포 정책대로 ignored artifacts의 개인 패키지에만 포함한다.
QA → 자체 리뷰 → completed 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] 다섯 항목의 안정적인 ID와 로컬 캐릭터 카탈로그/선택 저장 구현
- [x] imagegen으로 네 첨부의 투명 배경 3열×2행 반응 시트 준비, 펠리 측면 유지
- [x] 캐릭터 탭 선택 UI, 비동기 전환과 실패 복구, 표정 테스트와의 전환 경합 처리
- [ ] 선택/설정 호환 Core 검사와 Windows 합성 QA, 문서/개인 패키지 갱신
- [ ] QA 후 자체 리뷰와 Git 수명 완료

## QA Plan

macOS: verify_base.py, verify_app.py, diff --check, locked restore, format, Release build, Core tests, 개인 PNG의 투명/셀 크기/시각 검사.
Windows: 기존 CI smoke에 다섯 캐릭터 카탈로그 fixture와 연속 선택/표정 전환/창 공유/실패 복구 추가. CI는 합성 fixture만 사용한다. 실제 개인 PNG와 물리 마이크/OBS는 로컬 Windows 실기 대상이며 macOS 실행 성공으로 보고하지 않는다.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰: 다섯 이름/순서와 측면 펠리, 기존 기본/슬롯/의상 보존, frozen worker decode, 빠른 선택과 종료 경합, 개인 자산 Git 제외 확인.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-12 | plan-021 별도 worktree 사용 | plan-020 작업 중이며 main과 원격 동기 상태 확인 |
| 2026-10-12 | 개인 캐릭터는 실행 폴더 Characters에서 카탈로그로 제공하고 공개 CI는 fixture 사용 | 기존 개인 PNG 배포/공개 저장소 경계를 유지 |
| 2026-10-12 | 신규 네 캐릭터는 우선 3입×2눈 시트로 준비하고 없는 표정은 해당 캐릭터 평상으로 복구 | 첨부는 프로필/참고 시트이며 안전한 기존 PNG 계약으로 통합 |

| 2026-10-12 | 기존 캐릭터 탭 상단에 선택 UI 배치 | 미리보기/방송창/PNG 세부 페이지 어디서나 선택 가능 |
| 2026-10-12 | 종료 진입에서 표정/캐릭터 gate를 먼저 닫고 오디오 해제와 함께 기다림 | 마이크 Stop 지연 동안 준비된 캐릭터가 종료 도중 적용되는 경합을 자체 리뷰에서 확인 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-12 | 지도 | 저장소/QA/설정/기본 PNG 경로 확인. 첫 fetch는 sandbox에서 FETCH_HEAD 쓰기 제한으로 실패, 승인 실행으로 성공. 원격과 main 일치. |

## Completion Notes

구현과 QA 진행 중. macOS에서 WPF 실행은 불가능하므로 Windows 합성 QA 결과와 실기 제한을 별도 기록한다.

### 초기 QA 기록

- verify_base 61문서 / verify_app 36공개PNG / diff --check 통과.
- 고정 SDK 10.0.401 locked restore, Release build 경고0/오류0, format --verify-no-changes 통과.
- 최초 로컬 SDK 경로는 인증서 assembly 누락으로 실패하여 기존 정상 SDK019 사용. restore는 sandbox 네트워크 제한으로 NU1900, 승인 실행에서 취약성 검사 포함 성공.
- Core test는 sandbox 로컬 소켓 금지로 시작 실패, 승인 환경에서 재실행.
- Core160/160 통과. 최초 Windows CI38231326083은 build/Core160 성공, WPF884고유검사 통과와24개 실패. 실패는 캐릭터 ComboBox 선택값/팝업 글씨가 어두운 테마 색을 사용한 것뿐이며 명시적 검정 ItemTemplate으로 수정. 신규 실제 선택·경합·파일 복구 검사 전부 통과. OBS38231326034 성공.
- 신규4RGBA 시트/24셀/6고유 상태/셀 내부 여백·알파 확인, 기존12PNG byte-identical. 개인 시트/프롬프트/QA 기록은 ignored artifacts에 보관.
- 2f9bc4f Windows38231988396 전체 통과 후 자체 리뷰에서 종료 순서 경합 발견. delayed Stop fixture를 추가하여 사용자 마이크 없이 종료 지연 도중 캐릭터 적용 차단을 검증한다.
- Windows CI용 실제 Selector/선택 경합 fixture와 일반 EXE 펠리 복원·누락·손상 사례 추가. 개인 자료는 CI에 전달하지 않음.
