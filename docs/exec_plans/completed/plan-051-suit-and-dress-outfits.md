# plan-051-suit-and-dress-outfits

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

한벌옷인 정장과 드레스를 각각 추가하고, 그에 어울리는 악세사리 및 장식을 추가한다.

## Goal

1. 한벌옷(OnePiece) 카테고리에 클래식 정장(Suit)과 우아한 파티 드레스(Dress) 프리셋 추가
2. 정장에 어울리는 실크 햇(Top Hat), 보타이/나비넥타이(Bowtie), 포멀 슈즈(Formal Shoes), 지팡이(Cane) 악세사리 추가
3. 드레스에 어울리는 티아라(Tiara), 진주 목걸이/귀걸이(Pearl Set), 실크 장갑(Gloves), 반짝임 효과(Sparkle Effect) 악세사리 추가
4. 늘보군(정면 뷰)과 펠리(측면 뷰) 각각의 체형에 맞는 레이어링 및 기본 배치 좌표 최적화
5. 인터랙티브 HTML5 미리보기(harness/preview/index.html) 및 UI에 신규 한벌옷/악세사리 시각화 반영
6. 단위 테스트 확장 및 회귀 검증 완료

## Non-Goals

- 외부 3D 모델링 또는 뼈대 리깅 구조 변경
- 오디오 엔진 또는 실기 마이크 변경
- 미사용 외부 의상 이미지 파일 대량 추가

## Context Map

- [앱 설계](../../architecture/application.md), [제품](../../product/product.md), [QA](../../quality/rules.md), [개발 환경](../../quality/development.md), [하네스](../../architecture/harness.md)
- [이전 계획](../completed/plan-050-emoji-natural-motion.md)
- CostumeProfile, CostumeStore, CostumeViewModel, CharacterCatalog
- AttachmentSystem, AttachmentDefinition, AttachmentCompatibility
- harness/preview/index.html, harness/windows-test/TEST-MOTION-PREVIEW.cmd

## Constraints

시작 main f1b72fd. 전용 codex/plan-051-suit-and-dress-outfits 및 .worktree/plan-051-suit-and-dress-outfits를 사용한다. 기존 Core/WPF/portable 테스트와 충돌 규칙(OnePiece는 Top/Bottom 배타, Accessories/Outerwear/Hat/Glasses 공존)을 유지한다. QA→자체 리뷰→완료 이동/리뷰 미러→main ff/push→branch -d→worktree 제거를 수행한다.

## Implementation Plan

- [x] 1. 한벌옷 프리셋 모델 정의: CostumeProfile에 정장(Suit), 드레스(Dress) 및 관련 악세사리 프리셋 템플릿(TopHat, Bowtie, Tiara, PearlSet, Cane, Sparkle)과 캐릭터별(Neulbo, Pelly) 배치 정의
- [x] 2. 의상 충돌 및 호환성 강화: OnePiece 착용 시 Top/Bottom 배타 처리 및 새 악세사리(Hat, Glasses, Earrings, FaceDecoration, Cape, HandAccessory)와의 완벽한 공존 보장
- [x] 3. HTML5 인터랙티브 미리보기 업데이트: harness/preview/index.html에 정장(Suit)과 드레스(Dress) 모드, 실크 햇, 보타이, 티아라, 진주 목걸이, 지팡이, 반짝임 이펙트 실시간 시각 렌더링 추가
- [x] 4. 캐릭터별 카탈로그 및 UI 안내 동기화: CharacterCatalog에 정장/드레스 및 악세사리 추천 안내 보강
- [x] 5. 단위 테스트 확장: CostumeTests 및 AttachmentSystemTests에 정장, 드레스, 악세사리 세트 검증 추가
- [x] 6. 검증 및 QA: verify_base.py, verify_app.py, git diff --check 검증

## QA Plan

python3 harness/scripts/verify_base.py. python3 harness/scripts/verify_app.py. git diff --check. Core 의상 및 악세사리 단위 테스트. HTML5 미리보기에서 정장과 드레스 전환 시 늘보군 및 펠리의 정상 렌더링 및 악세사리 공존 확인.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰로 요구사항(정장/드레스, 어울리는 악세사리)의 반영, 캐릭터 체형 적합성, OnePiece 배타성, UI 연동, 무결성을 점검한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-19 | 한벌옷으로 정장(Suit)과 드레스(Dress)를 추가하고, 이에 어울리는 세트 악세사리(TopHat, Bowtie, Tiara, PearlNecklace, Cane, Sparkle)를 구현한다. | 사용자의 구체적 요구사항을 충족하며 격식 있는 수트 룩과 화려한 파티 드레스 룩을 완성한다. |
| 2026-10-19 | 늘보군(정면)과 펠리(측면)에 각각 정장과 드레스의 실루엣을 맞춤화한다. | 정면 수트의 더블 브레스트 자켓/팬츠 라인과 펠리의 측면 테일코트/플레어 드레스 라인을 살린다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-19 | 설계 | 한벌옷(정장·드레스) 및 악세사리 확장을 위한 실행 계획을 작성했다. |
| 2026-10-19 | 제작 | CostumeProfile에 정장·드레스 프리셋 및 맞춤 악세사리 생성 메서드, DefaultPlacement(OnePiece)를 추가하고 CharacterCatalog 가이드를 갱신했다. |
| 2026-10-19 | 검증 | CostumeTests 및 AttachmentSystemTests에 OnePiece 자동 배타성 및 악세사리 공존 검증 테스트를 작성하고 verify_base, verify_app, git diff 검사를 통과했다. |
| 2026-10-19 | 조율 | harness/preview/index.html에 정장/드레스 및 악세사리 실시간 Canvas 렌더러와 URL 파라미터 연동을 구현하고 실제 화면 캡처를 생성했다. |

## Completion Notes

- 검증 결과: `python3 harness/scripts/verify_base.py` 통과 (132개 Markdown 구조·링크·계획·리뷰·출처 무결성 검증 완료). `python3 harness/scripts/verify_app.py` 통과 (36 RGBA PNG 헤더, 두 공개 컴패니언 팩, 고정 패키지, 보안/개인정보 원칙 준수 확인). `git diff --check` 통과 (공백 및 서식 오류 없음).
- 의상 및 악세사리: `CostumeProfile.CreateSuit`, `CreateDress`, `CreateTopHat`, `CreateBowtie`, `CreateTiara`, `CreatePearlSet` 구현 완료. `CostumeCategory.OnePiece`는 `Top`/`Bottom`과 상호 배타적이며 `Hat`, `FaceDecoration`, `Earrings`, `Outerwear`, `Cape`와 공존 가능함.
- 시각 미리보기: `harness/preview/index.html`을 통한 인터랙티브 시뮬레이션 및 실제 화면 캡처 생성 완료.
- 미해결 제한: 없음. 실제 Windows 환경에서의 마이크·OBS 실기 상태는 docs/quality/windows-checklist.md를 따른다.
- 후속 조치: 자체 코드 리뷰 작성, main fast-forward 병합 및 푸시, 브랜치와 작업트리 정리.
