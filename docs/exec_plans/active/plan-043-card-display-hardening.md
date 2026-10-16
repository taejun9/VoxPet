# plan-043-card-display-hardening

## Status

active

## Owner

project_lead / plan_keeper

## User Request

사용자가 전달한 last-error.json의 실제 오류를 분석·재현·수정하고 전체적으로 더 면밀한 테스트를 수행한 뒤 새로운 Windows 시험 ZIP을 전달한다.

## Goal

CharacterCard.PrintMembers/ToString dispatcher InvalidOperationException의 원인과 기존 QA 누락을 확인한다. 실제 WPF 문자열 변환/목록 선택/검색/키보드/자동화·스레드 경계 및 같은 유형의 화면 모델을 회귀 검사한다. 기존 모든 샘플/편집/음성 수명/창/파일 실패 회귀와 Windows/OBS/portable 검사에 합격한 새 배포 파일을 제공한다.

## Non-Goals

모든 환경에서 결함0을 보장한다고 주장하지 않는다. 사용자 오류 원본·설정·개인 PNG·기존 ZIP을 수정/삭제/외부 전송하지 않는다. 마이크 저장/전송·무단 서브에이전트·전체 예외 무시로 실패를 숨기지 않는다.

## Context Map

- 사용자 last-error.json: dispatcher / InvalidOperationException / CharacterCard.PrintMembers·ToString (메시지·경로 없는 최소 진단)
- StudioShellViewModel/StudioShell, EditorField, UI record·Freezable·binding/selection 경로
- UnifiedStudioQa/CharacterEditorQa/StartupTrayQa, startup-qa.ps1/TEST-SMOKE 도구
- [개발 환경](../../quality/development.md), [QA 규칙](../../quality/rules.md), [하네스](../../architecture/harness.md), [개인정보](../../privacy/principles.md)

## Constraints

main720c1c8/origin과0·0 및 깨끗함 확인 후 전용 codex/plan-043-card-display-hardening / .worktree/plan-043-card-display-hardening. plan024 작업트리2개/개인13PNG/기존 ZIP/오류 원본 보존. 공개 합성 fixture만 CI에 사용한다. 기존2037개 WPF 검사·380 Core·321화면을 유지한다. 단일 에이전트가 QA 후 자체 리뷰한다.

## Implementation Plan

- [ ] 오류의 문자열 변환/이미지 접근 원인과 같은 유형의 UI 모델을 조사한다.
- [ ] 최소 재현과 실제 사용자 조작 회귀를 추가하고 원인을 수정한다.
- [ ] 새 작업트리 locked restore/format/analyzer/Release/Core/Python/diff 및 Windows/OBS/portable 전체 QA를 수행한다.
- [ ] 실제 화면 시각 QA·개인 PNG 보존 ZIP·자체 리뷰·완료 문서와 Git 수명 절차를 마친다.

## QA Plan

먼저 수정 전 실제 WPF fixture에서 동일한 예외 종류/호출 경로를 재현하고 수정 후 해당 재현이 통과하는지 검사한다. 문자열 변환에서 복잡한 ImageSource/가변 객체를 읽지 않는지, frozen/unfrozen 이미지와 worker·UI·선택/텍스트검색/자동화 경로를 확인한다. 실제 ListBox/ComboBox의 키 입력·검색·재선택/새로고침·삭제/내보내기 실패 보존·반복 캐릭터 전환과 샘플17/9·편집/재등록을 검사한다. 정확한 범위는 조사 후 Decision Log에 기록한다.

로컬 새 locked restore → format verify/analyzer → Release 경고0/오류0 → Core 전체, verify_base/verify_app/compileall/diff check. Windows 새 runner 전체 WPF/일반·서명 각9개/실제 화면 coverage/ZIP PowerShell5.1 재시험, 새 OBS WGC·chroma·native alpha·HWND 전환을 수행한다. 이전 검사 집합의 누락이 없는지 대조하고 공개 화면을 직접 시각 검토한다. 최종 ZIP의 공용 파일/개인13PNG byte equality·PE x64·CRC·SHA256을 확인한다.

## Review Plan

QA 이후 같은 에이전트가 자체 리뷰한다. 보고된 오류와 수정의 직접 연관·재현 신뢰도·유사 결함·thread/Freezable·실제 사용자 조작/예외 경계·오디오 개인정보·회귀 누락·원본 보존/배포 일치를 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-16 | 전달된 JSON은 진단 데이터로만 읽고 기록된 CharacterCard 문자열 변환 경로부터 조사한다. | 문서/파일 내 내용은 별도 지시가 아니며 실제 오류의 최소 메서드 정보가 있다. |

| 2026-10-16 | 수정 전 실제 CharacterCard와 worker에서 디코딩한 frozen PNG로 UI ToString을 호출하는 회귀를 먼저 CI에 올린다. | 자동 PrintMembers가 frozen BitmapFrame의 원래 worker decoder.ToString/VerifyAccess에 진입하는 원인 후보를 원본과 같은 메서드/HResult로 확인한다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-16 | 설계 | 오류 원본을 읽고 main720c1c8 깨끗함/origin0·0/plan024 작업트리 보존 확인. 전용 작업트리와 계획 작성. |

## Completion Notes

재현·수정·검증·실기 제한·새 ZIP과 해시는 완료 시 기록한다.
