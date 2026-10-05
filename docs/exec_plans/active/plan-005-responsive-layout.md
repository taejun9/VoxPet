# plan-005-responsive-layout

## Status

active

## Owner

project_lead / harness_builder / quality_runner

## User Request

전체 기능이 실사용 가능하도록 구현과 버그 검증을 계속한다. 작은 화면과 높은 배율에서 설정 UI에 접근할 수 있어야 한다.

## Goal

870×680으로 고정된 최소 창 크기를 해소하고 좁은 창에서 마이크/Start/Stop/슬라이더/방송 옵션을 스크롤로 조작할 수 있게 한다.
Windows 실제 WPF 창에서 1000×730, 960×540, 640×480, 480×320 논리 크기의 배치와 접근성을 검증한다.
논리 크기와 고해상도 render 검사를 실제 모니터 DPI 변경 시험으로 대체하지 않는다.

## Non-Goals

음성 저장/전송, 실제 마이크 자동 시작, 사용자 디스플레이 설정 변경, UI 프레임워크 교체.
현재 실행 중인 plan004 OBS 장시간 작업을 취소하거나 합격으로 미리 판정하지 않는다.

## Context Map

MainWindow.xaml/코드, App의 Windows smoke, product.md/quality/windows-checklist.md/사용 안내.
현재 main7936158에서 시작하며 plan004 완료 후 그 결과를 이 브랜치에 통합한다.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-005-responsive-layout` / `.worktree/plan-005-responsive-layout` 사용.
QA → 자체 리뷰 → completed 이동/리뷰 미러 → main ff-only 병합/push → branch -d → worktree 제거.
실패한 Git 단계 후 강제 우회 금지. plan004와 별도 브랜치로 독립 작업하며 OBS 진행을 유지한다.

## Implementation Plan

- [ ] Windows에서 기존 최소 크기 문제를 재현하는 검사
- [ ] 좁은 창의 단일 열/스크롤/버튼 줄바꿈과 넓은 창의 기존 두 열 배치
- [ ] 실제 WPF 논리 크기/컨트롤 가시성/PNG 배치와 render 증거
- [ ] plan004 통합 이후 QA/자체 리뷰/문서/완료 기록/Git 수명

## QA Plan

Windows 실제 창의 요청 논리 크기가 최소값으로 강제 확대되지 않아야 한다.
각 크기에서 Start/Stop/데모, 모든 슬라이더와 방송 옵션이 BringIntoView로 창 및 부모 scroll viewport 안에 완전히 표시되어야 한다.
넓은 화면 두 열/좁은 화면 한 열 전환, 고급 옵션 펼침/접힘, 원래 크기 복구를 확인한다.
100/150/200% render PNG는 logical layout의 raster 출력 검사이며 실제 시스템 DPI/다중 모니터 이동 합격으로 기록하지 않는다.
Core70/Release build/publish/WPF smoke, verify_base.py/verify_app.py, git diff --check 유지.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰. scroll clipping/레이아웃 전환/명령 접근/기존 방송창 수명/개인정보 경계를 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-05 | main에서 plan005 별도 작업, plan004의 완료 결과는 후속 통합 | OBS 70분 시험의 검증 대상과 실행을 유지하며 화면 접근성 문제를 병행 해결 |
| 2026-10-05 | 실제 DPI 시험과 논리 크기/raster scale 검증 구분 | CI의 고정된 화면만으로 사용자 다중 모니터/DPI 전환을 입증할 수 없음 |
| 2026-10-05 | Windows QA용 checkpoint branch push 허용 | main 병합 전에 실제 WPF에서 재현/수정 근거 확보 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-05 | 지도 | main/원격 동기화 확인. 지정 branch/worktree 생성. 기존 MinWidth870/MinHeight680은 작은 논리 작업 영역을 초과함 |

## Completion Notes

진행 중. 실제 마이크/사용자 DPI 환경의 전체 실사용 목표는 여전히 미완료다.
