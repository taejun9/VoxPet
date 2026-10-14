# plan-032-trigger-engine Review

## Summary

[완료 계획](../exec_plans/completed/plan-032-trigger-engine.md)의 플랫폼 독립 TriggerEngine/Definition/Event/Request, ActionBinding/Settings, legacy 설정 변환과 Windows provider 이벤트 경계를 모든 QA 이후 단일 에이전트가 자체 리뷰했다.

## QA

macOS arm64/SDK10.0.401: locked restore, format verify, Release 솔루션 build 경고0·오류0, Core295/295(기존287+신규8), skipped0. verify_base/verify_app 및 stage diff 공백 검사 수행.

커밋 b134d97의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38554556302), [OBS 회귀](https://github.com/taejun9/VoxPet/actions/runs/38554556380) 성공. Windows workflow의 실제 WPF smoke, 배포/서명/일반 시작 복원·desktop coverage 통과. 내려받은 smoke JSON은1491 passed/0 failed이며 expression_platform_neutral_hotkey_pulse, motion_platform_neutral_hotkey_pulse, motion_platform_neutral_mouse_pulse, motion_platform_neutral_dispose_stops_events 네 항목을 직접 확인했다. 로컬 증거는 artifacts/test-results/plan032/에 보존한다.

## Findings

미해결 Phase 5 결함 없음. hotkey는 modifier 일치와 raw down/up 억제, RegisterHotKey pulse의 재입력을 모두 지원한다. voice threshold는 rising edge로 요청하고 mute/non-finite input이0으로 정착한 뒤 다시 재무장한다. 지연 interval은 현재+간격으로 coalesce한다. mouse/manual/disabled/중복 action 제거와 결정적 순서를 시험했다. manual ID는 case-sensitive이며 hotkey key identity만 정규화한다.

binding은 입력 collection을 복사하고 기존 Enabled/Mode/Interval/Shortcut/X/Y/Scale을 보존한다. Rigs와 기존 설정 파일은 그대로 유지한다. TriggerRequest는 현재 tick의 출처를 제공해 후속 Runtime에서 자동 예약과 명시 입력을 구분할 수 있게 한다. 키 상태는 설정된 입력만 일시 보관하고 입력 기록/좌표/문자열 수집·전송은 추가하지 않았다.

Windows API는 App에 남기고 HandMotionInputs와 ExpressionHotkeys가 공통 pulse를 추가 제공한다. 기존 콜백/등록·충돌·dispose/표정·음소거 키가 실제 Windows 회귀에서 통과했다. mouse Dispatcher queue는 설정 변경·dispose의 generation을 확인해 오래된 입력이 새 설정에 적용되지 않는다.

## Residual Risk

Windows 시험은 공용/합성 자료의 일회용 runner다. 물리 마이크·키보드/사용자 GPU·OBS 실기는 아니다. generic package의 trigger 설정과 CharacterRuntime 연결은 Phase 7/9에서 수행한다. 다중 always/interval의 공정 재생은 기존 scheduler adapter가 계속 담당하며 후속 Runtime 통합에서도 이 예약 규칙을 보존해야 한다.

## Follow-Ups

Phase 6 Anchor 상속·category conflict·Costume adapter와 입에 붙은 콧수염 이동 시험. Phase 7 trigger 요청 출처/자동 예약을 중앙 Runtime에 결합한다.
