# plan-041-full-retest

## Status

completed

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
- [x] 새 작업트리에서 문서/자산/Python/locked restore/format/analyzer/Release/Core 전체 검사를 수행한다.
- [x] 새 Windows runner에서 일반/서명 시작·WPF·실화면305장·ZIP 재압축 해제 후 실행을 검사한다.
- [x] OBS 초록/크로마/투명·같은 HWND·프레임 변화 회귀를 새로 수행한다.
- [x] 화면/결과를 대조하고 개인13PNG를 보존한 새 ZIP·자체 리뷰·완료 기록을 준비한다.

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

| 2026-10-16 | 검증 | bin/obj 없는 새 작업트리 확인 후 문서111개·자산36RGBA/잠금·금지 API·Python compileall/diff check, locked restore, format verify/analyzer, Release 경고0·오류0, Core369/369(샘플3/3) 통과. 새 Windows38874585603/OBS38874637732를 b44685e에서 실행했다. |

| 2026-10-16 | 검증 | OBS38874637732 통과. source b44685e 일치, WGC 초록/크로마/투명·동일 HWND 배경 왕복·각7프레임 변화 확인. BitBlt는 기존 layered 창 제한으로 유지한다. 번들 샘플57파일/32개512RGBA PNG 전체 chunk CRC도 통과했다. |

| 2026-10-16 | 검증 | 새 Windows38874585603 전체 통과: Core369/369·WPF1981/0(이전 집합과 동일)·실화면305·unsigned/signed 각9개·portable1694/0. JSON/TRX/로그와 소스 커밋을 대조했다. |
| 2026-10-16 | 심사 | 모든 QA 후 같은 에이전트가 새 Studio56화면을 시각 검토했다. 통합 navigation/14의상/9동작/선택값 대비·눈물 이름 정상, 추가 필수 수정 사항 없음. CI39파일+개인13PNG byte equality/PE/CRC/SHA256 통과, 이전 파일 보존. |

## Completion Notes

새 작업트리·새 runner에서 모든 기존 자동 검사를 처음부터 다시 실행해 통과했다. 앱 변경 없이 이전 수정의 회귀 없음과 동일 검사 집합을 확인했다. [자체 리뷰](../../reviews/plan-041-full-retest-review.md)에 명령·결과·실기 제한을 기록했다.

Windows 파일은 `artifacts/VoxPet-Windows-test-plan041.zip`(52파일/95,605,003bytes), SHA256 `2cbb2b016a55222e94c1a0bf38688bc3971ceb25f0444cd3661670e027d027e0`다. 새 CI에서 실행한 공용39파일과 기존 개인13PNG의 bytes를 보존했다. 모든 증거는 `artifacts/test-results/plan041/`에 있다.

물리 마이크/GPU/DPI·개인 그림의 Windows 시각 검증·새60분 측정은 대상PC 실기와 구분한다. 이전 plan040 정리는 정상 승인 경로로 완료했다. 이번 완료 기록을 통합한 뒤 하네스 순서로 main push와 작업 브랜치/작업트리를 정리한다.
