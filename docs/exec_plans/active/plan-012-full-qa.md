# plan-012-full-qa

## Status

active

## Owner

project_lead / plan_keeper

## User Request

모든 테스트와 lint를 통과시키고 주석과 README를 확인한다. 실제 화면 시험은 늘보군으로 수행한다.

## Goal

고정 SDK의 전체 자동 QA와 Windows 실제 WPF 화면 검증을 다시 실행하고, 늘보군 시트의 표시·반응·표정 전환을 확인한다. 주석과 README의 불일치를 수정하고 증거를 남긴다.

## Non-Goals

음성 녹음·전송, 공개 배포에 개인 캐릭터 추가, 실제 마이크가 없는 CI를 물리 장치 실기로 주장하기.

## Context Map

- README.md, docs/quality/development.md, docs/quality/windows-checklist.md: 사용자 안내와 QA 기록.
- src/, tests/, harness/scripts/: 구현·주석·자동 검증.
- .github/workflows/windows.yml, obs.yml: Windows WPF와 OBS 실행 환경.
- root artifacts/characters/neulbo/: 기존 개인 시험 시트, Git 추적 제외.

## Constraints

`codex/plan-012-full-qa` / `.worktree/plan-012-full-qa`에서 작업한다. 사용자 요청 범위의 기존 파일을 부분 수정한다. 개인 시트를 Git/공개 배포에 넣지 않는다. QA → 자체 리뷰 → 완료 이동/리뷰 미러 → main 병합/push → branch -d → worktree 제거 절차를 따른다. 서브에이전트를 실행하지 않는다.

## Implementation Plan

- [x] README와 코드·하네스 주석을 실제 구현과 비교하고 불일치 수정.
- [x] 문서/자산/공백 검사, locked restore, format/analyzer, Release build, 전체 Core 테스트.
- [ ] Windows 전체 QA와 늘보군 실제 WPF 화면·상태/표정 전환 검증, OBS 회귀 확인.
- [ ] 결과 기록, QA 이후 자체 리뷰, Git 통합과 정리.

## QA Plan

macOS arm64의 기존 임시 SDK10.0.401로 verify_base.py, verify_app.py, git diff --check, locked restore, dotnet format --verify-no-changes, Release build, Core test를 수행한다. Windows에서 qa.ps1 -Publish -Smoke를 실행하고 실제 WPF 이미지와 JSON을 직접 확인한다. 기존 늘보군3×2 시트로 여섯 상태·합성 반응·미리보기와 방송창·표정 슬롯/눈물/blink를 확인한다. OBS WGC green/chroma/native alpha 회귀를 수행한다. 합성 입력과 물리 마이크·키보드·DPI 시험을 구분한다. 개인 시트 전송이 필요한 경우 기존 scoped URL 방식만 사용하고 임시 secret·draft·개인 QA artifact는 로컬 증거 수집 후 삭제한다.

## Review Plan

QA 이후 동일 에이전트가 주석의 설명/구현 일치, README의 기능·명령·검증 상태, QA 증거/개인정보·회귀를 자체 리뷰한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-07 | 기존 늘보군 로컬 시트를 재사용하고 Windows 실행 PNG를 확인 | 사용자가 실제 화면 시험과 캐릭터를 지정 |
| 2026-10-07 | macOS의 Git metadata 쓰기 제한은 승인 실행으로 동일 명령 재시도 | FETCH_HEAD 쓰기 거부, 사용자 변경 없이 worktree 규칙 준수 |
| 2026-10-07 | 로컬 QA 뒤 작업 브랜치 checkpoint commit/push로 Windows CI 실행 | 실제 WPF 실행에는 Windows runner가 필요, 완료 판정은 전체 QA 이후 |
| 2026-10-07 | 개인 시트의 표정 슬롯 재시작·손상 복구와 두 창 캡처를 기존 smoke에 추가 | 이전 개인 시험은 일반 불러오기의 입 반응만 확인 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-07 | 지도 | main/origin 동기화와 clean 상태 확인. plan012 worktree 생성. 기존 SDK와 늘보군 시트 확인. |
| 2026-10-07 | 제작 | README의84개/PNG6장 기록을97개/36장과 표정 슬롯 기능으로 동기화. 주석의 시간·좌표·저장/종료·키 범위를 보완. 정적 검사 성공 문구의 픽셀 정렬 주장을 헤더 검사로 정정. |
| 2026-10-07 | 검증 | 초기 fixture 컴파일에서 private StopAsync 접근 오류를 발견하고 기존 StopCommand로 수정. 재실행한 locked restore/format/analyzer/Release build(경고0/오류0)/Core97/97 통과. C#28파일의XML doc93블록, XAML4개, Python4개 구문 통과. |

## Completion Notes

진행 중. Windows 실제 실행과 늘보군 화면 확인 후 완료를 판정한다.
