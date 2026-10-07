# plan-011-expression-motion

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

Ctrl+Shift+F1~F12 단축키에 다양한 표정을 저장하고 자연스럽게 전환한다. 평상시 눈 깜빡임과 우는 표정의 눈물 모션을 제공한다.

## Goal

12개 로컬 영구 표정 슬롯과 전역 단축키, 기본 표정/사용자 3×2 PNG 시트, 연속 입력에도 끊기지 않는 전환, 두 창의 동일한 blink/눈물 모션을 구현한다.

## Non-Goals

음성 녹음·전송, 표정 자동 감정 인식, 웹캠, Live2D, 임의 단축키 편집.

## Context Map

- src/VoxPet.Core/Services/CharacterAnimator.cs: 독립 blink/idle 시간축.
- src/VoxPet.App/ViewModels/CharacterViewModel.cs: 공유 frozen PNG 상태.
- src/VoxPet.App/Services/CharacterSheetLoader.cs: 제한된 로컬 시트 검증.
- src/VoxPet.App/Views/MainWindow.xaml 및 CharacterWindow.xaml: 공유 렌더러/수명.
- docs/quality/development.md 및 windows-checklist.md: 자동/실기 구분.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-011-expression-motion` / `.worktree/plan-011-expression-motion` 사용.
QA → 리뷰 → completed 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거.
오디오 callback/수명 계약 보존. 사용자 파일을 수정하지 않고 선택된 시트만 앱 로컬 저장소에 복사한다.
기존 파일 변경은 이번 기능에 필요한 코드·사용 안내·QA 문서에 한정한다.

## Implementation Plan

- [x] 공식 WPF/Win32 API 확인 및 기본 표정/슬롯/전환/모션 모델 구현.
- [x] 로컬 슬롯 저장 및 사용자 시트 적용·복구, 슬롯 편집 UI 구현.
- [x] 전역 단축키 등록/충돌 안내/해제 및 공유 렌더러 연결.
- [x] 의미 있는 Core/Windows smoke 회귀, 사용 안내와 설계 동기화.
- [x] QA → 자체 리뷰 → 완료/리뷰 미러 → Git 통합·정리.

## QA Plan

macOS에서 verify_base.py, verify_app.py, diff --check, locked restore/format/Release build/Core test. OBS 자동 WGC green/chroma 및 native alpha/7프레임 회귀도 확인한다. Windows CI에서 qa.ps1 -Publish -Smoke로 실제 WPF/전역 단축키/표정/모션/작은 화면 회귀. 단축키 충돌·해제, 슬롯 저장/재실행, 손상 시트 복구, 연속 전환, 유한한 모션을 검사한다. 실제 물리 키보드/OBS/사용자 GPU는 자동 시험과 구분한다.

## Review Plan

QA 후 같은 에이전트가 요구사항·슬롯 데이터 경계·전환 중 수명·개인정보·회귀를 자체 리뷰한다. 서브에이전트를 실행하지 않는다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-07 | 12개 고정 Ctrl+Shift+F 슬롯, 버튼 fallback과 등록 충돌 안내 | 방송 중 설정창 포커스 없이 전환 |
| 2026-10-07 | 기본 표정과 슬롯별 사용자 PNG 시트, 로컬 영구 저장 | 기본 캐릭터와 사용자 캐릭터 모두 표정 저장 지원 |
| 2026-10-07 | F12는 전역 등록하지 않고 두 창의 로컬 키 처리 | Microsoft 문서가 F12를 디버거 예약 키로 규정 |
| 2026-10-07 | 기존 blink/idle과 독립 눈물 시간축, 두 창 공유 상태 | 무음/Stop/음소거에도 모션 유지 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-07 | 설계 | 저장소/기존 blink 구조 확인. sandbox Git 쓰기 제한 이후 승인 실행 경로로 worktree 생성. |
| 2026-10-07 | 제작 | 내장36장/슬롯 초안과 저장값 분리/관리 PNG 복사/공통 렌더러/전역 키와 F12 fallback/회귀 fixture 구현. |
| 2026-10-07 | 검증 | macOS SDK10.0.401 locked restore/Release build(경고0)/Core97/97 통과. 문서·36 PNG·개인정보 정적 검사 통과. 테스트 IPC는 승인 실행 환경을 사용했다. Windows fixture를 이어서 실행한다. |
| 2026-10-07 | 심사 | 로컬 QA 이후 중간 자체 리뷰: 슬롯 초기 로드 바인딩 통지와 기존 JSON 파일 읽기 경계, 중도 전환 snapshot/모션 좌표를 확인·보완했다. 최종 리뷰는 Windows QA 이후 수행한다. |
| 2026-10-07 | 검증 | Windows37576490136은 저장 실패 fixture의 macOS/Windows 예외 종류 차이로 Core96/97에서 중단. production은 두 오류를 이미 처리한다. fixture를 IOException/UnauthorizedAccessException 계약으로 수정. OBS37576490160 통과. |
| 2026-10-07 | 심사 | 허용된40자 표정 이름이 작은 창의 슬롯 버튼을 넘치게 만들 수 있어 최대폭/말줄임/전체 이름 tooltip을 추가하고 실제 Windows 배치에40자 슬롯과 이름 TextBox를 포함했다. |
| 2026-10-07 | 검증 | c18a8c2 Windows37576998278 및 OBS37576998263 최종 통과. Core97/97, 실제 WPF/합성 전역 키/무음 모션/긴 이름 배치/OBS 회귀 확인. |
| 2026-10-07 | 심사 | QA 이후 최종 자체 리뷰 완료. 미해결 구현 결함 없음. 남은 물리 입력/사용자 OBS 실기 조건을 구분한다. |
| 2026-10-07 | 정리 | completed 이동과 리뷰 미러 생성, 사용/설정/배포 안내를 동기화했다. |

## Completion Notes

12개 영구 표정 슬롯/내장6종 및 사용자 PNG, Ctrl+Shift+F1~F11 전역과 F12 내부 fallback, 240ms 중도 전환, 무음 blink/눈물 모션을 구현했다. 슬롯별 눈물 좌표와 즉시 저장/재시작 복구를 제공한다.

macOS locked restore/format/Release build(경고0/오류0)/Core97/97 및 문서/36 PNG/개인정보/diff 검사를 통과했다. 기준 코드 c18a8c2의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37576998278) 및 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37576998263)가 통과했다. Windows native 키 충돌/해제/최소화 전환, 슬롯 PNG 복사/재시작/손상 복구, 무음 blink/tears, 연속 요청, 긴 이름과 작은 창 접근성을 확인했다. 자체 리뷰는 [리뷰 미러](../../reviews/plan-011-expression-motion-review.md)에 기록했다.

실제 물리 키보드·마이크·사용자 GPU/OBS·물리 DPI·실제 입력 장시간은 별도 실기 조건이다. F12는 Windows 예약 키로 두 VoxPet 창 안에서만 제공한다. 정상적인 Git 통합/push/branch/worktree 정리 결과는 최종 사용자 보고와 로컬 증거 summary에 기록한다.
