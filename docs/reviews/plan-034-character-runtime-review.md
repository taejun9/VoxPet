# plan-034-character-runtime Review

## Summary

[완료 계획](../exec_plans/completed/plan-034-character-runtime.md)의 중앙 Runtime과 pure compositor/입 controller를 QA 후 단일 에이전트가 자체 리뷰했다.

## QA

macOS arm64/SDK10.0.401. locked restore, format verify, 솔루션 Release build 경고0·오류0, Core317/317(기존309+신규8), skipped0, verify_base/verify_app 및 stage diff check. TRX는 artifacts/test-results/plan034/core.trx에 보존한다. Phase 6 Windows/OBS CI 성공을 확인했다. 이번 단계에서 기존 App 호출 경로는 바꾸지 않았다.

## Findings

미해결 Phase 7 결함 없음. input snapshot은 복사하고 numeric/time 경계를 검증한다. motion→mouth frame/rig, trigger→action arbitration/자동 FIFO, attachment selection 및 최종 renderer layer를 Core에서 계산한다. 높은 priority의 target transform은 낮은 motion을 대체한다. 자동 예약은 action별1개이고1000회 manual 선점에도 크기가 bounded이다.

1~16 mouth count를1001음량 표본씩 검증하고 midpoint는 AwayFromZero로 통일했다. v1 legacy3/8 threshold를 보존한다. expression fallback 상태와 실제 출력이 일치한다. effect duration·asset size, 임시 action expression/attachment의 idle 복원, hidden head/arm의 얼굴/소매 제거와 deterministic replay를 확인했다.

기존 제작 틀을 새로운 Runtime과 ImageSetPlayer에서 같은 frozen 시각/음량/blink/표정으로 실행해 자산·순서·X/Y가 일치하는지 직접 비교했다. v1 face z-index와 Viewbox 밖 bounce는 명시 definition policy로 유지한다. RenderLayer/Frame의 기존 생성 API는 계속 지원하고 affine/logical size/viewport/mesh 출력을 추가했다.

Core tick은 WPF/NAudio/Win32/파일/JSON/wall-clock에 접근하지 않는다. Runtime은 원본 형식을 알지 않으며 definitions와 준비된 geometry/asset dimensions만 받는다.

## Residual Risk

App integration과 기존 sprite-sheet/garment의 mesh provider·pixel 동등성은 Phase 9 gate다. Core는 geometry 계약을 제공하고 필요한 provider가 없으면 active garment 출력을 거부한다. 실제 Windows/마이크/사용자 OBS 실기는 자동 합성 회귀와 구분한다. native inventory 저장과 패키지 파일 처리는 다음 codec 단계다.

## Follow-Ups

Phase 8 bounded ZIP+JSON+PNG loader/validator/exporter와 malformed/traversal/roundtrip tests. Phase 9는 동일 Runtime을 미리보기/방송에 연결하고 기존 mesh/sprite/표정·설정 호환을 검증한다.
