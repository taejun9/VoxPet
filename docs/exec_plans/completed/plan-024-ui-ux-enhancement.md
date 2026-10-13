# plan-024-ui-ux-enhancement

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

이 프로젝트의 UI/UX를 전체적으로 보기 좋게 수정해줘.

## Goal

WPF 데스크톱 애플리케이션의 UI/UX를 모던하고 완성도 높은 다크 테마 디자인 시스템으로 개편한다.
1. 세련된 색상 체계와 미세 테두리, 카드 계층 구조 구축.
2. 버튼, 탭(상위 탭과 하위 세그먼트), 슬라이더, 체크박스, 텍스트박스, 프로그레스 바 등 공통 컨트롤의 모던 커스텀 스타일 도입.
3. 캐릭터 미리보기 카드와 상태 패널의 시각적 위계 및 정돈된 레이아웃 연출.
4. 기존 자동 레이아웃 QA(LayoutQa 4개 창 크기 완전 가시성·스크롤 금지, ComboBox 색상 계약, 7개 상위 탭 유지) 및 오디오·캐릭터 동작과의 100% 호환성 유지.

## Non-Goals

1. 오디오 처리 파이프라인이나 캐릭터 렌더링 물리 로직 변경.
2. 외부 서드파티 UI 라이브러리(MaterialDesignXaml 등) 추가 도입 (WPF 순수 XAML 및 무의존성 유지).
3. 새로운 비즈니스 기능이나 설정 항목 추가.
4. 마이크 권한이나 원격 전송 등 MVP 개인정보 원칙 위반 기능.

## Context Map

- `src/VoxPet.App/App.xaml`: 전역 테마 색상, 리소스 브러시, 공통 컨트롤 스타일(Button, TabControl, TabItem, Slider, CheckBox, TextBox, ComboBox 등)
- `src/VoxPet.App/Views/MainWindow.xaml`: 메인 대시보드 구조, 미리보기 카드, 7개 상위 탭 및 하위 탭 배치, 상태 카드
- `src/VoxPet.App/Views/MainWindow.xaml.cs`: 반응형 레이아웃 전환 및 컨트롤 크기 동기화
- `src/VoxPet.App/Views/CharacterWindow.xaml`: 캐릭터 방송창 스타일 및 컨텍스트 메뉴
- `src/VoxPet.App/Views/LayoutQa.cs`: 창 크기별 컨트롤 완전 가시성 및 가로/세로 스크롤 금지 계약
- `harness/scripts/verify_base.py`, `harness/scripts/verify_app.py`: 정적 검증 및 계약 검사

## Constraints

No Exec Plan, No Work. main에서 직접 구현 금지.
`codex/plan-024-ui-ux-enhancement` 및 `.worktree/plan-024-ui-ux-enhancement` 사용.
QA → 리뷰 → completed 이동 및 리뷰 미러 → main ff-only 병합/push → worktree 및 브랜치 정리.
LayoutQa 계약(4개 창 크기 FullyVisible, ScrollableHeight == 0, ComboBox 검정 텍스트, 7개 상위 탭) 절대 보존.
단일 에이전트 자체 리뷰로 진행하며 서브에이전트는 사용하지 않는다.

## Implementation Plan

- [x] 다크 테마 디자인 시스템 리소스 정의 (`App.xaml`: 배경, 카드, 테두리, 텍스트, 액센트 브러시).
- [x] 모던 공통 컨트롤 스타일 구현 (`App.xaml`: Button 호버/프레스 효과, Slider 커스텀 트랙/원형 썸, CheckBox 다크 커스텀, TextBox 다크 인디케이터, ProgressBar 둥근 게이지).
- [x] 상위 탭 및 서브 탭의 시각적 계층화 (1차 세그먼트 헤더와 2차 칩 필터 형태 구분).
- [x] MainWindow 미리보기 카드 및 상태 바의 비주얼 강화 (`MainWindow.xaml`: 캐릭터 스튜디오 캔버스 프레임, 인포 박스 정돈).
- [x] 방송창 및 전체 뷰 반응형 크기 동기화 확인 (`MainWindow.xaml.cs`).
- [x] verify_base / verify_app 및 git diff 검증 실행.

## QA Plan

- `python3 harness/scripts/verify_base.py`: 마크다운 구조, 링크, 출처 검증.
- `python3 harness/scripts/verify_app.py`: PNG 자산, 패키지 잠금, API 계약 검사.
- `git diff --check`: 공백 오류 검사.
- LayoutQa 호환성 검토: x:Name 보존, 7개 상위 탭 유지, 480x320~1000x730 가시성 보장.

## Review Plan

QA 완료 후 single-agent 자체 리뷰 수행.
기존 UI 인터랙션 및 바인딩 무결성, 가시성, 컨트롤 조작성, 반응형 축소 시 오버플로우 여부를 확인.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-13 | 순수 WPF XAML 스타일로 디자인 개편 | 외부 패키지 추가 없이 자체 포함 단일 실행 및 경량성 유지 |
| 2026-10-13 | ComboBox 텍스트 검정색 유지 및 컨테이너 스타일링 | LayoutQa의 색상 assert 계약을 준수하면서 모던한 카드형 인풋으로 조화 유도 |
| 2026-10-13 | Slider와 ProgressBar에 커스텀 ControlTemplate 적용 | 기본 윈도우 컨트롤의 투박함을 해소하고 일관된 네온 다크 룩앤필 제공 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-13 | 설계 | UI/UX 전체 리디자인을 위한 plan-024 수립 및 스타일 개선안 기획 |
| 2026-10-13 | 제작 | App.xaml 다크 테마 토큰 및 공통 컨트롤 스타일, MainWindow.xaml 스튜디오 카드/배지 레이아웃 리디자인 구현 |
| 2026-10-13 | 검증 | verify_base, verify_app, git diff --check 무결성 통과 확인 |
| 2026-10-13 | 심사 | LayoutQa 계약, x:Name 바인딩, 반응형 규칙 보존 자체 리뷰 완료 |

## Completion Notes

모던 다크 테마 디자인 시스템 적용 완료. Button/Slider/CheckBox/ProgressBar/TextBox 커스텀 스타일링, 캐릭터 스튜디오 캔버스 프레임, 탭 비주얼 인디케이터 및 반응형 레이아웃 일관성 확보. verify_base.py, verify_app.py, git diff --check 검증 통과.
