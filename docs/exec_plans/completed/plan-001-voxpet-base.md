# plan-001-voxpet-base

## Status

completed

## Owner

project_lead / 조율, plan_keeper / 설계

## User Request

첨부된 VoxPet 기획에 `$base`를 적용하여 에이전트가 이어서 개발할 수 있는 저장소 기반을 만든다.

## Goal

Windows 음량 반응형 PNG 캐릭터 앱의 제품·설계·개발 순서·개인정보 원칙·공식 근거·QA/리뷰 절차를 기록하고 실행 가능한 문서 검증기를 제공한다.

## Non-Goals

- 이번 작업에서 C# 프로젝트, 오디오 캡처, 캐릭터 기능을 구현하지 않는다.
- 기존 README를 덮어쓰지 않는다.
- 웹캠, 원격 AI, STT, 녹음 저장, 추적 기능을 추가하지 않는다.

## Context Map

- 기존 저장소: README.md만 존재, 초기 커밋 `016c751`, main 작업 트리 깨끗함.
- 입력: 사용자가 첨부한 VoxPet 기획. 원문 개인 파일 경로는 제품 문서에 복제하지 않는다.
- 새 문서: AGENTS.md, docs/product/, docs/architecture/, docs/quality/, docs/privacy/, docs/references/.
- 검증: harness/scripts/verify_base.py, 계획·회의·리뷰 템플릿.

## Constraints

- No Exec Plan, No Work. 계획 변경은 Decision Log에 기록한다.
- `codex/plan-001-voxpet-base`, `.worktree/plan-001-voxpet-base`에서 작업한다.
- QA → 리뷰 → 완료 계획 이동/리뷰 미러 → main 병합 → main push → 브랜치 삭제 → worktree 제거.
- `docs/plan` 금지. root Markdown은 README.md와 AGENTS.md만 허용.
- 현재 호스트는 macOS이고 dotnet CLI가 없어 Windows 앱 검증 결과를 주장하지 않는다.

## Implementation Plan

- [x] 번들 스캐폴드 실행, 기존 README 보존.
- [x] 한국어 팀 지도와 제품·설계·로드맵·개인정보 문서 작성.
- [x] 공식 문서에서 스택 호환성 확인 및 미확정 항목 기록.
- [x] Python 표준 라이브러리 기반 문서 검증기 작성.
- [x] QA 후 별도 관점으로 자체 리뷰, 완료 기록 작성.

Git 전달은 문서 완료 후 하네스 절차대로 수행하며 실제 결과는 최종 사용자 보고에서 확인한다.

## QA Plan

- `python3 harness/scripts/verify_base.py`: 구조, 링크, 계획 상태/이름, 리뷰 미러, 공식 출처 필드 검사.
- `git diff --check`, 기존 README 바이트 비교, 전체 변경 목록 확인.
- 앱 빌드/테스트는 앱 소스가 없는 기반 작업이므로 적용하지 않는다.

## Review Plan

QA 완료 뒤 첨부 기획과 문서를 대조한다. MVP/후속 기능 경계, 수식·시간 기반 smoothing, UI 스레드/리소스 해제, 미검증 Windows·OBS 항목을 확인한다. 서브에이전트는 실행하지 않으며 단일 에이전트의 QA 후 자체 리뷰임을 기록한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-05 | 앱 구현 대신 저장소 기반 작업으로 범위 확정 | 사용자의 명시 요청은 `$base` 적용이다. |
| 2026-10-05 | README 보존, 시작 안내는 AGENTS와 제품 문서에 배치 | 스킬의 기존 파일 보존 규칙. |
| 2026-10-05 | .NET 8 기획 유지, 수명과 NAudio 2.x 제약 기록 | 최신 공식 자료의 NAudio 3는 net9.0 이상을 요구하며 .NET 8 지원 종료는 2026-11-10이다. |
| 2026-10-05 | Attack 40ms / Release 140ms를 기준으로 선택 | 첨부의 후반 오디오 집중 기획이 초기 예시 50/150ms를 구체화한다. |
| 2026-10-05 | sensitivity는 gate 이후 정규화 레벨 배율, body 상한은 제안 6 DIP | 모호한 gain 의미와 픽셀 단위를 명시하여 후속 구현의 계약으로 사용. |
| 2026-10-05 | worktree를 detach한 후 branch -d, 그 다음 worktree 제거 | Git은 checkout된 브랜치를 삭제할 수 없으므로 스킬의 삭제 순서를 지키는 안전한 절차. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-05 | 조율 | 기획/스킬/저장소 확인, origin fetch 후 worktree 생성, 구현 전에 계획 작성. |
| 2026-10-05 | 제작 | 번들 생성기를 적용하고 README와 사전 작성 계획을 보존, 한국어 설계/템플릿/검증기 작성. |
| 2026-10-05 | 검증 | 문서 QA·오류 주입 검사·README 보존 확인. symlink 경로 수정 후 통과. |
| 2026-10-05 | 심사 | QA 후 자체 리뷰 완료. 수명 관리 문구 정정, 앱 실행 미검증 경계 확인. |

## Completion Notes

20개 기반 파일을 생성하고 기존 README를 바이트 단위로 보존했다. 앱 코드는 추가하지 않았다.

- QA: 문서 검증 PASS, staging diff 공백 검사 PASS, README 보존 PASS.
- 임시 복제 검증: 금지 경로·깨진 링크·잘못된 계획 상태·리뷰 누락 탐지 PASS.
- QA 중 macOS 임시 경로 symlink에서 발생한 경로 판정 문제를 root.resolve()로 수정 후 재검증.
- QA 후 자체 리뷰: 기획과 MVP 경계, .NET/NAudio 호환성, DSP/스레드/수명/개인정보 계약 확인.
- 리뷰에서 capture보다 endpoint를 먼저 해제할 수 있는 문구를 수정. 기능 구현 전에도 종료 순서가 명확해야 한다.
- 완료 리뷰 미러: docs/reviews/plan-001-voxpet-base-review.md.
- Windows 앱/OBS 실행, 패키지 pin, OS/SDK/IDE 버전 조합, PNG 자산 라이선스는 후속 작업이다.
