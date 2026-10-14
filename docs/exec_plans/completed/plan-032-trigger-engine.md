# plan-032-trigger-engine

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

모든2.0 단계를 연속 완성한다. 이번 계획은 Phase 5 플랫폼 독립 TriggerEngine이다.

## Goal

hotkey/mouse/voice threshold/interval/manual 정의와 이벤트로 action 요청을 생성한다. Windows HandMotionInputs와 ExpressionHotkeys는 기존 콜백을 유지하며 공통 이벤트를 추가로 제공한다. 기존 손동작 설정을 action binding으로 변환한다.

## Non-Goals

Win32 API를 Core로 이동하지 않는다. CHZZK/OBS websocket/플러그인, UI 개편, 오디오 처리 변경은 포함하지 않는다.

## Context Map

- [개발 계약](../../product/voxpet-2-migration.md)
- `src/VoxPet.App/Services/HandMotionInputs.cs`, `ExpressionHotkeys.cs`, `ImageRuntimeInputs.cs`
- `src/VoxPet.Core/Models/HandMotionProfile.cs`, `Actions/ActionDefinition.cs`

## Constraints

Core의 시간은 monotonic caller time, 입력은 지정한 값만 처리하고 키 문자열/좌표/입력 로그를 기록하지 않는다. Windows API는 App에 유지한다.
`codex/plan-032-trigger-engine` / `.worktree/plan-032-trigger-engine`. QA → 자체 리뷰 → 완료 이동/미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] TriggerDefinition/Event/Engine 및 ActionBinding/ActionSettings 불변 계약 구현.
- [x] voice rising edge/지연 interval 한 번/키 반복·해제/pulse/manual와 action 중복 제거 시험.
- [x] Legacy 손동작 설정 변환 및 Windows provider의 공통 이벤트 출력 추가.
- [x] 기존 표정·음소거·마우스 hook/키 callback 보존, 큐 세대 변경 시 낡은 입력 무효화.
- [x] QA 후 리뷰 및 Git 수명 완료.

## QA Plan

SDK10.0.401/macOS arm64: locked restore, format verify, 솔루션 Release build, 전체 Core tests, verify_base/verify_app, stage diff check. Windows 합성 입력은 기존 smoke와 provider 추가 검사로 CI에서 확인한다.

## Review Plan

QA 후 단일 에이전트 자체 리뷰. 입력 정책/플랫폼 경계, 사용 중 key만 보관, voice edge와 interval 지연, legacy 변환과 callback/권한·hook 해제 계약 보존을 검토한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-14 | RegisterHotKey 통지는 pulse로 표시하고 raw key down/up과 구분한다. | MOD_NOREPEAT가 처리한 통지는 key-up 이벤트가 없으므로 held-key 억제에 영구 잠기지 않게 한다. |
| 2026-10-14 | interval은 지연 시 한 번만 요청하고 다음 시각을 현재+간격으로 갱신한다. | 기존 scheduler의 폭주 방지 계약을 보존한다. |
| 2026-10-14 | Core legacy shortcut mapper는 저장된 숫자 코드를 순수 데이터로 변환한다. Windows 등록/감지는 App provider에 남긴다. | 기존 settings를 읽으면서 Win32 API 의존성을 Core에 넣지 않는다. |
| 2026-10-14 | binding은 기존 동작 X/Y/Scale을 보존하고 tick 요청은 trigger 출처를 함께 제공한다. | 후속 Runtime에서 자동 예약과 명시 입력을 구분하고 기존 배치 값을 잃지 않게 한다. |
| 2026-10-14 | 작업 브랜치의 local QA/예비 리뷰 커밋을 먼저 push해 Windows 합성 입력 CI를 검증한 뒤 최종 리뷰·완료 이동·main 병합을 수행한다. | 이번 단계는 App 입력 hook 경계를 바꾸므로 실제 Windows 자동 시험을 main 통합 gate로 삼는다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-14 | 설계 | Phase 4 push·정리 후 전용 계획과 worktree 작성. |
| 2026-10-14 | 검증 | Local Core295/295(기존287+신규8), locked restore/format verify/Release build 경고0·오류0, 하네스 통과. |
| 2026-10-14 | 심사 | Local QA 후 자체 예비 리뷰: 입력 identity와 출처·불변 binding/원본 배치·Core/Windows 경계 확인. 최종 완료 전 Windows 합성 입력 CI를 추가 gate로 실행한다. |

| 2026-10-14 | 검증 | Windows/OBS workflow 성공. 실제 Windows smoke1491/0 및4개 신규 플랫폼 독립 event check 확인. |
| 2026-10-14 | 심사 | 모든 QA 후 최종 자체 리뷰. 남은 제한은 물리 입력 실기/후속 Runtime 연결이며 미해결 Phase 5 결함 없음. |

## Completion Notes

Phase 5 완료. Core295/295(기존287+신규8), locked restore/format verify/Release build 경고0·오류0, 문서/자산 검사와 QA 이후 최종 자체 리뷰 완료.

작업 커밋 b134d97의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38554556302)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38554556380) 성공. 실제 Windows smoke JSON은1491 passed/0 failed이며 추가한4개 공통 이벤트 검사를 직접 확인했다. 배포/서명/시작 복원/desktop 화면 coverage도 통과했다. 증거는 로컬 artifacts/test-results/plan032/에 보존한다.

원본 설정·Rig 저장 형식과 기존 hotkey/mouse 콜백·표정/음소거 키를 유지했다. 이번 adapter는 trigger/X/Y/Scale을 변환하고 기존 Rig 데이터는 후속 Runtime 단계에서 확장한다. Core에는 Win32/WPF/NAudio/파일·JSON tick 처리를 넣지 않았다. 물리 마이크/사용자 GPU·키보드/사용자 OBS 실기는 별도 제한이다.

완료 기록 검증 후 main 병합/push 및 새 작업 원격·로컬 브랜치 정리를 순서대로 수행한다. 전체 목표는 아직 진행 중이며 Phase 6 Attachment System으로 이어간다.
