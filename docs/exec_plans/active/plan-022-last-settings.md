# plan-022-last-settings

## Status

active

## Owner

project_lead / plan_keeper

## User Request

마지막 사용한 사이즈·마이크·위치 등의 설정을 껐다 켜도 유지한다.

## Goal

설정창/방송창 크기·위치, 설정창 최대화, 방송창 열림 여부, 마지막 마이크 선택, 오디오 조정/배경/항상 위/입 음소거/캐릭터/마지막 표정·탭을 로컬 저장하고 재실행에 복원한다. 변경은 지연 자동 저장하며 정상 종료에 최종 flush한다. 분리 모니터/없는 마이크/이전 설정/손상 필드에서 앱이 접근 가능하게 복구한다.

## Non-Goals

실행 시 마이크 자동 Start, 데모/측정 중간 상태 복원, 음성 저장/전송, 사용자 원본 덮어쓰기, 기존 슬롯/의상 콘텐츠의 명시적 저장 흐름 변경, 다른 plan020 작업 수정.

## Context Map

- Services/SettingsStore.cs, AudioCaptureService.cs; MainViewModel/ExpressionViewModel/CharacterViewModel
- Views/MainWindow/CharacterWindow와 시작/Windows 합성 QA
- tests/SettingsTests와 새로운 창 배치 수치 검사
- [개발 환경](../../quality/development.md), [품질 규칙](../../quality/rules.md), [개인정보](../../privacy/principles.md)

## Constraints

No Exec Plan, No Work. `codex/plan-022-last-settings` / `.worktree/plan-022-last-settings` 사용.
사용자의 마이크 선택 유지 요청은 기존 ID 미저장 지침보다 우선한다. 선택 endpoint ID는 로컬 설정에만 저장하고 오류 로그/CI 실데이터에는 포함하지 않는다. Start는 사용자 동작을 유지한다.
UI 파일 IO는 worker에서 수행. 저장은 순차·원자적 교체이며 읽기 복원 중 기본값으로 저장을 덮어쓰지 않는다.
QA → 자체 리뷰 → completed 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] 호환 가능한 설정 모델/필드별 정규화·창 화면 보정·마이크 ID 복원
- [x] 설정창/방송창 배치와 상태, 마지막 표정·탭 및 지연 자동 저장/종료 flush
- [ ] Core 왕복·이전/손상·모니터·마이크 회귀, Windows 재실행/자동 저장 fixture
- [ ] 문서/다섯 캐릭터 개인 ZIP 갱신, QA 후 자체 리뷰와 Git 수명 완료

## QA Plan

verify_base.py, verify_app.py, diff --check; 고정 SDK locked restore/format/Release build/Core test.
Windows CI는 합성 장치/임시 저장소로 선택 복원·목록 순서 변화·분리/재연결, 음성 자동 시작 없음, 재실행 크기/위치/최대화/음소거/탭/표정, 창 닫음/종료·자동 저장과 쓰기 실패 복구를 검사한다. 실제 다중 모니터 DPI/물리 마이크는 대상PC 실기로 구분한다. 기존 다섯 PNG는 그대로 개인 ZIP에만 동봉한다.

## Review Plan

QA 후 단일 에이전트 자체 리뷰. 창 최소화/트레이 종료에서도 정상 bounds 보존, 부팅 중 설정 덮어쓰기 방지, 연속 변경/저장/종료 순서, 비유한/없는 모니터·장치 복구, endpoint ID와 음성 로그 경계를 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-12 | 창 두 종류와 마이크·현재 UI 선택을 저장하고 변경 후 자동 저장 | 마지막 사용값 유지 요청을 포괄하고 강제 종료 시 손실을 줄임 |
| 2026-10-12 | 선택 마이크의 endpoint ID만 로컬 settings.json에 저장 | 사용자가 명시 요청했으며 장치 이름/음성/오류 로그 기록은 필요 없음 |
| 2026-10-12 | 미연결 마이크 선호 유지, 다른 선택/명시 Start 후에는 사용한 ID 저장 | USB 재연결 복원과 마지막 실제 사용값을 함께 지원 |
| 2026-10-12 | native 화면 좌표와 WPF DIP 크기 분리·SourceInitialized/Loaded에서 복원 | 음수 모니터/분리/배율 보정과 TwoWay 크기 바인딩 보존 |
| 2026-10-12 | 정상 시작 설정은 worker에서 읽고 VM에 주입 | 창 생성 전에 배치를 복원하면서 UI 동기 파일 IO 방지 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-12 | 지도 | 현재는 오디오/배경/항상 위/캐릭터만 종료 저장. 창 배치·마이크·음소거·표정·탭은 재실행 복원이 없음. |

## Completion Notes

구현/QA 진행 중. 이전 개인 캐릭터 ZIP과 원본 및 다른 worktree를 보존한다.

### 초기 QA

- locked restore/format/Release build 경고0·오류0, Core168/168 통과.
- verify_base/verify_app/diff --check 통과.
- 실제 창 재실행/자동 저장/마이크 factory0/손상 저장 복구 fixture, 일반 EXE restart-settings 추가. 개인 데이터는 임시 fake fixture로 대체.
