# plan-045-character-samples

## Status

active

## Owner

project_lead / harness_builder

## User Request

별도 샘플 캐릭터가 아니라 늘보군과 펠리에게 모든 의상·동작 샘플을 제공하고, 그림을 더 예쁘고 자연스럽게 개선한다.

## Goal

늘보군/펠리 선택을 유지하면서17의상/14분류·9동작·헬리콥터 모자/날개 애니메이션·3헤어를 바로 시험할 수 있게 한다. 체형/정면·측면을 고려한 고품질 새 그림을 넣고 Windows 시험 ZIP을 갱신한다.

## Non-Goals

개인 원본 PNG/표정/기존 설정/ZIP/plan024 작업트리를 덮어쓰거나 삭제하지 않는다. 개인 캐릭터를 공개 Git/CI에 올리지 않는다. 음성 저장/전송·자동 마이크 시작·무단 서브에이전트를 추가하지 않는다.

## Context Map

- [샘플 사용](../../guides/studio-samples.md), [캐릭터 목록](../../product/character-library.md)
- StudioShellViewModel/CharacterViewModel/LegacySpriteSheetRuntime/CharacterRig
- generate_studio_samples.py/StudioSampleTests/UnifiedStudioQa/AnimatedAccessoriesQa
- [개발 환경](../../quality/development.md), [QA 규칙](../../quality/rules.md)

## Constraints

main20c5006과origin0·0/깨끗함 확인 후 전용 codex/plan-045-character-samples와 .worktree/plan-045-character-samples 사용. 단일 에이전트. imagegen 내장 도구로 그림 제작, 프로젝트 자산/프롬프트 보존. 개인 그림은 로컬 검토·개인 ZIP에만 사용한다.

## Implementation Plan

- [x] 두 캐릭터 그림/앵커/기존 런타임 조사, 샘플 적용 설계 기록.
- [x] 자연스러운 새 의상·부착물·동작 그림 제작 및 투명도/체형 정렬 검토.
- [x] 두 캐릭터에 샘플 직접 연결, 선택/표정·입/저장·방송 공유와 기존 오류 수정 유지.
- [ ] Core/로컬 lint/build/Windows/OBS/ZIP 전체 QA 및 실제 화면 시각 검토.
- [ ] 자체 리뷰/문서/새 개인 ZIP/main ff push/정상 정리.

## QA Plan

verify_base/verify_app/compileall/diff, locked restore/format/analyzer/Release 경고0·오류0/Core 전체. Windows에서 늘보군과 펠리 각각 선택 유지·17의상/9동작·모자/날개/헤어 조합·해제/idle복귀·표정/입·창 공유·전환/재시작·파일 실패 보존을 공개 합성 fixture로 검사한다. 기존2090개 WPF 검사와1784개 portable 검사 범위를 유지하거나 새 UX의 대응 경로를 명시한다. 새 Windows 일반/서명 시작·화면 coverage·portable/OBS를 수행한다. 개인 원본은 로컬에서 byte hash와 그림 합성 정렬을 확인하며 CI에는 보내지 않는다. 최종 ZIP의 공용 파일/개인 PNG bytes·PE/CRC/SHA256을 검증한다.

## Review Plan

QA 후 동일 에이전트 자체 리뷰. 두 캐릭터에 직접 제공한다는 요구, 체형/레이어/그림 품질, 기존 표정/음성/입력/파일 경계와 원본 보존, CI와 전달 ZIP 일치, 실기 한계를 검토한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-17 | 별도 인형으로 교체하는 현재 UX를 바꾸고 선택한 늘보군/펠리의 원래 얼굴과 몸 위에 샘플을 적용한다. | 사용자가 샘플의 대상과 그림 품질을 명확히 정정했다. |
| 2026-10-17 | 공개 의상 패키지를 두 체형으로 각각 배치하고 기존 full-sprite 런타임의 중앙 ActionEngine·최종 RenderFrame에 연결한다. | 원본 표정/입8·3단계와 기존 손동작·의상 연결을 유지하며 별도 캐릭터로 교체하지 않는다. 공유 scheduler는 새9동작 ID를 enum으로 오해하지 않게 한다. |
| 2026-10-17 | 의상·헤어·모자/날개·주요 소품은 내장 imagegen, 작은 장식은 편집 가능한 곡선/음영 자산으로 다듬는다. 원본 생성물과 프롬프트를 보존한다. | 단순 도형의 시험용 그림을 개선하고 체형별 배치·반복 프레임 등록을 재현 가능하게 한다. 개인 캐릭터를 생성 도구/Git/CI에 전달하지 않고 공개 의상만 새로 만든다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-17 | 설계 | 원격/main 동기화·기존 작업 보존 확인, 전용 작업트리와 계획 작성. imagegen 스킬 읽음. |

| 2026-10-17 | 제작 | 13종 주요 원본과 날개 재시안을 내장 imagegen으로 제작해 보존했다. 로컬 개인 합성에서 장발의 눈 근처 침범/시트 여백의 이웃 조각을 발견해 배치와 날개 시안을 보완했다. 작은 장식은 곡선/음영으로 다듬었다. |
| 2026-10-17 | 검증 | 기존 임시 SDK의 tasks 파일 누락을 확인해 공식10.0.401을 새 임시 경로에 다운로드하고 공식 SHA512와 대조해 복구했다. 초기 의존성 복원 전 빌드/새 테스트의 잘못된 Export 오버로드를 수정 후 Release 경고0/오류0와 Core401/401 통과. 최종 lint/Windows/OBS/ZIP QA를 준비한다. |

| 2026-10-17 | 검증 | fff7112 첫 Windows38933487273/OBS38933486988 전체 통과. 이후 개인 시각 검토에서 장발 얼굴 가림과 앞쪽 날개의 각진 경계를 찾아 원본 새 시안/곡선 합성으로 보완했고, 얼굴 안전 영역·9개 고유 자세 회귀를 추가했다. 최종 커밋으로 전체를 다시 시험한다. |

| 2026-10-17 | 제작 | 통합 경로를 점검해 기존 추가 의상이 새 샘플 위를 덮는 분류 충돌을 처리했다. 샘플 착용 중 같은 분류만 잠시 숨기며 저장된 착용 플래그/원본은 그대로 유지하고 해제하면 복원한다. 두 캐릭터의 Core 회귀를 추가했다. |

## Completion Notes

QA/리뷰 후 기록한다.
