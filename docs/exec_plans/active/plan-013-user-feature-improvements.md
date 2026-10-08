# plan-013-user-feature-improvements

## Status

active

## Owner

project_lead / plan_keeper

## User Request

실제 사용자에게 필요한 기능을 리서치해 목록을 만들고 구현 여부를 확인하여 추가·수정한 뒤 프로젝트 전체를 테스트한다.

## Goal

공식 제품 문서와 사용자 요청을 근거로 기능/격차/우선순위 목록을 남기고 방송 사용성의 주요 누락을 개선한다. 전체 자동 QA와 Windows WPF/OBS 회귀 결과를 확인한다.

## Non-Goals

음성 저장·원격 전송·웹캠·추적, 개인 PNG 전송, 실물 마이크가 없는 환경의 실기 합격 주장, 사용자 요청 없는 서브에이전트 실행.

## Context Map

- docs/product/, docs/guides/, docs/quality/: 제품 요구·사용 안내·검증 상태.
- src/VoxPet.App/, src/VoxPet.Core/, tests/: UI·설정·오디오·자동 테스트.
- harness/scripts/, .github/workflows/: 정적 검사와 Windows/OBS QA.

## Constraints

No Exec Plan, No Work. codex/plan-013-user-feature-improvements 및 .worktree/plan-013-user-feature-improvements 사용. 기존 파일은 필요한 부분만 수정하고 개인 자료는 보존한다. QA → 자체 리뷰 → 완료 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거.

## Implementation Plan

- [x] 공식 제품 문서와 공개 사용자 요청 조사, 기능 목록과 구현 여부·우선순위 기록.
- [x] 범위 결정과 Decision Log 기록 후 핵심 누락 구현·사용 안내 동기화.
- [ ] 전체 자동 QA 및 Windows WPF/OBS 합성 회귀 실행·증거 확인.
- [ ] QA 이후 자체 리뷰, 완료 계획/리뷰 미러와 Git 통합·정리.

## QA Plan

문서/자산 검증, locked restore, format/analyzer, Release build, 전체 Core test, diff check를 실행한다. Windows CI의 qa.ps1 -Publish -Smoke 및 OBS 합성 캡처를 실행해 UI/설정/수명 회귀 증거를 확인한다. 새 동작에 의미 있는 Core 테스트와 WPF smoke를 추가한다. 물리 마이크·권한·DPI·키보드·사용자 GPU·장시간 실기는 별도 제한으로 기록한다.

## Review Plan

QA 이후 동일 에이전트가 요구사항/수명/스레드/설정 복구/개인정보/회귀/문서를 자체 리뷰한다. 요청 없이 worker를 만들지 않는다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-08 | Ctrl+Shift+M 전역 입 음소거, 3초 주변 소음 Gate 추천/명시 적용, Windows 마이크 권한 바로가기 구현 | 기존 표정 슬롯/시트/프리셋/OBS 창과 중복하지 않고 방송 중 조작 및 초기 마이크 설정 격차 개선 |
| 2026-10-08 | Gate 추천은 서로 다른 fresh snapshot의 90백분위+6dB, 30개/2초 이상, -80~-15dB로 제한 | PCM 없이 숫자만 사용하고 무신호·과도한 소음·중복 집계 방지; 수치는 VoxPet 설계이며 음성 분류가 아님 |
| 2026-10-08 | 조사 근거와 구현 격차를 문서로 남기고 우선순위가 높은 기능을 선택 | 모든 후보를 무제한 추가하지 않고 기존 로컬 오디오 제품 목적에 맞춘다 |
| 2026-10-08 | Git metadata 제한은 승인 실행으로 동일 명령 재시도 | FETCH_HEAD 쓰기가 sandbox에서 거부되어 기존 절차를 지키기 위해 승인 실행 사용 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-08 | 설계 | main/origin clean 동기화 확인, plan013 worktree와 실행 계획 생성 |

| 2026-10-08 | 제작 | 기능18개 조사와 격차 기록. 전역 입 음소거/3초 Gate 추천/권한 설정 바로가기 및 안내 동기화. 숫자 기반 Core와 Windows smoke 회귀 추가. |

| 2026-10-08 | 검증 | macOS SDK10.0.401 locked restore 및 Release build 경고0/오류0, Core110/110 통과. VSTest 로컬 소켓 제한은 승인 환경 동일 명령으로 통과. 문서45개/36 PNG 계약 통과. |

| 2026-10-08 | 검증 | 341f1b4 Windows37742692699: Core110/110와 새 기능 fixture 통과, WPF237검사 중480×320 초기 Start/Stop 가시성1건 실패. 권한 버튼을 Start/Stop 뒤로 이동해 기존 상단 조작 접근 복원. OBS37742692659 WGC green/chroma/native alpha 각7프레임 통과; BitBlt 기존 runner 제한. |

| 2026-10-08 | 제작 | 추천 완료 후에도 취소 버튼을 활성화해 대기 추천을 폐기하도록 보완. pending 추천 취소와 측정 도중 종료 fixture 추가. ReadSnapshot 실제 무음/무신호 구분 검사 보완. |

## Completion Notes

조사·구현·QA·자체 리뷰 완료 후 결과와 실기 제한을 기록한다.
