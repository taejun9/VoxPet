# plan-006-stop-error

## Status

completed

## Owner

project_lead / quality_runner / review_judge

## User Request

실사용 기능과 버그 테스트를 계속한다. native capture/Stop 오류는 사용자 상태에 전달되어야 한다.

## Goal

StopAsync의 정리 도중 Ended로 전달된 native 오류가 성공 상태로 숨겨지는지 재현한다.
오류가 있으면 정리된 자원은 해제 상태로 두되 Faulted/오류 안내/무음과 재시작 복구를 보장한다.

## Non-Goals

실제 마이크 테스트 완료 주장, 강제 native 해제, 오디오 저장/전송, UI나 수치 처리 변경.

## Context Map

AudioSession.StopAsync/CleanupAsync, AudioCaptureService.StopAsync/Ended, CaptureLoop, SessionTests.
plan004/005는 별도 worktree에서 유지한다. 그 main 통합 이후 이 브랜치에 main을 merge한다.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-006-stop-error` / `.worktree/plan-006-stop-error` 사용.
QA → 자체 리뷰 → completed 이동/리뷰 미러 → main ff-only 병합/push → branch -d → worktree 제거.
실패한 Git 단계 이후 강제 우회 금지. Core의 fake는 실제 마이크 점유 해제 증거로 대체하지 않는다.

## Implementation Plan

- [x] Stop 정리 도중 Ended 오류의 재현 검사
- [x] 정리 완료 이후 오류 상태 반영
- [x] 자원 없음/무음/새 Start 복구 및 기존 정상 Stop 회귀 검사
- [x] 기존 계획 통합 후 Windows QA/자체 리뷰/문서/완료 기록; Git 수명은 완료 커밋 이후 지정 순서로 수행

## QA Plan

fake input의 StopAsync가 Ended(IOException)를 보내고 정상 완료하는 경우 State=Faulted, Error 있음, HasResources=false, ReadLevel=Silence여야 한다.
새 input Start 이후 State=Running/Error=null로 복구해야 한다. 기존 정상 종료/timeout/동시 요청/old callback 검사 유지.
Core 전체 test, Release build, Windows smoke/publish, verify_base.py/verify_app.py/git diff --check.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰. 입력 정리가 끝난 뒤 settled 오류를 읽고, 정상 종료의 null 오류를 새 오류로 만들지 않는지 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-05 | 최소 Core 회귀로 native Ended/Stop race를 재현 | 현재 Stop은 Cleanup 전의 Ended만 확인하며 실제 native worker의 Stop 오류는 정리 대기 중에 전달될 수 있음 |
| 2026-10-05 | Windows QA용 checkpoint branch push 허용 | main 통합 전 Windows의 Core/WPF 회귀 검증 필요 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-05 | 지도 | main/원격 동기화 확인 후 지정 branch/worktree 생성 |
| 2026-10-05 | 검증 | 수정 전 targeted Core 회귀가 Expected Faulted / Actual Stopped로 실패. fake Stop은 Ended(IOException)를 보낸 뒤 정상 완료하여 native completion 오류 전달 누락을 재현 |
| 2026-10-05 | 검증 | 수정 후 macOS SDK10.0.401 locked restore/Release build 경고·오류0/Core71 통과. 새 회귀는 Faulted/오류 안내/자원없음/무음/새 Start 복구를 검사하며 기존 정상 Stop/정리 재시도 검사도 통과 |
| 2026-10-05 | 검증 | c8c0313 [Windows CI37274793440](https://github.com/taejun9/VoxPet/actions/runs/37274793440) 통과. TRX total/passed=71, failed=0 및 smoke JSON failures=0 직접 확인. root artifacts/qa/plan006에 보존. plan004/005와의 통합 검증은 진행 예정 |

| 2026-10-05 | 조율 | main의 완료 plan004/005를 정상 merge하여4ad3180 통합 checkpoint 생성. 이력 재작성 없이 Windows37278367856/OBS37278367918 실행 |
| 2026-10-05 | 검증 | 통합 macOS Release build 경고·오류0/Core71 및 문서27개/자산/diff 검사 통과 |

| 2026-10-05 | 검증 | 통합4ad3180 [Windows37278367856](https://github.com/taejun9/VoxPet/actions/runs/37278367856)와 [OBS37278367918](https://github.com/taejun9/VoxPet/actions/runs/37278367918) 모두 통과. TRX71/71, smoke failures0, 4크기×15controls/3startup bounds/wheel, green·transparent WGC와native alpha 결과JSON 및 PNG 직접 확인 |
| 2026-10-05 | 심사 | QA 이후 단일 에이전트 자체 리뷰: Stop이 worker의 Ended 완료를 기다린 뒤 오류를 읽으며 정상Stop/timeout/자원정리/새Start 회귀71개 통과. 오디오 저장·전송이나 UI blocking 변경 없음 |
| 2026-10-05 | 정리 | 통합 Windows runner의 자체 포함 EXE/라이선스/사용안내/manifest를 root artifacts/win-x64-plan006과VoxPet-win-x64-plan006.zip으로 보존. PE AMD64/필수파일/ZIP CRC/SHA256 검증. 누적 증거는 artifacts/qa/plan006/integrated에 보존 |

## Completion Notes

Stop 오류 누락 수정과 누적 자동 QA를 완료했다. settled native 오류는 Faulted/오류 안내로 전달되며 자원 정리와 무음, 새 Start 복구를 유지한다.
계획은 completed로 이동하고 리뷰 미러를 남겼다. 완료 커밋 후 main ff-only 병합/push와 branch/worktree 정리를 수행한다.
실제 Windows11 마이크/권한 거부/USB 제거/실제 DPI 전환/실제 입력60분은 미검증이다. OBS60.17분 합성 자원 결과는 plan004 d35f707 버전이다. 전체 사용자의 목표는 실기 증거 없이 완료로 표시하지 않는다.
