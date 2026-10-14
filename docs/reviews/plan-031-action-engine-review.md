# plan-031-action-engine Review

## Summary

[완료 계획](../exec_plans/completed/plan-031-action-engine.md)의 Generic ActionEngine/Timeline/State와 손동작 adapter를 QA 후 단일 에이전트가 자체 리뷰했다.

## QA

macOS arm64/SDK10.0.401. locked restore, format verify, 솔루션 Release build 경고0·오류0, Core287/287(기존277+신규10), skipped0, verify_base/verify_app 및 stage diff 공백 검사. TRX는 artifacts/test-results/plan031/core.trx에 보존한다. 초기 Dictionary 타입 추론 컴파일 오류는 명시적 생성 타입으로 수정하고 전체 재검증했다. Phase 2 Windows/OBS 및 Phase 3 OBS CI 성공을 확인했으며 Phase 3 Windows는 당시 진행 중이었다.

## Findings

미해결 결함 없음. pose/transform/visibility/sprite/effect/expression/attachment 실행과 Sound 예약 무동작을 확인했다. 효과는 end 시각에 만료한다. 높은 우선순위가 낮은 요청을 차단하고 interruptible=false는 System100만 중단한다. 같은 priority 입력의 교체, 같은 ID 재시작/중복 거부, once idle 복귀/hold 최종 상태, loop wrap 및 double.MaxValue 지연 시각을 시험했다.

재평가 범위는 현재 cycle의 timeline으로 제한되며 지연된 여러 loop를 폭주 실행하지 않는다. 이전 snapshot은 수정되지 않는다. LegacyImageSet 변환 프레임과 기존 ImageSetPlayer의 여러 boundary 출력을 같은 generic engine에서 비교했다. 기존 HandMotionTests 전부 통과하며 간격 한 번 예약·공정 always 선택·preview·중복 입력·설정 변경·stop/cancel API를 보존했다. adapter는 시간을 기존처럼 clamp하고 신규 engine은 monotonic 입력을 검증한다.

Core tick에는 파일/JSON/NAudio/WPF/Win32/실시간 시계·음원 재생을 넣지 않았다. 종료 시각 pose는 허용하고 동작 최소1ms로 seconds 변환 underflow를 차단한다.

## Residual Risk

ActionState의 priority/transform과 MotionEngine의 합성은 Phase 7에서 최종 검증한다. 현재 App 손동작은 compatibility wrapper로 새 엔진을 쓰며 캐릭터 정의의 generic action 렌더 연결은 후속 단계다. Windows/WASAPI/실제 사용자 OBS는 로컬 macOS에서 미실행이다.

## Follow-Ups

Phase 5 플랫폼 독립 TriggerEngine과 Windows 입력 provider의 이벤트 경계를 구현한다. Phase 7에서 idle/voice/action/idle와 무기·얼굴·입 장식 우선순위를 렌더 결과로 검증한다.
