# plan-041-full-retest

## Status

active

## Owner

project_lead / plan_keeper

## User Request

이전 화면 통합·전체 의상/동작 샘플 작업을 이어서 진행하고 처음부터 다시 테스트한다.

## Goal

main5c5dd4c의 앱을 새 작업트리/Windows runner에서 복원·lint·빌드·Core·WPF·일반 시작·OBS·ZIP 실행까지 새로 검증한다. 샘플14분류/9동작과 통합 화면을 재확인하고 검증된 최신 Windows 테스트 ZIP·결과를 제공한다. 실패가 있으면 원인을 수정하고 필요한 전체 회귀를 재실행한다.

## Non-Goals

개인 PNG/사용자 설정의 외부 전송·삭제·원본 덮어쓰기, 음성 저장/전송, 물리 마이크/GPU/DPI를 합성 CI 결과로 합격 처리하는 일은 하지 않는다. 별도60분 장시간 측정은 이번 기존 전체 QA 재실행과 구분한다.

## Context Map

- [이전 완료 계획](../completed/plan-040-unified-test-studio.md), [자체 리뷰](../../reviews/plan-040-unified-test-studio-review.md)
- [개발 환경](../../quality/development.md), [QA 규칙](../../quality/rules.md), [Git 절차](../../architecture/harness.md)
- .github/workflows/windows.yml / obs.yml, harness/scripts/qa.ps1, verify_base.py, verify_app.py, verify_screens.py
- [Studio 샘플](../../guides/studio-samples.md), [Windows 직접 시험](../../quality/windows-test-package.md)

## Constraints

codex/plan-041-full-retest / .worktree/plan-041-full-retest에서 단일 에이전트 작업. 새 작업트리에 bin/obj 없이 시작하며 잠금 파일을 그대로 복원한다. 이전 개인13PNG·ZIP·다른 작업트리를 보존한다. QA 후 자체 리뷰/완료 계획·리뷰 미러를 기록하고 main fast-forward/push/branch -d/worktree 제거 순서를 따른다. 실패한 Git 단계는 강제로 우회하지 않는다.

## Implementation Plan

- [x] 이전 main push와 작업 상태를 확인하고 승인 제한으로 중단된 plan040 정리를 재개한다.
- [ ] 새 작업트리에서 문서/자산/Python/locked restore/format/analyzer/Release/Core 전체 검사를 수행한다.
- [ ] 새 Windows runner에서 일반/서명 시작·WPF·실화면305장·ZIP 재압축 해제 후 실행을 검사한다.
- [ ] OBS 초록/크로마/투명·같은 HWND·프레임 변화 회귀를 새로 수행한다.
- [ ] 화면/결과를 대조하고 개인13PNG를 보존한 새 ZIP·자체 리뷰·완료 기록을 준비한다.

## QA Plan

로컬 Python verify_base/verify_app/compileall, git diff --check, SDK10.0.401 locked restore → format --verify-no-changes --no-restore → Release build 경고0/오류0 → Core 전체 테스트/TRX를 새로 실행한다. Windows CI는 qa.ps1 -Publish -Smoke, 일반/서명 각9사례, source 기반305화면 coverage/PNG CRC, SDK 없는 ZIP 재실행을 수행한다. OBS workflow는 long_run_minutes=0으로 새 runner에서 실행한다. 결과 JSON/TRX/로그·소스 커밋을 직접 대조하고 Studio 변경56화면을 시각 검토한다. 개인 ZIP은 CI 공용 파일과 기존13PNG의 byte equality·PE x64·CRC·SHA256을 확인한다.

## Review Plan

모든 QA 후 같은 에이전트가 자체 리뷰한다. 이전 회귀(단일 navigation·샘플 범위·착용 충돌·동작/방송창·선택값 대비·눈물 패널 이름), 입력/개인정보 경계와 검사 누락/불안정성, 패키지/안내 일치를 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-16 | 새 계획/작업트리와 새 CI 실행으로 검증한다. 앱 변경은 실패가 재현될 때만 수행한다. | 사용자가 처음부터 재시험을 요청했고 기존 산출물/개인 자산을 보존해야 한다. |
| 2026-10-16 | plan040의 main push는 완료된 것을 확인하고 사용량 제한으로 실행되지 않았던 detach/branch -d/worktree 제거를 정상 승인 경로로 재개해 완료했다. | 외부 상태 변경과 사용자의 재개 요청이 있으며 우회/강제 작업 없이 절차를 마친다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-16 | 설계 | main5c5dd4c 깨끗함/origin과0·0 확인. plan040 정리 완료, 다른 plan024 작업트리2개/기존 ZIP 보존. plan041 새 작업트리에서 계획 작성. |

## Completion Notes

새 실행의 명령·결과·화면·패키지 무결성 및 실기 제한은 QA 후 기록한다.
