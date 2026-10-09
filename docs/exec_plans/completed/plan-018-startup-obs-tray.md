# plan-018-startup-obs-tray

## Status

completed

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
- [x] 초기화·오류 복구/최소 진단/일반 시작 프로세스 QA.
- [x] OBS 표시 문제의 캡처 호환 경로·UI/안내·실제 OBS QA.
- [x] 트레이 이동/복원/우클릭 방송창·종료/자원 해제.
- [x] 문서/개인 패키지/QA→리뷰→Git 수명.

## QA Plan

macOS verify_base.py/verify_app.py/diff --check/locked restore/format/Release/Core. Windows는 전체 WPF smoke 외에 일반 인수 없는 EXE의10초 이상 생존/보이는 창/재시작,12가족(352px셀)·손상 설정/순서·이전 저장 슬롯/PNG를 임시 LocalAppdata로 시험한다. 개인 이미지 전송 없이 동일 구조 합성PNG로 먼저 재현한다. 실제 개인 PNG가 원인 확인에 필요하면 전송 허용을 확인한다.
트레이에서는 main숨김/restore/native broadcast가시성/데모 유지/닫기 자원 해제. OBS32.2.2는 일반 설정창이 숨겨진 상태의 broadcast WGC/호환 캡처/green/chroma key와 재열기를 확인한다. 로컬 개인12PNG/ZIP 해시/CRC 보존.

## Review Plan

QA 이후 동일 에이전트 자체 리뷰. 재현 증거/원인-수정 대응, 실제 일반 실행 경로/저장값 보존, tray/방송창/마이크 수명, OBS 방식·제한과 문서, 오류 로그의 개인정보 경계를 확인한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-09 | persistSettings=false smoke 외에 일반 EXE를 별도 프로세스에서 실행 | 사용자 실제 시작 경로가 기존 QA에 빠짐 |
| 2026-10-09 | 초록 배경은 불투명/non-layered 창, 투명 전환은 방송창만 재생성 | 기존 layered 초록 창의 캡처 호환 제한을 줄이고 native alpha 선택은 유지 |
| 2026-10-09 | 사용자 Win11/OBS32.2.2 창 캡처 환경을 기준으로 조사 | Server CI/GPU 시험을 사용자 환경의 완전한 재현으로 주장하지 않음 |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-09 | 지도 | plan017 메타데이터/개인PC 안내를 완료·병합한 뒤 plan018 생성. 개인 원본과 모든 이전ZIP 보존 |

| 2026-10-09 | 검증 | 코드 수정 전37935023184에서 일반12가족 시작 실패 재현: NullReferenceException → 이동 버튼 CanExecute → InitializeAsync/SetBusy. WPF Selector가 초기화 중 Selected=null 전달 |
| 2026-10-09 | 제작 | 유효한 마지막 선택 유지/null·외부 슬롯 무시, 동일 위치 Move 생략/순서 갱신 후 선택 바인딩 재동기화 |

| 2026-10-09 | 제작 | 트레이 메뉴/복원/숨김 상태 종료, 초록=불투명/non-layered 방송창·모드 변경 재생성/크기위치 유지, 마지막 오류 최소 진단 구현 |
| 2026-10-09 | 검증 | macOS format/analyzer/Release0경고0오류/Core140통과. 일반 프로세스5저장상태/실제 Selector/트레이와 OBS 재실행 |

| 2026-10-09 | 검증 | 일반 시작5종 모두10초/정상 종료/저장 불변,Core140/WPF725 통과. OBS32.2.2의 BitBlt·WGC 초록/크로마키와 투명/native alpha 통과. PNG 직접 확인 |
| 2026-10-09 | 심사 | 동일 에이전트 QA 이후 자체 리뷰. 재현-수정 연결/선택 불변식/트레이와 숨긴 창 종료/방송창 모드 수명/로컬 최소 진단 경계 확인 |
| 2026-10-09 | 정리 | 완료 계획/리뷰 미러와 사용·OBS·오류 안내 동기화. 기존12PNG/원본/ZIP 보존 및 새 개인ZIP CRC 확인 |

## Completion Notes

세 요청을 구현·검증하고 QA 이후 자체 리뷰를 완료했다. 일반 시작 오류는 수정 전 실제 프로세스에서 재현하여 원인을 확인했다.

- 재현37935023184: 일반12가족 프로세스 조용한 종료(-532462766), NullReferenceException/이동 명령 CanExecute/초기화 경로. Selected의 일시적 null을 무시하여 마지막 유효 선택을 유지하고 목록 갱신 후 바인딩을 동기화했다.
- 실행 코드4403f8d. [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37937258075) Core140/WPF725·실패0·Release 경고/오류0·publish. 일반 프로세스5종×10초/초기화 완료/가시성/정상 종료/슬롯·순서 불변 통과.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37937258114): 초록 불투명/non-layered 창으로 이전 실패하던 BitBlt와WGC 모두 통과, 각7프레임 변화/Chroma Key. 투명 WGC/native alpha65.08% 유지. main트레이 숨김 상태에서 실제 OBS32.2.2 캡처.
- 트레이는 설정창만 숨기며 입력/데모/방송창을 유지한다. 메뉴 복원/OBS 호환 열기/숨겨진 main 종료 및 자원 해제 검증. X는 정상 종료 유지.
- 시작 설정 실패는 안내와 창을 유지하며, 치명 UI 오류는 최소 로컬 진단과 메시지 후 종료한다. 예외 메시지/개인 경로/장치명/음성 기록·전송 없음 확인.
- 개인 ZIP artifacts/VoxPet-win-x64-neulbo-plan018.zip은26파일·CRC 통과, 기존12PNG와 원본 해시 동일. 이전 ZIP은 보존. 게시자 회사=김태중·NotSigned/개인PC 서명 안내 포함.
- 증거 artifacts/qa/plan018/repro/, windows/, obs/, summary.json/package.json. 실제 사용자 Windows11 GPU/OBS 장면/개인PNG 화면/물리Shell·마이크는 별도 확인. 개인PNG는 CI에 보내지 않았으나 PNG 그림 내용과 무관한 UI 회귀를 동일12가족 구조와 일반 실행으로 확인했다.
