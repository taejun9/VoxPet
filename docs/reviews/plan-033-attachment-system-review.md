# plan-033-attachment-system Review

## Summary

[완료 계획](../exec_plans/completed/plan-033-attachment-system.md)의 Rig/affine·부착물 상속/충돌·기존 의상 읽기 경계를 QA 후 단일 에이전트가 자체 리뷰했다.

## QA

macOS arm64/SDK10.0.401. locked restore, format verify, 솔루션 Release build 경고0·오류0, Core309/309(기존295+신규14), skipped0, verify_base/verify_app 및 stage diff check. TRX는 artifacts/test-results/plan033/core.trx에 보존한다. 이번 단계는 App 호출 경로를 바꾸지 않으며 실제 pixel/WPF 연결은 Phase 7/9의 통합 gate에서 검증한다.

## Findings

미해결 Phase 6 결함 없음. 부모 순서는 iterative 검증으로 cycle/누락/중복을 거부한다. 비균일 scale+rotation의 affine 합성과 inverse, 반전, 큰 유한 rotation과 숫자 overflow 경계를 시험했다. 기존 CharacterDefinition은 같은 RigDefinition 검증을 재사용한다.

콧수염 mouth 이동은 head90°회전과 mouth10px 이동에서 실제 점 위치로 증명했고 안경은 mouth를 제외한다. position/rotation/scale/mouth 선택을 독립 시험했다. Follow50%는 local rig 변환을 먼저 가중해 기존 head rotation*Follow와 mouth 순서를 보존한다. 반전의 중간 영향도는 방향 전환으로0배율을 피하고 작은 endpoint scale을 그대로 유지한다. custom Anchor와 표준14종을 지원한다.

onePiece와 top/bottom, 동일 singular 분류·명시 exclusiveGroup 충돌을 처리한다. faceDecoration/earrings/effect는 복수 선택을 허용한다. 저장 선택 복구는 기존 wardrobe처럼 앞 항목 우선이며 명시 착용은 새 선택 우선이다. action pose 이미지/숨김 의존/무기 tag·효과 선택은 Core가 결정한다.

Legacy adapter/store는 기존 logical size·placement·follow·뒤/앞·garment 정보를 변환한다. 원본 wardrobe.json과 이미지 bytes의 재읽기·손상 복구·원본 보존 시험을 통과했다. RootTransform/Bounce의 기존 Viewbox 바깥 DIP 계약은 후속 Runtime에 보존해야 한다. animation 계산에는 파일/JSON/WPF/Win32/NAudio 접근이 없다.

## Residual Risk

garment의 연결 격자는 현재 배치 metadata로 보존하며 pixel 출력 통합은 Phase 7/9에서 증명한다. AttachmentStore는 기존 파일 읽기 facade이고 native 저장은 이후 공통 package codec으로 확장한다. 실제 물리 마이크·입력/사용자 GPU·OBS 실기는 기존 제한으로 남는다.

## Follow-Ups

Phase 7 CharacterRuntime/RenderComposer/MouthController와 idle→voice→action→idle, v1 frozen composition 동등성, Core-owned affine output을 구현한다. 이후 package codec/App renderer/editor를 연결한다.
