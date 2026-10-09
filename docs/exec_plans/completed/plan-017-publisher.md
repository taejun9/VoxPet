# plan-017-publisher

## Status

completed

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
메타데이터 변경만으로 Windows 실행 경고의 게시자가 수정되었다고 보고하지 않는다. 사용자가 인증서 없음/개인PC용 설정 방법을 선택하여 로컬 서명·수동 신뢰 안내를 제공한다. 개인 자산/기존 ZIP 유지.

## Implementation Plan

- [x] 김태중 Company/Authors 메타데이터.
- [x] EXE 실제 Windows 속성/서명 상태 검사와 사용자 설명.
- [x] 인증서 조건 확인 후 허용된 서명 경로 또는 정확한 잔여 조건 기록.
- [x] 문서/빌드/Windows QA→리뷰→개인 ZIP→Git 수명.

## QA Plan

verify_base.py/verify_app.py/git diff --check. SDK10.0.401 locked restore/format/Release/Core. Windows QA에서 published EXE CompanyName=김태중 및 실제 Authenticode 상태를 읽어 증거 JSON 생성. 서명 인증서가 없는 기본 CI는 unsigned 상태를 숨기지 않는다. 실행 로직과12개 PNG는 보존·해시 비교, 개인 ZIP CRC 검사.

## Review Plan

QA 이후 동일 에이전트 자체 리뷰. 제작자 메타데이터와 서명 게시자 차이, 개인 키/신뢰 저장소 경계, 문서와 실제 배포 서명 상태를 확인.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-09 | Company/Authors와 Authenticode를 구분 | 이름 문자열은 서명 신뢰를 대체할 수 없음 |
| 2026-10-09 | 인증서 보유 여부를 사용자에게 확인하면서 독립적인 메타데이터 작업 진행 | 서명할 인증서 없이 검증된 게시자라고 보고하지 않음 |
| 2026-10-09 | 사용자 답변에 따라 개인PC용 설정 안내로 범위 확정 | 인증서가 없으며 설정 방법을 요청. 본인 Windows의 개인 키와 수동 신뢰 등록으로 수행 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-09 | 지도 | main clean/0-0 확인, plan017 worktree. 저장소에 서명 인증서 없음. 공식 Windows 서명 문서 확인 |
| 2026-10-09 | 제작 | Company/Authors=김태중. 실제 최종EXE 속성/Authenticode 검사, 인증서 보유·개인PC 시험 경로 문서 작성 |
| 2026-10-09 | 검증 | 코드9367120 macOS locked restore/format/Release0경고0오류. Windows37932695017 Core140/WPF690/CompanyName김태중·NotSigned 확인. OBS37932694997 통과 |
| 2026-10-09 | 설계 | 사용자 인증서 없음/개인PC 설정 안내 선택. 생성·서명은 사용자 Windows에서, 신뢰 등록은 직접 확인하는 경로 확정 |
| 2026-10-09 | 심사 | QA 이후 동일 에이전트 자체 리뷰. 메타데이터와 서명 신뢰의 차이/개인 키와 신뢰 저장소 자동 변경 없음 확인 |

## Completion Notes

사용자가 인증서 없음/개인PC용 설정 방법을 선택했다. 제작자 메타데이터와 설정 안내를 구현·검증하여 전달했다. 실제 Windows EXE는 NotSigned이며, 서명과 인증서 수동 신뢰 설정은 대상 Windows PC에서 안내에 따라 수행해야 한다. 실행 경고가 이미 바뀌었다고 보고하지 않는다.

- [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37932695017): Core140/WPF690/실패0/Release 경고오류0·publish, publisher.json의 CompanyName=김태중, signatureStatus=NotSigned, publisherVerified=false.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37932694997): 합성 WGC/green/Chroma Key/native alpha 회귀 통과.
- 개인 패키지 artifacts/VoxPet-win-x64-neulbo-plan017.zip에는 기존12PNG를 그대로 보존하고 게시자 현재상태와 Windows 서명 안내를 포함한다. 서명 완료 배포본으로 보고하지 않는다.
- 사용자 PC의 인증서 신뢰 저장소/SmartScreen/보안 정책을 변경하지 않았으며 인증서나 개인 키를 생성·전송하지 않았다. 개인 인증서 안내 명령은 사용자 PC에서 실행해야 하며 이번 CI에서는 실행하지 않았다. 사용자 선택에 따라 안내 제공 범위를 완료한다.
