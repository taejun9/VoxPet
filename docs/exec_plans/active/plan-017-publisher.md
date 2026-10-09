# plan-017-publisher

## Status

active

## Owner

project_lead / plan_keeper

## User Request

알 수 없는 게시자를 수정하고 게시자 이름을 "김태중"으로 표시.

## Goal

EXE/assembly 제작자 메타데이터를 김태중으로 반영하고, Windows 실행 경고의 Authenticode 게시자와 파일 속성을 구분하여 실제 인증서 조건을 확인한다. 기존 개인12표정 패키지/원본을 보존하며 검증된 새 개인 패키지를 제공한다.

## Non-Goals

보안 경고/SmartScreen 우회·비활성화, 인증서 구매/계정 가입, 사용자 PC 신뢰 저장소 자동 변경, 개인 키/PNG의 Git·CI 업로드.

## Context Map

- src/VoxPet.App/VoxPet.App.csproj: Company/Authors/product metadata.
- harness/scripts/qa.ps1: Windows 실제 EXE VersionInfo와 서명 상태 검사.
- docs/guides/installation.md, docs/quality/development.md: 게시자 표시/서명 안내.
- root artifacts/VoxPet-neulbo-plan016/: 개인12종 패키지 보존·복사.
- Microsoft 공식 Code signing options/Set-AuthenticodeSignature: 신뢰와 서명 조건.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-017-publisher` / `.worktree/plan-017-publisher` 사용.
QA→자체 리뷰→completed/리뷰 미러→main 병합/push→branch-d→worktree 제거.
메타데이터 변경만으로 Windows 실행 경고의 게시자가 수정되었다고 보고하지 않는다. 사용할 인증서 보유 여부는 사용자 답변 확인 중. 개인 자산/기존 ZIP 유지.

## Implementation Plan

- [ ] 김태중 Company/Authors 메타데이터.
- [ ] EXE 실제 Windows 속성/서명 상태 검사와 사용자 설명.
- [ ] 인증서 조건 확인 후 허용된 서명 경로 또는 정확한 잔여 조건 기록.
- [ ] 문서/빌드/Windows QA→리뷰→개인 ZIP→Git 수명.

## QA Plan

verify_base.py/verify_app.py/git diff --check. SDK10.0.401 locked restore/format/Release/Core. Windows QA에서 published EXE CompanyName=김태중 및 실제 Authenticode 상태를 읽어 증거 JSON 생성. 서명 인증서가 없는 기본 CI는 unsigned 상태를 숨기지 않는다. 실행 로직과12개 PNG는 보존·해시 비교, 개인 ZIP CRC 검사.

## Review Plan

QA 이후 동일 에이전트 자체 리뷰. 제작자 메타데이터와 서명 게시자 차이, 개인 키/신뢰 저장소 경계, 문서와 실제 배포 서명 상태를 확인.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-09 | Company/Authors와 Authenticode를 구분 | 이름 문자열은 서명 신뢰를 대체할 수 없음 |
| 2026-10-09 | 인증서 보유 여부를 사용자에게 확인하면서 독립적인 메타데이터 작업 진행 | 서명할 인증서 없이 검증된 게시자라고 보고하지 않음 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-09 | 지도 | main clean/0-0 확인, plan017 worktree. 저장소에 서명 인증서 없음. 공식 Windows 서명 문서 확인 |

## Completion Notes

메타데이터와 인증서 조건 확인 진행 중. Windows 경고의 실제 게시자 변경은 신뢰되는 코드 서명을 필요로 한다.
