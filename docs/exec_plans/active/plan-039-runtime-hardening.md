# plan-039-runtime-hardening

## Status

active

## Owner

project_lead / plan_keeper

## User Request

전체 test/lint를 통과시키고 실제 앱의 버그와 위험요소를 조사·수정한다. Windows에서 직접 테스트할 파일을 준비한다.

## Goal

실제 production 경로의 재현 가능한 결함을 회귀 검사와 함께 수정한다. locked restore/format/analyzer/build/Core/harness를 통과시키고 Windows 자동 WPF QA 및 실행 가능한 win-x64 ZIP과 수동 검사 안내를 준비한다.

## Non-Goals

새 기능·음성 저장/전송·개인 PNG 외부 전송·인증서 자동 신뢰를 추가하지 않는다. macOS 또는 합성 CI 결과를 실제 사용자 마이크/GPU 실기 합격으로 표현하지 않는다.

## Context Map

- [개발 환경](../../quality/development.md), [품질 규칙](../../quality/rules.md), [개인정보](../../privacy/principles.md)
- src/VoxPet.App/Services, ViewModels, Views; src/VoxPet.Core; tests/VoxPet.Core.Tests
- harness/scripts/qa.ps1 및 Windows/OBS workflows

## Constraints

codex/plan-039-runtime-hardening / .worktree/plan-039-runtime-hardening에서 작업한다. 단일 에이전트가 QA 후 자체 리뷰한다. 기존 사용자 파일·개인 PNG·다른 worktree를 보존한다. Git 실패는 원인을 확인하고 강제 우회하지 않는다.

## Implementation Plan

- [ ] 현재 검사와 production 경로를 확인하고 결함 재현 근거를 수집한다.
- [ ] 위험도 높은 결함을 수정하고 의미 있는 회귀 검사를 추가한다.
- [ ] 전체 local QA 및 Windows 자동 QA를 실행한다.
- [ ] Windows 실행 ZIP/테스트 안내를 준비하고 CRC/파일 구성을 확인한다.
- [ ] QA 후 자체 리뷰, 완료 이동/리뷰 미러, main 병합/push와 브랜치/worktree 정리를 수행한다.

## QA Plan

Python verify_base/verify_app, git diff --check, SDK10.0.401 locked restore, dotnet format verify, Release build 경고0/오류0 및 전체 Core tests. Windows CI에서 qa.ps1 -Publish -Smoke와 시작/종료/desktop coverage를 실행한다. 렌더 변경 시 OBS QA도 수행한다. ZIP에는 자체 포함 unsigned EXE·자산·라이선스·수동 테스트 안내를 넣고 CRC와 해시를 확인한다. 개인 자산은 전송하지 않는다.

## Review Plan

QA 이후 같은 에이전트가 production 수명/동시성/패키지·문서 경계/개인정보·회귀를 자체 리뷰한다. 자동 검사와 실기 미확인 사항을 구분한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-15 | plan039 격리 worktree에서 기존 최신 main을 기준으로 조사한다. | 기존 완료355 Core/WPF1744를 보존하며 실제 경로의 결함을 찾는다. |
| 2026-10-15 | scene/document 교체 전 worker에서 첫 frame까지 준비하고 native/편집 preview의 지연 변환 실패를 격리한다. | 유효한 패키지에서도 극단적 변환의 합성 overflow가 첫 tick 또는 action 중 발생해 기존 상태 오염/Dispatcher 오류로 이어질 수 있다. |
| 2026-10-15 | 삭제 후 preview ID를 재선택하고 미적용 입력 중 삭제/드래그를 막으며 앵커 없는 문서 편집을 지원한다. | 존재하지 않는 action/expression/attachment 참조와 암묵적 입력 손실을 방지한다. |
| 2026-10-15 | 입력128개/프레임, 마우스 버튼별 예약1개로 제한하고 standalone 테스트 ZIP을 CI에서 재실행한다. | UI 지연 때 메모리/Dispatcher 작업 누적을 막고 사용자에게 전달할 실제 파일까지 검사한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-15 | 설계 | main/origin 동기화와 깨끗한 상태 확인, 전용 worktree와 실행 계획 작성. |
| 2026-10-15 | 검증 | 기존 Core355/355 및 수정 중 Release 빌드 경고0/오류0. 로컬 임시 SDK 중 불완전한 설치 대신 기존 완전한10.0.401 설치를 사용했고 NuGet audit는 네트워크 허용 환경에서 통과했다. |

## Completion Notes

조사·QA 결과와 Windows 패키지 경로를 완료 시 기록한다.
