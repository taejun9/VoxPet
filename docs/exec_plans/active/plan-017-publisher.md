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

- [x] 김태중 Company/Authors 메타데이터.
- [x] EXE 실제 Windows 속성/서명 상태 검사와 사용자 설명.
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
| 2026-10-09 | 제작 | Company/Authors=김태중. 실제 최종EXE 속성/Authenticode 검사, 인증서 보유·개인PC 시험 경로 문서 작성 |
| 2026-10-09 | 검증 | 코드9367120 macOS locked restore/format/Release0경고0오류. Windows37932695017 Core140/WPF690/CompanyName김태중·NotSigned 확인. OBS37932694997 통과 |
| 2026-10-09 | 심사 | QA 이후 동일 에이전트 자체 리뷰. 메타데이터와 서명 신뢰의 차이/개인 키와 신뢰 저장소 자동 변경 없음 확인 |

## Completion Notes

제작자 메타데이터는 구현·검증했다. 실제 Windows 서명 상태는 NotSigned이며 Windows 경고의 게시자는 아직 변경하지 못했다. 사용할 인증서 보유 여부 답변을 기다린다. 인증서/개인 키를 갖고 있지 않아 신뢰되는 코드 서명을 수행할 수 없다. 현재 plan은 active로 보존하며 main 병합·완료 처리하지 않는다.

- [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37932695017): Core140/WPF690/실패0/Release 경고오류0·publish, publisher.json의 CompanyName=김태중, signatureStatus=NotSigned, publisherVerified=false.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37932694997): 합성 WGC/green/Chroma Key/native alpha 회귀 통과.
- 개인 패키지 artifacts/VoxPet-win-x64-neulbo-plan017.zip에는 기존12PNG를 그대로 보존하고 게시자 현재상태와 Windows 서명 안내를 포함한다. 서명 완료 배포본으로 보고하지 않는다.
- 사용자 PC의 인증서 신뢰 저장소/SmartScreen/보안 정책을 변경하지 않았으며 인증서나 개인 키를 생성·전송하지 않았다. 개인 인증서 안내 명령은 Windows에서 실행하지 않았다.


# 중간 자체 리뷰

## Summary

김태중 제작자 메타데이터 및 서명 상태 검사를 QA 이후 동일 에이전트가 자체 리뷰했다. 서브에이전트를 사용하지 않았다. 실제 Windows 실행 경고의 게시자 변경은 인증서가 없어 미완료다. plan은 active로 유지한다.

## QA

- macOS SDK10.0.401 locked restore/format/analyzer/Release0경고0오류. 문서/자산/diff 검증 통과.
- 코드9367120 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37932695017): Core140/WPF690/실패0, publish된 EXE CompanyName=김태중 확인. 실제 서명 NotSigned/서명자null/검증된 게시자false를 JSON으로 기록.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37932694997): 기존 합성 WGC/green/Chroma Key/native alpha 회귀 통과.
- 새 개인 패키지의12PNG는 이전 plan016과 해시/바이트 일치. 원본과 이전 ZIP은 보존한다.

## Findings

- Company/Authors 문자열은 Authenticode의 신원/신뢰를 대체하지 않는다. 파일 속성 제작자만 수정했으며 미서명 상태를 숨기거나 실행 경고 수정 완료로 보고하지 않는다.
- 인증서 개인 키/암호의 채팅·Git 전달, 자동 신뢰 저장소 설치, 보안 정책 변경을 추가하지 않았다. 문서의 개인 시험 명령은 실제로 실행하지 않았다.
- QA는 최종 single-file EXE의 CompanyName을 읽어 검사한다. 기본 CI에 인증서를 추가하지 않고 실제 서명 상태를 기록하므로 추후 서명 경로와도 구분할 수 있다.

## Residual Risk

사용자 인증서 보유 여부/서명자 이름과 대상 Windows PC의 신뢰 상태가 필요하다. 자체 서명 인증서는 해당 PC의 수동 신뢰 설정이 필요하며 공용 배포 신원 검증이나 SmartScreen 평판을 대신하지 않는다. 이 조건이 해소될 때까지 게시자 경고 수정은 미완료다.

## Follow-Ups

인증서 보유 여부 응답에 따라 로컬 서명 경로를 확정하고 실제 Authenticode 서명/Windows 표시를 검증한다. 이후 계획 완료 이동과 main 병합/push/브랜치·worktree 정리를 수행한다.
