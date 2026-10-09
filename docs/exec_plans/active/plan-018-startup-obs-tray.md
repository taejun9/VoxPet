# plan-018-startup-obs-tray

## Status

active

## Owner

project_lead / plan_keeper

## User Request

Windows11에서 plan016 실행 직후 조용히 종료되는 버그를 제대로 시험. OBS32.2.2 창 캡처에서 plan015 캐릭터가 보이지 않는 문제 해결. 트레이 이동 기능 추가. 게시자 설정은 인증서 없는 개인PC 안내를 선택.

## Goal

일반 실행과 저장 상태/개인 PNG 가족 경로를 실제 Windows 프로세스에서 검사하고 종료 원인을 확인·수정한다. 복구 가능한 초기화 실패는 창/안내를 유지하며 치명 오류는 로컬 최소 진단을 남긴다. OBS 캡처 호환 모드/안내와 실제 캡처 회귀, 트레이 숨김·복원·방송창 독립·종료를 제공한다. 기존 설정/PNG/원본/ZIP 보존.

## Non-Goals

음성 저장/전송, 보안 정책 우회, 사용자 인증서 자동 신뢰 등록, 승인 없는 개인PNG Git/CI 전송, 물리 마이크/사용자GPU 결과를 합성 QA로 대체.

## Context Map

- App.OnStartup/MainWindow.Loaded/MainViewModel.InitializeAsync/ExpressionViewModel.InitializeAsync: 일반 시작과 저장 상태.
- CharacterSheetLoader/CharacterViewModel:12가족 PNG 로딩·스레드 경계.
- CharacterWindow/MainWindow.ShowCharacter: OBS background/투명·native 창 수명.
- 새 tray 서비스/시작 QA 및 harness/scripts: 실제 일반 프로세스/트레이/capture 증거.
- docs/quality/windows-checklist.md와 사용 안내: 재현 환경·이전 QA 사각지대·수동 Windows 게시자 설정.

## Constraints

No Exec Plan, No Work. main 구현 금지.
`codex/plan-018-startup-obs-tray` / `.worktree/plan-018-startup-obs-tray` 사용.
QA→자체 리뷰→completed/리뷰 미러→main 병합/push→branch-d→worktree 제거.
사용자는 Windows11/OBS32.2.2/창 캡처/시작 오류문구 없이 종료라고 확인했다. 기존 합성 smoke는 persistSettings=false여서 일반 로컬 저장/기본12PNG 시작을 검증하지 않았다. 실패 원인을 재현 전 단정하지 않는다.

## Implementation Plan

- [x] 기존 plan016의 일반 실행/12가족/저장 상태 재현과 원인 확인.
- [ ] 초기화·오류 복구/최소 진단/일반 시작 프로세스 QA.
- [ ] OBS 표시 문제의 캡처 호환 경로·UI/안내·실제 OBS QA.
- [ ] 트레이 이동/복원/우클릭 방송창·종료/자원 해제.
- [ ] 문서/개인 패키지/QA→리뷰→Git 수명.

## QA Plan

macOS verify_base.py/verify_app.py/diff --check/locked restore/format/Release/Core. Windows는 전체 WPF smoke 외에 일반 인수 없는 EXE의10초 이상 생존/보이는 창/재시작,12가족(352px셀)·손상 설정/순서·이전 저장 슬롯/PNG를 임시 LocalAppdata로 시험한다. 개인 이미지 전송 없이 동일 구조 합성PNG로 먼저 재현한다. 실제 개인 PNG가 원인 확인에 필요하면 전송 허용을 확인한다.
트레이에서는 main숨김/restore/native broadcast가시성/데모 유지/닫기 자원 해제. OBS32.2.2는 일반 설정창이 숨겨진 상태의 broadcast WGC/호환 캡처/green/chroma key와 재열기를 확인한다. 로컬 개인12PNG/ZIP 해시/CRC 보존.

## Review Plan

QA 이후 동일 에이전트 자체 리뷰. 재현 증거/원인-수정 대응, 실제 일반 실행 경로/저장값 보존, tray/방송창/마이크 수명, OBS 방식·제한과 문서, 오류 로그의 개인정보 경계를 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-09 | persistSettings=false smoke 외에 일반 EXE를 별도 프로세스에서 실행 | 사용자 실제 시작 경로가 기존 QA에 빠짐 |
| 2026-10-09 | 사용자 Win11/OBS32.2.2 창 캡처 환경을 기준으로 조사 | Server CI/GPU 시험을 사용자 환경의 완전한 재현으로 주장하지 않음 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-09 | 지도 | plan017 메타데이터/개인PC 안내를 완료·병합한 뒤 plan018 생성. 개인 원본과 모든 이전ZIP 보존 |

| 2026-10-09 | 검증 | 코드 수정 전37935023184에서 일반12가족 시작 실패 재현: NullReferenceException → 이동 버튼 CanExecute → InitializeAsync/SetBusy. WPF Selector가 초기화 중 Selected=null 전달 |
| 2026-10-09 | 제작 | 유효한 마지막 선택 유지/null·외부 슬롯 무시, 동일 위치 Move 생략/순서 갱신 후 선택 바인딩 재동기화 |

## Completion Notes

원인 재현/구현·검증 진행 중. 사용자 환경의 조용한 종료와 OBS 미표시를 이전 합성 통과만으로 해결되었다고 취급하지 않는다.
