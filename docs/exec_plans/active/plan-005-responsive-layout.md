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
| 2026-10-05 | 마우스 휠 routed-event 검사 추가 | WPF 공식 ScrollViewer 소스는 내측 영역에서 wheel을 handled로 바꿈. logical BringIntoView 성공만으로 실제 스크롤 입력을 입증하지 못함 |
| 2026-10-05 | 설정 영역을 하나의 ScrollViewer로 구성 | 중첩 wheel 가로채기를 제거하고 마우스/키보드/scrollbar가 같은 영역을 조작하도록 단순화 |
| 2026-10-05 | 초기 크기를 primary 작업 영역의 논리 좌표에 맞춤 | 최소 크기만 낮춰도 기본1000×730이 높은 DPI의 작업 영역을 넘을 수 있음. 공식 WPF WorkArea pixel→logical 변환 확인; QA는 가상 작업 영역을 주입하되 실제 창 생성과 bounds를 검사 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-05 | 지도 | main/원격 동기화 확인. 지정 branch/worktree 생성. 기존 MinWidth870/MinHeight680은 작은 논리 작업 영역을 초과함 |
| 2026-10-05 | 검증 | dd8c7d1 [37270743596](https://github.com/taejun9/VoxPet/actions/runs/37270743596): Core70/빌드/publish 통과, 정확히 세 requested-size 실패. 960×540은 실제960×680, 640×480/480×320은 실제870×680. JSON 직접 확인 |
| 2026-10-05 | 검증 | 31ab122 [37271000291](https://github.com/taejun9/VoxPet/actions/runs/37271000291): 크기/컨트롤 스크롤 가시성/기존 smoke 통과. 추가 입력 검토에서 nested wheel 처리 가능성 발견, routed-event 검사 보완 |
| 2026-10-05 | 검증 | 302a858 [37271324428](https://github.com/taejun9/VoxPet/actions/runs/37271324428): 정확히 작은 창 세 wheel 입력 실패. da17bb9 [37271591812](https://github.com/taejun9/VoxPet/actions/runs/37271591812)는 중첩 제거 후 전체 Windows QA 통과 |
| 2026-10-05 | 검증 | 3dcded2 [37272421104](https://github.com/taejun9/VoxPet/actions/runs/37272421104): Core70/Release/publish/smoke와 실제 창4크기×15컨트롤/작은 창wheel/초기StartStop, 가상작업영역3case의 실제창bounds/종료 통과. JSON직접확인, 최소창PNG시각검사. 물리DPI시험으로 해석하지 않음 |
| 2026-10-05 | 조율 | plan004 장시간 작업은 동일 run37269646209로 진행 중. 완료 결과를 main으로 통합한 뒤 이 브랜치에 main을 merge하고 전체 QA를 재검증한다. 이미 push한 checkpoint 이력은 rebase/force push로 덮어쓰지 않음 |

## Completion Notes

진행 중. 실제 마이크/사용자 DPI 환경의 전체 실사용 목표는 여전히 미완료다.
