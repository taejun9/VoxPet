# plan-040-unified-test-studio

## Status

active

## Owner

project_lead / plan_keeper

## User Request

두 화면이 섞여 테스트하기 어려운 UI를 정리한다. 옷과 동작을 모두 시험할 수 있도록 샘플을 만들어 앱에 넣는다.

## Goal

일반 실행에서 한 navigation과 공통 미리보기 안에서 설정·옷·동작을 시험한다. 지원 분류별 의상과 동작 샘플을 제공하고 바로 착용·재생할 수 있게 한다. Windows 직접 시험 ZIP과 사용 안내를 준비한다.

## Non-Goals

개인 캐릭터 원본/설정 삭제, 음성 저장·전송·웹캠·추적, 사용자 PNG 외부 전송, 검증 없는 기존 기능 제거를 하지 않는다.

## Context Map

- src/VoxPet.App/Views/{MainWindow,StudioShell,CharacterEditorView} 및 ViewModels
- src/VoxPet.Core/{Attachments,Actions,Packages,Legacy}, Assets/ImageSetTemplate
- [개발 환경](../../quality/development.md), [규칙](../../quality/rules.md), [직접 시험](../../quality/windows-test-package.md)

## Constraints

codex/plan-040-unified-test-studio / .worktree/plan-040-unified-test-studio에서 단일 에이전트 작업. 구현 전 계획 작성, QA 후 자체 리뷰. 기존 개인 이미지와 다른 worktree를 보존하고 새로운 ZIP을 만든다. main 직접 구현 커밋/강제 Git 작업 금지.

## Implementation Plan

- [ ] 새 Studio와 기존 탭 화면 전환·설정·캐릭터 모드 관계를 확인한다.
- [ ] 선택된 통합 화면 안에서 기존 설정에 접근하도록 navigation을 정리한다.
- [ ] 의상·동작별 샘플과 즉시 시험 동선을 구현한다.
- [ ] Core/format/analyzer/build/harness 및 Windows/OBS·화면 QA를 수행한다.
- [ ] Windows ZIP·안내·자체 리뷰와 완료 계획/미러를 준비하고 Git 수명을 완료한다.

## QA Plan

locked restore/format verify/Release build 경고0·오류0/Core 전체 테스트, verify_base/verify_app/diff check. Windows 실제 WPF에서 navigation·의상/동작 샘플·미리보기/방송창 공유·설정 보존·실패/재시작과 작은 창 접근성을 검사한다. 화면 캡처를 시각 검토하고 OBS WGC 회귀 및 ZIP 실행 도구를 확인한다. 개인 이미지는 CI로 보내지 않는다.

## Review Plan

QA 후 같은 에이전트가 요구사항·단일 navigation·기존 기능/설정 호환·자산 권리·worker/입력 수명·샘플 실제 출력과 문서/패키지를 자체 리뷰한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-15 | 새 sidebar와 기존 tab 설정 전환이 혼재 원인으로 보이며 화면 선호/샘플 범위를 질문했다. 답을 기다리는 동안 공통 구조와 안전한 통합 지점을 조사한다. | 일반 실행은 Studio에서 시작하지만 설정·기존 의상·동작 버튼은 LayoutRoot로 화면 전체를 바꾼다. |
| 2026-10-15 | 사용자가 새 사이드바 통합과 모든 분류/동작 샘플을 선택했다. 설정의 기존 모델/패널은 동일 Studio 안으로 재배치하고 light palette를 적용한다. | 기능·저장 호환을 유지하면서 일반 사용자 동선에서 전체 이전 화면 전환을 제거한다. |
| 2026-10-15 | 공개 정적 .voxpet에 의상14분류·동작9종을 담고 번들 카드/샘플 시작 버튼을 제공한다. | 손동작5종과5개 ActionTemplate 전체를 포함하고 사용자 입력 없이도 수동 재생을 시험한다. 새 도형은 절차 생성 CC0이며 개인 PNG는 쓰지 않는다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-15 | 설계 | main/origin 동기화·깨끗한 상태 확인, 전용 worktree와 계획 작성. 서브에이전트 없음. |
| 2026-10-15 | 제작 | 단일 Studio navigation/공통 미리보기·설정 host·light 리소스와 의상 착용 상태/충돌 표시·동작 중지·샘플 카드/관리 복사본 재사용을 구현했다. 이전 전체 화면은 기존 합성 QA 기준에만 남기며 일반 버튼은 Studio 안으로 연결했다. |
| 2026-10-15 | 검증 | locked restore와 Release 컴파일 경고0·오류0, Core369/369 통과. 샘플57파일(32PNG/14의상/9동작/2문서)과 Core 회귀3개, 새 Studio WPF QA/48desktop 장면을 추가했다. 최종 formatter/build/Windows QA와 시각 검토를 이어서 수행한다. |

| 2026-10-15 | 검증 | 첫 Windows QA에서 editor_library_card_text_contrast 1실패를 확인했고, 실제 PNG에서 캐릭터·의상·동작 카드 이름의 밝은 글씨를 재현했다. DataTemplate의 TextBlock에 명시적 어두운 색을 적용하고 샘플 이름 대비 검사/작은 sidebar 줄바꿈을 보완했다. OBS bc9cebb WGC 회귀는 통과했다. |

## Completion Notes

통합 화면·샘플·QA·Windows 배포 결과와 실기 제한을 완료 시 기록한다.
