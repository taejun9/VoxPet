# plan-029-legacy-adapter Review

## Summary

[완료 계획](../exec_plans/completed/plan-029-legacy-adapter.md)의 Phase 2 Adapter와 필요한 불변 데이터 모델을 QA 이후 같은 에이전트가 자체 리뷰했다. 독립 리뷰는 아니다.

## QA

macOS arm64 / SDK10.0.401. locked restore와 format verify 통과. 솔루션 Release build 경고0·오류0. Core267/267(기존261+신규6), skipped0. verify_base/verify_app와 stage diff 공백 검사를 수행한다. 초기 CharacterDefinition 이름 충돌은 타입 별칭으로 해결하고 서식은 SDK formatter로 수정 후 전체 재검증했다. TRX는 로컬 artifacts/test-results/plan029/core.trx에 보존한다.

## Findings

미해결 결함 없음. 제공 ImageSetTemplate의 source.Files와 신규 Assets가 완전히 일치하고6파츠·얼굴·입 이동·threshold·의상/무기 pose 및 각 프레임 시작/총 시간을 직접 비교했다. null 교체는 숨김으로 보존하며 같은 fire 이름의 여러 프레임은 고유 pose ID로 구별한다. Legacy Eye/Face/Mouth는 head의 독립 자식으로 원래 offset 합산을 보존한다. 원본 배열/사전 수정 후에도 신규 정의는 변하지 않고 read-only collection 수정이 실패한다. 잘못된 원본/신규 참조/경로를 거부한다.

신규 Action/Attachment 정의는 데이터만 추가했다. 기존 ImageSet/ImageSetPlayer와 App/마이크/사용자 설정 경로는 유지한다. Core에 WPF/NAudio 의존성·파일 읽기·JSON parsing tick을 추가하지 않았다.

## Residual Risk

모션/액션/부착물 합성과 App 연결은 이후 단계다. 데이터 비교는 렌더 동등성의 전체 증명을 대신하지 않으므로 Phase 7/9에서 frozen render 회귀를 추가한다. macOS에서 Windows/WASAPI/OBS 실행은 불가능하다. 패키지 ZIP 안전성은 Phase 8에서 검증한다.

## Follow-Ups

Phase 3 MotionEngine 구현, 이후 동일 정의에서 legacy/new render 출력의 경계·프레임·숨김·입 장식 상속을 비교한다.
