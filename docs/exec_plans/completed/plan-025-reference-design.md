# plan-025-reference-design

## Status

completed

## Owner

project_lead / plan_keeper

## User Request

두 참고 이미지의 분위기를 참고해 전체 디자인을 수정하고 캐릭터는 늘보군과 펠리만 남긴다. 추가 제안의 파츠·레이어·액션·무기·의상·Anchor·입력 규칙을 고려하며 새 이미지 조합 실행 엔진까지 구현한다.

## Goal

파란 강조색의 다크 스튜디오 UI, 두 캐릭터 선택, 준비된 이미지 세트를 조합하는 런타임과 제작 가이드를 구현한다. 액션은 기존 파츠를 명시적으로 대체해 팔 중복을 방지하고 의상·무기·표정·음성의 담당 영역을 분리한다.

## Non-Goals

외부 그림 전송, 음성 저장, 웹캠, 그림 편집기, 신규 캐릭터 그림 생성. 기존 개인 원본·패키지 삭제는 하지 않는다.

## Context Map

- `src/VoxPet.App/App.xaml`, `src/VoxPet.App/Views/MainWindow.xaml`: 디자인·제작 가이드·런타임 등록 UI.
- `src/VoxPet.Core/Models/CharacterCatalog.cs`: 두 캐릭터 목록.
- `src/VoxPet.App/ViewModels/CharacterViewModel.cs`, `src/VoxPet.App/Views/CharacterView.xaml`: 기존 PNG와 신규 파츠 조합 통합.
- `src/VoxPet.Core/Models/`, `src/VoxPet.Core/Services/`: 이미지 세트 규격·검증·프레임 선택.
- `tests/VoxPet.Core.Tests/`, `src/VoxPet.App/Views/LayoutQa.cs`: 회귀·레이어·프레임·복구 검증.

## Constraints

`codex/plan-025-reference-design` / `.worktree/plan-025-reference-design`. 단일 에이전트 QA 후 자체 리뷰. 기존 오디오 분리와 finite 0~1 보존. 새 파일은 로컬에만 저장하며 원본을 덮어쓰지 않는다. Windows 실기와 macOS 빌드를 구분한다.

## Implementation Plan

- [x] 파츠 Canvas/레이어/대체/포즈/Anchor/우선순위/연타 규칙 확정.
- [x] 이미지 세트 manifest·검증·액션 프레임 조합 엔진 구현.
- [x] WPF 로컬 등록·조합 렌더·액션/입력 연결·가이드 구현.
- [x] 전체 디자인과 반응형 내비게이션 개편.
- [x] 캐릭터 두 종·이전 설정 복구·문서와 QA 동기화.
- [x] QA → 자체 리뷰 → 완료 이동/미러.
- main 병합/push 및 브랜치/worktree 정리는 이 검증된 완료 기록 커밋 이후 Git 수명 절차로 수행한다. 실패 시 중단하고 정확한 상태를 보고한다.

## QA Plan

verify_base.py, verify_app.py, locked restore, Release build, formatter, Core tests, git diff --check. 신규 테스트는 경로 탈출·Canvas/파츠 계약·액션 우선순위/숨김·무기/소매 포즈·빠른 재시작·기존 설정 복구를 검증한다. WPF 실행은 Windows QA에서 확인한다.

## Review Plan

QA 후 요구사항·바인딩·스레드·자산 경계·기존 PNG 호환성·가시성을 별도 자체 리뷰한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-13 | 참고 이미지의 색상·패널·탐색 구조만 차용 | 예시 속 미지원 기능을 제품에 허위 표시하지 않음 |
| 2026-10-13 | 사용자 답변에 따라 신규 이미지 조합 엔진까지 포함 | 디자인만 변경하는 범위를 확장함 |
| 2026-10-13 | 설정 초기화 완료 전에는 등록/키 편집/저장을 막음 | 시작 직후 종료가 이전 세트 등록을 기본값으로 덮어쓰지 않도록 함 |
| 2026-10-13 | 기본 Parts는 정확히6개, 의상·장식은 별도 레이어 | 장식이 기본 파츠 목록으로 우회해 머리 숨김·Anchor 규칙을 벗어나지 않도록 함 |
| 2026-10-13 | 무기 관리 썸네일은 투명 여백만 축소 | 전체 Canvas의 작은 무기를 읽기 쉽게 표시하고 출력의 원본 방향·좌표는 유지 |
| 2026-10-13 | Windows QA용 작업 브랜치 snapshot을 먼저 push | macOS에서 실행할 수 없는 WPF 화면·입력을 main 통합 전에 검증 |
| 2026-10-13 | 단일 Canvas의 사전 배치 PNG와 manifest 사용 | 좌표 편집 없이 파츠 정렬·액션 대체를 재현함 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-13 | 검증 | 두 번째 Windows QA: 새 엔진/배경 키/복원/가시성 통과, 신규 ComboBox4개의 기본 문자열 템플릿이 흰 글씨를 적용한32검사 실패. 선택/팝업 템플릿의 검정색을 명시해 수정 |
| 2026-10-13 | 검증 | 첫 Windows QA: build/Core198/publish 통과 후 새 fixture가 실제 runner보다 큰 가상 작업 영역을 사용해 화면 캡처 예외. 실제 SystemParameters.WorkArea로 수정하고 예외 시 검사 진행 기록도 보존 |
| 2026-10-13 | 검증 | 최종 Core199 / Windows1487검사 실패0 / 실제 화면240 / OBS 합성 QA 통과 |
| 2026-10-13 | 심사 | QA 이후 단일 에이전트 자체 리뷰 완료, 미해결 P1/P2 없음, 실기 경계 명시 |
| 2026-10-13 | 설계 | main/origin 동기화 확인 후 별도 worktree와 계획 생성 |

## Completion Notes

로컬 locked restore(공식 NuGet 취약성 확인 포함), formatter/analyzer, Release 경고0/오류0, Core199/199, 문서·PNG·diff 통과. 코드1dc9750의 Windows 전체 QA(38372703545)와 OBS 합성 QA(38372703485) 통과. WPF1487고유검사·실패0,240실제 화면 커버리지/PNG CRC, 일반/서명 재시작을 확인했다. 새 런타임과 기존 호환 경로의 바인딩/수명/자산 경계를 자체 리뷰했다.

로컬 실행 패키지는 root artifacts/VoxPet-reference-plan025.zip 및 같은 이름의 폴더다. 기존 검증된 늘보군12PNG와 펠리1PNG만 복사하며 원본을 보존하고 외부 전송하지 않는다. 새 제작 틀/앱 내 가이드와 상세 이미지 세트 문서를 포함한다. 개인 새 분리 파츠·실제 게임/마이크/DPI/장시간은 별도 실기 항목이다.
