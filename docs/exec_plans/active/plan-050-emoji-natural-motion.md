# plan-050-emoji-natural-motion

## Status

active

## Owner

project_lead / plan_keeper

## User Request

동작들의 부드러움이 이모티콘처럼 자연스럽게 나오도록 기능을 수정하고, 늘보군과 펠리 각각 자연스럽게 작업한다.
의상도 각각 캐릭터에 맞게 수정한다.
윈도우 테스트용 파일과 미리보기를 만든다.
모든 UI/UX도 사용자 편의에 맞게 정리정돈한다.

## Goal

1. 이모티콘/이모지 수준의 부드럽고 자연스러운 캐릭터 동작 (호흡, 바운스, 머리 흔들림, 눈 깜빡임, 입 전환)
2. 늘보군과 펠리 각각의 캐릭터 특성에 맞는 개별 모션 프로파일과 의상 기본 설정
3. 윈도우 테스트 빌드와 HTML 미리보기 생성
4. UI/UX 일관성 및 사용자 편의 개선

## Non-Goals

- Live2D/Spine 전환
- 새 캐릭터 추가
- OBS 실기 재검증
- 마이크/GPU/DPI 물리 검증

## Context Map

- [앱 설계](../../architecture/application.md), [제품](../../product/product.md), [QA](../../quality/rules.md), [개발 환경](../../quality/development.md), [하네스](../../architecture/harness.md)
- [이전 계획](../completed/plan-049-avatar-performance.md)
- MotionEngine, VoiceMotion, BlinkMotion, IdleMotion, MotionProfile, AvatarPerformance, RigMotionSolver
- CharacterRuntime, LegacySpriteSheetRuntime, CharacterAnimator, ConnectedMotion, ExpressionMotion
- CharacterViewModel, MainViewModel, MotionSurface, RenderFrameDrawing, CompanionParts
- CharacterCatalog (neulbo/pelly), CostumeProfile, CompanionEquipment

## Constraints

시작 main 8f65905. 전용 codex/plan-050-emoji-natural-motion/.worktree/plan-050-emoji-natural-motion을 사용한다. 기존 Core/WPF/portable 테스트와 공개/개인 자산 분리를 유지한다. QA→자체 리뷰→완료 이동/리뷰 미러→main ff/push→branch -d→worktree 제거를 수행한다.

## Implementation Plan

- [ ] 1. 이모지 스타일 자연 모션: IdleMotion에 부드러운 호흡 곡선(smoothstep 기반), VoiceMotion에 탄성 바운스(underdamped spring), BlinkMotion에 이모지 스타일 반감기/이중 깜빡임, ExpressionMotion에 부드러운 전환 개선
- [ ] 2. 캐릭터별 모션 프로파일: 늘보군(느린 호흡, 부드러운 바운스, 느긋한 머리 흔들림), 펠리(활발한 호흡, 빠른 바운스, 부리 강조 표현) MotionProfile에 캐릭터별 기본 매개변수 추가
- [ ] 3. 캐릭터별 의상 기본 설정: CharacterCatalog에 기본 의상 프리셋 연결, CostumeProfile에 캐릭터 적합성 표시
- [ ] 4. UI/UX 정리정돈: 설정창 레이아웃 일관성, 버튼/라벨/슬라이더 그룹화, 상태 메시지 개선, 미리보기 영역 비율 최적화
- [ ] 5. HTML 미리보기 생성기: 현재 캐릭터의 모션 상태를 보여주는 HTML 테스트 페이지
- [ ] 6. 윈도우 테스트 빌드: verify_base.py 통과, 앱 빌드 확인
- [ ] 7. 전체 QA: Core 테스트, WPF 테스트, lint, git diff --check

## QA Plan

python3 harness/scripts/verify_base.py. dotnet format/analyzer/Release. Core 전체 테스트. WPF 실제 표시 검사. git diff --check. 새 모션 매개변수는 유한값/상한/경계/전환/시작/정지를 검사한다. HTML 미리보기는 두 캐릭터의 기본/반응 상태를 표시한다.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰로 요구사항·모션 자연스러움·캐릭터 특성·UI 일관성·호환·스레드/수명을 점검한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-19 | 이모지 스타일 모션을 기존 MotionEngine 프레임워크 안에서 개선한다. | 기존 구조를 유지하면서 매개변수 조정과 커브 개선으로 자연스러운 동작을 달성한다. |
| 2026-10-19 | 캐릭터별 모션 프로파일을 CompanionActing enum과 MotionProfile에 통합한다. | 기존 CompanionActing.Neulbo/Pelly 구분을 활용한다. |
| 2026-10-19 | HTML 미리보기를 harness/preview/ 폴더에 만든다. | 윈도우 없이도 모션 상태를 확인할 수 있다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-19 | 설계 | 코드베이스 분석을 완료하고 실행 계획을 작성했다. |

## Completion Notes

검증 결과, 미해결 제한, 후속 조치. 완료 시 Status를 completed로 변경.
