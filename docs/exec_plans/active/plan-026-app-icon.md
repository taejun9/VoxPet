# plan-026-app-icon

## Status

active

## Owner

project_lead / plan_keeper

## User Request

첨부한 늘보군 그림을 프로그램 아이콘으로 적용하고 macOS에서도 테스트할 수 있는지 안내한다.

## Goal

원본 그림을 보존하는 재현 가능한 PNG/ICO 변환을 적용해 실행 파일·창·트레이·앱 머리글을 일치시킨다. macOS 빌드/Core 검사와 Windows GUI 검증을 구분한다.

## Non-Goals

그림 재생성·배경 제거·캐릭터 파츠 변경·macOS 앱 이식. 기존 개인 캐릭터 자료와 패키지는 보존한다.

## Context Map

- `src/VoxPet.App/Assets/`: 아이콘 원본과 출력.
- `harness/scripts/generate_icon.py`: 재생성 경로.
- `src/VoxPet.App/VoxPet.App.csproj`, `src/VoxPet.App/Views/MainWindow.xaml`: 리소스 연결.
- `docs/quality/development.md`: 플랫폼별 실행 범위.

## Constraints

`codex/plan-026-app-icon` / `.worktree/plan-026-app-icon`. 단일 에이전트 QA 후 별도 자체 리뷰. 사용자 지정 프로그램 아이콘만 Git에 포함하고 개인 캐릭터 PNG는 로컬 패키지에만 둔다.

## Implementation Plan

- [ ] 원본 보존 및 7크기 ICO/PNG 변환·머리글 적용.
- [ ] macOS 검증과 Windows 실행 파일 아이콘·GUI 검증.
- [ ] 문서·자체 리뷰·완료 이동과 Git 절차.
- [ ] 새 Windows 실행 패키지 생성.

## QA Plan

verify_base.py, verify_app.py, 아이콘 크기/원본 해시·출력 재현 확인, locked restore, format/analyzer, Release build, Core tests, git diff --check. Windows CI에서 publish·서명/아이콘·WPF smoke를 실행한다.

## Review Plan

요구사항, 아이콘 사용 경로와 재생성, 원본 보존, 플랫폼 설명, 개인 자료 제외를 QA 이후 검토한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-13 | 첨부 그림의 여백과 색상을 그대로 보존해 형식만 변환 | 요청은 지정 그림 적용이며 추가 그림 편집은 요구하지 않음 |
| 2026-10-13 | Windows QA용 브랜치를 먼저 push | macOS에서 WPF/트레이 실행을 검증할 수 없음 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-13 | 설계 | clean main과 origin 동기화 후 작업 격리, 공식 WPF/WindowsTargeting 문서 확인 |

## Completion Notes

검증 후 결과를 기록한다.
